using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using QuantForge.Core;

namespace QuantForge.Governance;

/// <summary>
/// First-line source inspection. It never compiles, loads, invokes, or executes imported source.
/// Detection is heuristic and is not a malware certification.
/// </summary>
public sealed class StrategySourceScrubber
{
    private static readonly (StrategyFeatureCategory Category, string Name, string Pattern, bool Sensitive, bool Supported)[] Rules =
    {
        (StrategyFeatureCategory.Indicators, "EMA/SMA indicator", @"\b(EMA|SMA|ExponentialMovingAverage|SimpleMovingAverage)\b", false, true),
        (StrategyFeatureCategory.Indicators, "ATR indicator", @"\b(ATR|AverageTrueRange)\b", false, true),
        (StrategyFeatureCategory.Signals, "Cross signal", @"\b(CrossAbove|CrossBelow|crossover|crossunder)\b", false, true),
        (StrategyFeatureCategory.Orders, "Order placement", @"\b(EnterLong|EnterShort|ExitLong|ExitShort|SubmitOrder|strategy\.entry|strategy\.exit)\b", true, true),
        (StrategyFeatureCategory.RiskManagement, "Stop or target", @"\b(SetStopLoss|SetProfitTarget|StopLoss|ProfitTarget|stop_loss|take_profit)\b", false, true),
        (StrategyFeatureCategory.PlotsAndDrawings, "Plot or drawing", @"\b(AddPlot|Draw\.|plot\(|plotshape\(|plotchar\()", false, true),
        (StrategyFeatureCategory.Alerts, "Alert", @"\b(Alert|alertcondition|alert\()", false, true),
        (StrategyFeatureCategory.Timeframes, "Secondary timeframe", @"\b(AddDataSeries|request\.security|security\(|timeframe\.|TimeFrame)\b", false, true),
        (StrategyFeatureCategory.ExternalDependencies, "Import/include/require", @"\b(import|using|#include|require\s*\()\b", true, false),
        (StrategyFeatureCategory.NetworkAccess, "Network access", @"\b(HttpClient|WebClient|fetch\s*\(|WebSocket|socket|HttpRequest|urllib|requests\.)\b", true, false),
        (StrategyFeatureCategory.FileSystemAccess, "File system access", @"\b(File\.|FileStream|Directory\.|open\s*\(|readFile|writeFile)\b", true, false),
        (StrategyFeatureCategory.EnvironmentAccess, "Environment/secret access", @"\b(Environment\.|process\.env|os\.environ|Cookies?|localStorage|sessionStorage)\b", true, false),
        (StrategyFeatureCategory.DynamicExecution, "Dynamic code execution", @"\b(eval\s*\(|exec\s*\(|Compile\s*\(|DynamicMethod|Assembly\.Load|Function\s*\()", true, false),
        (StrategyFeatureCategory.ProcessExecution, "Child process/shell", @"\b(Process\.Start|child_process|spawn\s*\(|execFile\s*\(|powershell|cmd\.exe|bash\s+-c)\b", true, false),
        (StrategyFeatureCategory.Authentication, "Credential/authentication access", @"\b(API[_-]?KEY|SECRET|PASSWORD|TOKEN|Authorization|Credential|Bearer)\b", true, false),
        (StrategyFeatureCategory.DataAccess, "External data access", @"\b(ReadCsv|CSV|JsonDocument|Http|WebRequest|DataReader)\b", true, false),
    };

    public StrategyAuditReport Audit(string sourceId, string source, StrategySourceLanguage language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(source);
        var contentHash = Hash(source);
        var features = new List<StrategyFeature>();
        var dependencies = new List<string>();
        var safety = new List<string>();
        var licenseSignals = new List<string>();

        foreach (var rule in Rules)
        {
            var match = Regex.Match(source, rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
            if (!match.Success) continue;
            var evidence = Evidence(source, match.Index, match.Length);
            var featureSeed = $"{rule.Category}|{rule.Name}|{NormalizeEvidence(evidence)}";
            var featureId = $"F-{Hash(featureSeed)[..12]}";
            features.Add(new StrategyFeature(featureId, rule.Category, rule.Name, evidence, rule.Sensitive, rule.Supported));
            if (rule.Category == StrategyFeatureCategory.ExternalDependencies) dependencies.Add(evidence);
            if (rule.Sensitive) safety.Add($"{rule.Name}: detected; manual review required before canonical commit.");
        }

        foreach (Match m in Regex.Matches(source, @"(?i)(?:license|spdx|copyright|mit|apache(?:-2\.0)?|gpl(?:-?3)?|bsd)\b", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250)))
            licenseSignals.Add(Evidence(source, m.Index, m.Length));
        var orderedFeatures = features.OrderBy(f => f.FeatureId, StringComparer.Ordinal).ToArray();
        var fingerprint = Hash(string.Join("|", new[] { sourceId, contentHash, language.ToString() }.Concat(orderedFeatures.Select(f => $"{f.FeatureId}:{f.Category}:{f.Name}:{f.Evidence}"))));
        return new StrategyAuditReport(sourceId, contentHash, language, orderedFeatures, dependencies.Distinct().Order(StringComparer.Ordinal).ToArray(), safety.Distinct().Order(StringComparer.Ordinal).ToArray(), licenseSignals.Distinct().Order(StringComparer.Ordinal).ToArray(), false, fingerprint,
            "Source audit is static inspection only; it cannot establish tick, bid/ask, spread, replay, or execution fidelity and does not certify source as safe.");
    }

