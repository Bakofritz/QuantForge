using QuantForge.Core;

namespace QuantForge.Core.Runtime;

public sealed record ControlledConcurrencyLifecycleCheck(string Name, bool Passed, string Detail);
public sealed record ControlledConcurrencyLifecycleResult(string HarnessVersion, bool Passed, IReadOnlyList<ControlledConcurrencyLifecycleCheck> Checks, string ResultFingerprint);

/// <summary>Deterministic state-machine validation for admission, cancellation, terminality and aggregation.</summary>
public sealed class ControlledConcurrencyLifecycleHarness
{
    public ControlledConcurrencyLifecycleResult Run()
    {
        var checks = new List<ControlledConcurrencyLifecycleCheck>
        {
            CheckAdmissionAndStart(),
            CheckCancellationTerminality(),
            CheckDuplicateContextRejection(),
            CheckNonTerminalAggregateRejection(),
            CheckDeterministicAggregate()
        };
        var fp = ResearchFingerprint.Sha256(string.Join("|", checks.Select(x => $"{x.Name}:{x.Passed}:{x.Detail}")));
        return new("QF-CONCURRENCY-LIFECYCLE-1", checks.All(x => x.Passed), checks.AsReadOnly(), fp);
    }

    private static AdmissionCaseLedger New(string id) => new(id, "job-" + id, AdmissionCaseState.Pending, "", 0, 0, null, null, "");
    private static ControlledConcurrencyLifecycle Engine => new();

    private static ControlledConcurrencyLifecycleCheck CheckAdmissionAndStart()
    {
        var c = Engine.Start(Engine.Admit(New("case-a"), "worker-a"));
        return new("ADMISSION_START_ORDER", c.State == AdmissionCaseState.Running && c.WorkerFingerprint == "worker-a", "Only admitted cases may enter Running state.");
    }

    private static ControlledConcurrencyLifecycleCheck CheckCancellationTerminality()
    {
        var c = Engine.RequestCancellation(Engine.Start(Engine.Admit(New("case-a"), "worker-a")));
        c = Engine.Cancel(c, 10, 1);
        var unchanged = Engine.RequestCancellation(c);
        var passed = c.State == AdmissionCaseState.Canceled && unchanged.State == AdmissionCaseState.Canceled;
        return new("CANCELLATION_TERMINALITY", passed, "Cancellation becomes terminal and remains idempotent.");
    }

    private static ControlledConcurrencyLifecycleCheck CheckDuplicateContextRejection()
    {
        var a = Engine.Cancel(Engine.Start(Engine.Admit(New("case-a"), "worker-a")), 1, 0);
        var b = Engine.Cancel(Engine.Start(Engine.Admit(New("case-a"), "worker-b")), 2, 0);
        try { Engine.Aggregate("batch", "fp", new[] { a, b }); return new("DUPLICATE_CONTEXT_REJECTION", false, "Duplicate contexts were accepted."); }
        catch (InvalidOperationException ex) { return new("DUPLICATE_CONTEXT_REJECTION", ex.Message == "DUPLICATE_CONTEXT", "Duplicate contexts are rejected before aggregation."); }
    }

    private static ControlledConcurrencyLifecycleCheck CheckNonTerminalAggregateRejection()
    {
        var c = Engine.Start(Engine.Admit(New("case-a"), "worker-a"));
        try { Engine.Aggregate("batch", "fp", new[] { c }); return new("NON_TERMINAL_AGGREGATE_REJECTION", false, "Running case was aggregated."); }
        catch (InvalidOperationException ex) { return new("NON_TERMINAL_AGGREGATE_REJECTION", ex.Message == "BATCH_NOT_TERMINAL", "Aggregation requires terminal case states."); }
    }

    private static ControlledConcurrencyLifecycleCheck CheckDeterministicAggregate()
    {
        var a = Engine.Complete(Engine.Start(Engine.Admit(New("case-a"), "worker-a")), "result-a", 10, 1);
        var b = Engine.Fail(Engine.Start(Engine.Admit(New("case-b"), "worker-b")), "FAILED", 20, 2);
        var x = Engine.Aggregate("batch", "fp", new[] { b, a });
        var y = Engine.Aggregate("batch", "fp", new[] { a, b });
        var passed = x.AggregateFingerprint == y.AggregateFingerprint && x.TotalElapsedMilliseconds == 30 && x.TotalHeartbeatRenewals == 3;
        return new("DETERMINISTIC_AGGREGATION", passed, "Terminal receipts aggregate independently of input arrival order.");
    }
}
