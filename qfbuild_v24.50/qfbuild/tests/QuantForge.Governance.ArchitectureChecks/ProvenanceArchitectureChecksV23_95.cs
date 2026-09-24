namespace QuantForge.Governance.ArchitectureChecks;
public static class ProvenanceArchitectureChecksV23_95
{
    public static bool RequiresAllFingerprints() => !QuantForge.Backtesting.ResearchReproducibilityGateV23_95.IsReproducible(new("r","s","d","p","e",""));
}