    public StrategyComponentSelection Select(StrategyAuditReport audit, IEnumerable<string> selectedFeatureIds, bool commitRequested)
    {
        var available = audit.Features.Select(f => f.FeatureId).ToHashSet(StringComparer.Ordinal);
        var selected = selectedFeatureIds.Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        if (selected.Any(id => !available.Contains(id))) throw new InvalidOperationException("Selection contains a feature that was not present in the audit report.");
        if (selected.Any(id => audit.Features.First(f => f.FeatureId == id).IsSafetySensitive))
            throw new InvalidOperationException("Safety-sensitive source features require explicit governance review before canonical commit.");
        var fingerprint = Hash(string.Join("|", audit.AuditFingerprint, string.Join(",", selected.Order(StringComparer.Ordinal))));
        return new StrategyComponentSelection(audit.SourceId, selected, fingerprint, commitRequested);
    }

    public CanonicalStrategyModel BuildCanonicalModel(StrategyAuditReport audit, StrategyComponentSelection selection)
    {
        if (!string.Equals(audit.SourceId, selection.SourceId, StringComparison.Ordinal)) throw new InvalidOperationException("Audit and selection source IDs do not match.");
        if (!selection.CommitRequested) throw new InvalidOperationException("Canonical model creation requires an explicit component-selection commit request.");
        var expectedSelection = Hash(string.Join("|", audit.AuditFingerprint, string.Join(",", selection.SelectedFeatureIds.Order(StringComparer.Ordinal))));
        if (!string.Equals(expectedSelection, selection.SelectionFingerprint, StringComparison.Ordinal)) throw new InvalidOperationException("Component selection fingerprint does not match the audited source.");
        var selected = audit.Features.Where(f => selection.SelectedFeatureIds.Contains(f.FeatureId) && f.IsSupportedByCanonicalModel).ToArray();
        var indicators = selected.Where(f => f.Category == StrategyFeatureCategory.Indicators).Select(f => f.Name).ToArray();
        var entries = selected.Where(f => f.Category == StrategyFeatureCategory.Signals).Select(f => f.Name).ToArray();
        var exits = selected.Where(f => f.Category == StrategyFeatureCategory.Orders).Select(f => f.Name).ToArray();
        var risk = selected.Where(f => f.Category == StrategyFeatureCategory.RiskManagement).Select(f => f.Name).ToArray();
        var visuals = selected.Where(f => f.Category == StrategyFeatureCategory.PlotsAndDrawings).Select(f => f.Name).ToArray();
        var alerts = selected.Where(f => f.Category == StrategyFeatureCategory.Alerts).Select(f => f.Name).ToArray();
        var timeframes = selected.Where(f => f.Category == StrategyFeatureCategory.Timeframes).Select(f => f.Name).ToArray();
        var modelFingerprint = Hash(string.Join("|", audit.ContentHash, selection.SelectionFingerprint, string.Join(",", indicators), string.Join(",", entries), string.Join(",", exits), string.Join(",", risk), string.Join(",", timeframes), string.Join(",", visuals), string.Join(",", alerts)));
        return new CanonicalStrategyModel($"canonical:{modelFingerprint[..16]}", audit.SourceId, audit.ContentHash, indicators, entries, exits, risk, timeframes, visuals, alerts, modelFingerprint, true, false);
    }

    private static string NormalizeEvidence(string evidence) => Regex.Replace(evidence.Trim(), @"\s+", " ", RegexOptions.CultureInvariant);

    private static string Evidence(string source, int index, int length)
    {
        var start = Math.Max(0, index - 45);
        var count = Math.Min(source.Length - start, Math.Max(80, length + 45));
        return source.Substring(start, count).Replace('\r', ' ').Replace('\n', ' ').Trim();
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
