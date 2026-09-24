using QuantForge.Core;

namespace QuantForge.Core.Runtime;

public sealed record ControlledConcurrencyBoundaryCheck(string Name, bool Passed, string Detail);
public sealed record ControlledConcurrencyBoundaryResult(string HarnessVersion, bool Passed, IReadOnlyList<ControlledConcurrencyBoundaryCheck> Checks, string ResultFingerprint);

/// <summary>Deterministic integration validation for the native/resource/lifecycle execution boundary.</summary>
public sealed class ControlledConcurrencyExecutionBoundaryHarness
{
    public ControlledConcurrencyBoundaryResult Run()
    {
        var checks = new List<ControlledConcurrencyBoundaryCheck>
        {
            CheckUnqualifiedRuntimeBlocked(),
            CheckUnknownEstimateBlocked(),
            CheckBudgetBlocked(),
            CheckQualifiedAdmissionProducesEffectiveConcurrency(),
            CheckWorkerIdentityRequired(),
            CheckLifecycleFingerprintDeterminism(),
            CheckNoImplicitExecutionAuthority()
        };
        var fp = ResearchFingerprint.Sha256(string.Join("|", checks.Select(x => $"{x.Name}:{x.Passed}:{x.Detail}")));
        return new("QF-CONCURRENCY-BOUNDARY-1", checks.All(x => x.Passed), checks.AsReadOnly(), fp);
    }

    private static ControlledConcurrencyExecutionBoundary Boundary => new();
    private static ControlledConcurrencyExecutionRequest Request(bool qualified = true, long perCase = 100, long budget = 400, int requested = 4, int configured = 3)
        => new("batch-a", "worker-a", "worker-fp", requested, configured, perCase, budget, qualified);

    private static ControlledConcurrencyBoundaryCheck CheckUnqualifiedRuntimeBlocked()
    {
        var r = Boundary.Evaluate(Request(false));
        return new("NATIVE_RUNTIME_REQUIRED", !r.Allowed && r.StopCode == "NATIVE_RUNTIME_NOT_QUALIFIED", "Unqualified native runtime cannot admit controlled execution.");
    }

    private static ControlledConcurrencyBoundaryCheck CheckUnknownEstimateBlocked()
    {
        var r = Boundary.Evaluate(Request(perCase: 0));
        return new("RESOURCE_ESTIMATE_REQUIRED", !r.Allowed && r.StopCode == "RESOURCE_ESTIMATE_UNAVAILABLE", "Unknown resource demand fails closed.");
    }

    private static ControlledConcurrencyBoundaryCheck CheckBudgetBlocked()
    {
        var r = Boundary.Evaluate(Request(perCase: 200, budget: 400, requested: 4, configured: 3));
        return new("MEMORY_BUDGET", !r.Allowed && r.StopCode == "MEMORY_BUDGET_EXCEEDED", "Effective concurrency must fit the declared memory budget.");
    }

    private static ControlledConcurrencyBoundaryCheck CheckQualifiedAdmissionProducesEffectiveConcurrency()
    {
        var r = Boundary.Evaluate(Request(perCase: 100, budget: 400, requested: 4, configured: 3));
        return new("EFFECTIVE_CONCURRENCY", r.Allowed && r.EffectiveConcurrency == 3 && string.IsNullOrEmpty(r.StopCode), "Qualified admission is bounded by configured concurrency.");
    }

    private static ControlledConcurrencyBoundaryCheck CheckWorkerIdentityRequired()
    {
        try
        {
            Boundary.Evaluate(Request() with { WorkerFingerprint = "" });
            return new("WORKER_IDENTITY", false, "Missing worker identity was accepted.");
        }
        catch (ArgumentException)
        {
            return new("WORKER_IDENTITY", true, "Worker identity is mandatory at the execution boundary.");
        }
    }

    private static ControlledConcurrencyBoundaryCheck CheckLifecycleFingerprintDeterminism()
    {
        var a = Boundary.Evaluate(Request());
        var b = Boundary.Evaluate(Request());
        return new("LIFECYCLE_FINGERPRINT", a.LifecycleFingerprint == b.LifecycleFingerprint && a.AdmissionFingerprint == b.AdmissionFingerprint, "Identical admission inputs produce identical boundary fingerprints.");
    }

    private static ControlledConcurrencyBoundaryCheck CheckNoImplicitExecutionAuthority()
    {
        var methods = typeof(ControlledConcurrencyExecutionBoundary).GetMethods().Select(x => x.Name).ToArray();
        var passed = !methods.Any(x => x.Contains("Execute", StringComparison.OrdinalIgnoreCase) && x != nameof(ControlledConcurrencyExecutionBoundary.Evaluate));
        return new("NO_IMPLICIT_EXECUTION", passed, "The boundary evaluates admission; it does not invoke arbitrary case callbacks.");
    }
}
