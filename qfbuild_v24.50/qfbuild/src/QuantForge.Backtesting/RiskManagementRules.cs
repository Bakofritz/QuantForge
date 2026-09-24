using QuantForge.Core;

namespace QuantForge.Backtesting;

public sealed record RiskManagementSnapshot(
    decimal AccountSize,
    decimal InitialStopPoints,
    decimal TrailingActivationPoints,
    decimal TrailingDistancePoints,
    decimal EstimatedInitialRisk,
    decimal EstimatedRoundTripFriction,
    bool RiskBudgetSatisfied,
    string Fingerprint);

public static class RiskManagementRules
{
    public static RiskManagementSnapshot Evaluate(TradingRiskSettings settings)
    {
        var initialRisk = settings.InitialStopRisk;
        var friction = settings.EstimatedRoundTripFriction;
        var satisfied = settings.MaxContracts == 1 && initialRisk <= settings.MaxRiskPerTrade;
        var normalized = string.Join("|", settings.InitialAccountSize, settings.PrimaryInstrument, settings.MaxContracts,
            settings.MaxRiskPerTrade, settings.InitialStopPoints, settings.TrailingActivationPoints,
            settings.TrailingDistancePoints, settings.CommissionPerSide, settings.SlippageTicksPerSide,
            settings.EstimatedRoundTripLatencyMs);
        return new RiskManagementSnapshot(settings.InitialAccountSize, settings.InitialStopPoints,
            settings.TrailingActivationPoints, settings.TrailingDistancePoints, initialRisk, friction, satisfied,
            ResearchFingerprint.Sha256(normalized));
    }
}
