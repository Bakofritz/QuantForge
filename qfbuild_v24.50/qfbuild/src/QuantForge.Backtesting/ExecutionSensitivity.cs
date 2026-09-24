using QuantForge.Core;

namespace QuantForge.Backtesting;

public sealed record ExecutionScenario(
    string ScenarioId,
    int LatencyMs,
    decimal SlippageTicksPerSide,
    string ConnectionProfile);

public sealed record ExecutionScenarioResult(
    string ScenarioId,
    int LatencyMs,
    decimal SlippageTicksPerSide,
    decimal CommissionPerSide,
    decimal RoundTripFriction,
    decimal FrictionAsPercentOfAccount,
    string AssumptionFingerprint);

public sealed record ExecutionSensitivityReport(
    string Instrument,
    decimal AccountSize,
    IReadOnlyList<ExecutionScenarioResult> Results,
    string ReportFingerprint,
    string FidelityDeclaration);

public static class ExecutionSensitivityAnalyzer
{
    public static ExecutionSensitivityReport Analyze(TradingRiskSettings baseline, IEnumerable<ExecutionScenario> scenarios)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(scenarios);
        var results = scenarios.Select(s =>
        {
            if (s.LatencyMs < 0) throw new ArgumentOutOfRangeException(nameof(scenarios));
            if (s.SlippageTicksPerSide < 0m) throw new ArgumentOutOfRangeException(nameof(scenarios));
            var friction = (baseline.CommissionPerSide * 2m) + (s.SlippageTicksPerSide * baseline.TickValue * 2m);
            var percent = baseline.InitialAccountSize == 0m ? 0m : friction / baseline.InitialAccountSize * 100m;
            var fp = ResearchFingerprint.Sha256($"{baseline.PrimaryInstrument}|{baseline.InitialAccountSize}|{baseline.CommissionPerSide}|{s.LatencyMs}|{s.SlippageTicksPerSide}|{s.ConnectionProfile}");
            return new ExecutionScenarioResult(s.ScenarioId, s.LatencyMs, s.SlippageTicksPerSide, baseline.CommissionPerSide, friction, percent, fp);
        }).ToArray();
        var reportFp = ResearchFingerprint.Sha256(string.Join("|", results.Select(x => x.AssumptionFingerprint)));
        return new(baseline.PrimaryInstrument, baseline.InitialAccountSize, results, reportFp,
            "Sensitivity scenarios are modeled assumptions. Historical OHLCV does not establish network, broker-routing, exchange-matching, or packet-level latency.");
    }

    public static IReadOnlyList<ExecutionScenario> DefaultScenarios() => new[]
    {
        new ExecutionScenario("WIN_ETHERNET_BASE", 35, 1m, "Windows Ethernet"),
        new ExecutionScenario("WIN_WIFI_BASE", 80, 1m, "Windows Wi-Fi"),
        new ExecutionScenario("ANDROID_WIFI_BASE", 80, 1m, "Android Wi-Fi"),
        new ExecutionScenario("ANDROID_MOBILE_BASE", 180, 1m, "Android mobile data"),
        new ExecutionScenario("HIGH_LATENCY", 250, 2m, "Stress scenario"),
        new ExecutionScenario("EXTREME_LATENCY", 500, 3m, "Stress scenario")
    };
}
