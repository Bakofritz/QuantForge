namespace QuantForge.ArchitectureChecks;

public static class ArchitectureCheckV24_25
{
    public static bool LiveBrokerageMustRemainDisabled => true;
    public static bool LiveOrdersMustRemainDisabled => true;
    public static bool ResearchDataMustRemainReadOnly => true;
    public static bool ResearchExecutionMustRemainSimulationOnly => true;
}
