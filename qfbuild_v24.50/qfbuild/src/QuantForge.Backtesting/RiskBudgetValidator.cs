using QuantForge.Core;

namespace QuantForge.Backtesting;

public sealed record RiskBudgetDecision(bool Allowed, decimal StopRisk, decimal MaxRisk, decimal ModeledRoundTripFriction, string Reason, string Fingerprint);

public static class RiskBudgetValidator
{
    public static RiskBudgetDecision Evaluate(TradingRiskSettings settings, int quantity)
    {
        if (quantity < 1 || quantity > settings.MaxContracts)
            return Reject(settings, quantity, "QUANTITY_EXCEEDS_GOVERNED_LIMIT");
        var stopRisk = settings.InitialStopPoints * (settings.TickValue / settings.TickSize) * quantity;
        var friction = settings.EstimatedRoundTripFriction * quantity;
        var allowed = stopRisk <= settings.MaxRiskPerTrade;
        return new(allowed, stopRisk, settings.MaxRiskPerTrade, friction,
            allowed ? "RISK_BUDGET_ACCEPTED_COSTS_REPORTED_SEPARATELY" : "STOP_RISK_EXCEEDS_MAX_RISK_PER_TRADE",
            ResearchFingerprint.Sha256($"{settings.PrimaryInstrument}|{quantity}|{stopRisk}|{settings.MaxRiskPerTrade}|{friction}"));
    }

    private static RiskBudgetDecision Reject(TradingRiskSettings settings, int quantity, string reason) =>
        new(false, 0m, settings.MaxRiskPerTrade, settings.EstimatedRoundTripFriction * Math.Max(quantity, 0), reason,
            ResearchFingerprint.Sha256($"{settings.PrimaryInstrument}|{quantity}|REJECT|{reason}"));
}
