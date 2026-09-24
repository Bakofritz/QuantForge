using QuantForge.Core;

namespace QuantForge.Backtesting;

public static class CanonicalRiskExecutionAdapter
{
    public static CanonicalExecutionRiskSemantics Build(TradingRiskSettings settings) => CanonicalExecutionRiskSemantics.FromSettings(settings);

    public static CanonicalStrategyExecutionPlan Bind(CanonicalStrategyExecutionPlan plan, TradingRiskSettings settings)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var risk = Build(settings);
        var fp = ResearchFingerprint.Sha256($"{plan.ExecutionPlanFingerprint}|{risk.Fingerprint}");
        return plan with { RiskProfileFingerprint = risk.Fingerprint, ExecutionPlanFingerprint = fp };
    }
}
