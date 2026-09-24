namespace QuantForge.Core;

public static class ResearchAuthorityBoundary
{
    public static bool IsSimulationOnly(string operation) => operation switch
    {
        "BACKTEST" or "OPTIMIZE" or "REPLAY" or "ANALYZE" or "AUDIT" => true,
        _ => false
    };

    public static bool IsProhibited(string operation) => QuantForgeAuthority.ProhibitedAuthorities.Contains(operation);
}
