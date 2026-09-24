using QuantForge.Core;

namespace QuantForge.Governance;

/// <summary>Source-aware import facade. It only selects the static language profile; it never compiles or executes source.</summary>
public sealed class SupportedStrategyImportService
{
    private static readonly IReadOnlySet<StrategySourceLanguage> Supported = new HashSet<StrategySourceLanguage> { StrategySourceLanguage.NinjaScript, StrategySourceLanguage.TradingViewPine, StrategySourceLanguage.CSharp, StrategySourceLanguage.Python, StrategySourceLanguage.JavaScript };
    private readonly StrategyAcquisitionService _acquisition;
    public SupportedStrategyImportService(StrategyAcquisitionService? acquisition = null) => _acquisition = acquisition ?? new StrategyAcquisitionService();

    public QuarantinedStrategySource ImportForAudit(string sourceId, string source, StrategySourceLanguage language)
    {
        if (!Supported.Contains(language)) throw new NotSupportedException($"Strategy source language '{language}' is not enabled for governed import.");
        return _acquisition.AcquireAndAudit(sourceId, source, language);
    }
}
