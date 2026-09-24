using QuantForge.Core;
using QuantForge.Governance;

namespace QuantForge.ArchitectureTests;

public static class StrategyGovernanceArchitectureChecks
{
    public static void VerifyNoLiveDeploymentFromCanonicalModel()
    {
        var model = new CanonicalStrategyModel("x","s","f",Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),"m",true,false);
        if (model.LiveDeploymentEligible) throw new InvalidOperationException("Canonical research models must not grant live deployment authority.");
    }

    public static void VerifyFeatureSelectionFingerprintIsOrderIndependent()
    {
        var source = "EMA CrossAbove SMA";
        var scrubber = new StrategySourceScrubber();
        var audit = scrubber.Audit("test",source,StrategySourceLanguage.CSharp);
        var ids = audit.Features.Select(x=>x.FeatureId).ToArray();
        var a = scrubber.Select(audit,ids,true); var b = scrubber.Select(audit,ids.Reverse(),true);
        if (a.SelectionFingerprint != b.SelectionFingerprint) throw new InvalidOperationException("Selection fingerprint changed with selection ordering.");
    }
}
