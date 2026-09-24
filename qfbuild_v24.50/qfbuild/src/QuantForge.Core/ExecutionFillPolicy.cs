namespace QuantForge.Core;

public enum OhlcvIntrabarAmbiguityPolicy
{
    ConservativeStopFirst,
    TargetFirst,
    CloseProximity,
    RejectAmbiguousBar
}

public sealed record IntrabarExitDecision(
    string Reason,
    decimal ExitPrice,
    bool Ambiguous,
    string Policy,
    string FidelityDeclaration)
{
    public bool ExitTriggered => !string.Equals(Reason, "NONE", StringComparison.OrdinalIgnoreCase);
}
