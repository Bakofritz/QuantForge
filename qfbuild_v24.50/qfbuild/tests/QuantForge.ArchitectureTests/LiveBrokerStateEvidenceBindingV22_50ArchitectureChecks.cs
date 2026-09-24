using QuantForge.Governance;
namespace QuantForge.ArchitectureTests;
public static class LiveBrokerStateEvidenceBindingV22_50ArchitectureChecks
{ public static void RejectsMismatchedEvidence(){var e=new LiveBrokerStateEvidenceBindingV22_50("e","i","f","broker-order","s");if(LiveBrokerStateEvidenceBindingV22_50Policy.Matches(e,"e","i","changed","s"))throw new InvalidOperationException("MISMATCHED_BROKER_EVIDENCE_ACCEPTED");} }
