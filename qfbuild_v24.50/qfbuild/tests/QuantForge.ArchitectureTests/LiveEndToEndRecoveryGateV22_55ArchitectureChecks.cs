using QuantForge.Governance;
namespace QuantForge.ArchitectureTests;
public static class LiveEndToEndRecoveryGateV22_55ArchitectureChecks
{ public static void UnknownNeverResumes(){var s=new LiveEndToEndRecoveryStateV22_55(LiveRecoveryStageV22_55.Unknown,true,true,true,true,true);if(LiveEndToEndRecoveryGateV22_55.CanResume(s))throw new InvalidOperationException("UNKNOWN_RESUMED");} public static void ResumeRequiresAllGates(){var s=new LiveEndToEndRecoveryStateV22_55(LiveRecoveryStageV22_55.Revalidated,true,true,true,true,true);if(!LiveEndToEndRecoveryGateV22_55.CanResume(s))throw new InvalidOperationException("VALID_RECOVERY_BLOCKED");} }
