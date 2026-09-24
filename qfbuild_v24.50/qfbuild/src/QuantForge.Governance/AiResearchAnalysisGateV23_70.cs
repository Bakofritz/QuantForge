namespace QuantForge.Governance;

public sealed record AiResearchAuthorityV23_70(bool ReadOnlyMarketData, bool SimulationOnly, bool CanChangeApplicationSettings, bool CanRouteOrders, string ScopeFingerprint);
public static class AiResearchAnalysisGateV23_70
{
    public static bool IsPermitted(AiResearchAuthorityV23_70 a) => a.ReadOnlyMarketData && a.SimulationOnly && !a.CanChangeApplicationSettings && !a.CanRouteOrders && !string.IsNullOrWhiteSpace(a.ScopeFingerprint);
}
