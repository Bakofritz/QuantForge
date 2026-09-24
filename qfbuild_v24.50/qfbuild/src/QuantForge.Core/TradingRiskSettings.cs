namespace QuantForge.Core;

public enum QuantForgeConnectionProfile
{
    WindowsEthernet,
    WindowsWifi,
    AndroidWifi,
    AndroidMobileData
}

public sealed record TradingRiskSettings(
    decimal InitialAccountSize,
    string PrimaryInstrument,
    IReadOnlyList<string> PlannedInstruments,
    int MaxContracts,
    decimal MaxRiskPerTrade,
    decimal DailyLossLimit,
    decimal InitialStopPoints,
    decimal TrailingActivationPoints,
    decimal TrailingDistancePoints,
    decimal ProfitTargetPoints,
    decimal CommissionPerSide,
    decimal SlippageTicksPerSide,
    decimal TickSize,
    decimal TickValue,
    QuantForgeConnectionProfile ConnectionProfile,
    int EstimatedRoundTripLatencyMs,
    string CommissionAssumptionSource,
    string LatencyAssumptionSource,
    string FidelityDeclaration)
{
    public decimal EstimatedRoundTripFriction =>
        (CommissionPerSide * 2m) + (SlippageTicksPerSide * TickValue * 2m);

    public decimal InitialStopRisk => InitialStopPoints * (TickValue / TickSize);

    public static TradingRiskSettings DefaultMes() => new(
        InitialAccountSize: 3000m,
        PrimaryInstrument: "MES",
        PlannedInstruments: new[] { "MNQ" },
        MaxContracts: 1,
        MaxRiskPerTrade: 30m,
        DailyLossLimit: 60m,
        InitialStopPoints: 6m,
        TrailingActivationPoints: 4m,
        TrailingDistancePoints: 2m,
        ProfitTargetPoints: 0m,
        CommissionPerSide: 0.95m,
        SlippageTicksPerSide: 1m,
        TickSize: 0.25m,
        TickValue: 1.25m,
        ConnectionProfile: QuantForgeConnectionProfile.WindowsWifi,
        EstimatedRoundTripLatencyMs: 80,
        CommissionAssumptionSource: "User-specified simulation assumption: $0.95 per side. Verify against the live account agreement before deployment.",
        LatencyAssumptionSource: "Engineering estimate for scenario testing only; measure actual connection/provider latency before relying on it.",
        FidelityDeclaration: "Risk model is an OHLCV research assumption with explicit slippage and estimated network-latency scenarios; it is not a measurement of broker or exchange latency.");
}

public static class QuantForgeRiskDefaults
{
    public static TradingRiskSettings DefaultMes => TradingRiskSettings.DefaultMes();
}
