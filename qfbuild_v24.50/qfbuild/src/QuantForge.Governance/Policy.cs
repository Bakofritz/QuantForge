namespace QuantForge.Governance;

public sealed record GovernanceDecision(bool Allowed, string Code, string Reason);

public interface IAuthorityPolicy
{
    GovernanceDecision Evaluate(string operation);
}

public sealed class DeterministicAuthorityPolicy : IAuthorityPolicy
{
    private static readonly HashSet<string> Blocked = new(StringComparer.Ordinal)
    {
        "LIVE_ORDER", "LIVE_TRADING", "ARBITRARY_CODE_EXECUTION", "CANONICAL_DATA_MUTATION", "UNDECLARED_APP_MUTATION", "MANIFEST_MUTATION"
    };

    public GovernanceDecision Evaluate(string operation) =>
        Blocked.Contains(operation)
            ? new(false, "AUTHORITY_BLOCKED", $"Operation '{operation}' is outside QuantForge authority boundaries.")
            : new(true, "ALLOWED", "Operation is eligible for downstream bounded-policy checks.");
}
