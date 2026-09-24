using QuantForge.Core;

namespace QuantForge.Core.Runtime;

public enum AdmissionCaseState { Pending, Admitted, Running, CancelRequested, Completed, Failed, Canceled }

public sealed record AdmissionCaseLedger(
    string ContextId,
    string JobId,
    AdmissionCaseState State,
    string WorkerFingerprint,
    int HeartbeatRenewals,
    long ElapsedMilliseconds,
    string? ResultFingerprint,
    string? ErrorCode,
    string StateFingerprint);

public sealed record ConcurrencyBatchLedger(
    string BatchId,
    string BatchFingerprint,
    IReadOnlyList<AdmissionCaseLedger> Cases,
    long TotalElapsedMilliseconds,
    int TotalHeartbeatRenewals,
    int ActiveCaseCount,
    string AggregateFingerprint);

/// <summary>
/// Deterministic lifecycle state machine for controlled multi-case execution.
/// It is deliberately storage-agnostic: durable stores and real leases remain external boundaries.
/// No method starts user code or grants execution authority.
/// </summary>
public sealed class ControlledConcurrencyLifecycle
{
    public AdmissionCaseLedger Admit(AdmissionCaseLedger current, string workerFingerprint)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (string.IsNullOrWhiteSpace(workerFingerprint)) throw new ArgumentException("Worker identity is required.", nameof(workerFingerprint));
        if (current.State != AdmissionCaseState.Pending) throw new InvalidOperationException("CASE_NOT_PENDING");
        return Rebuild(current with { State = AdmissionCaseState.Admitted, WorkerFingerprint = workerFingerprint });
    }

    public AdmissionCaseLedger Start(AdmissionCaseLedger current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (current.State != AdmissionCaseState.Admitted) throw new InvalidOperationException("CASE_NOT_ADMITTED");
        return Rebuild(current with { State = AdmissionCaseState.Running });
    }

    public AdmissionCaseLedger RequestCancellation(AdmissionCaseLedger current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (current.State is AdmissionCaseState.Completed or AdmissionCaseState.Failed or AdmissionCaseState.Canceled)
            return current;
        if (current.State is not (AdmissionCaseState.Admitted or AdmissionCaseState.Running or AdmissionCaseState.CancelRequested))
            throw new InvalidOperationException("CASE_CANNOT_CANCEL");
        return Rebuild(current with { State = AdmissionCaseState.CancelRequested });
    }

    public AdmissionCaseLedger Complete(AdmissionCaseLedger current, string resultFingerprint, long elapsedMilliseconds, int heartbeatRenewals)
    {
        ValidateTerminalInputs(current, elapsedMilliseconds, heartbeatRenewals);
        if (current.State != AdmissionCaseState.Running) throw new InvalidOperationException("CASE_NOT_RUNNING");
        if (current.State == AdmissionCaseState.CancelRequested) throw new InvalidOperationException("CANCELLATION_ALREADY_REQUESTED");
        if (string.IsNullOrWhiteSpace(resultFingerprint)) throw new ArgumentException("Result fingerprint is required.", nameof(resultFingerprint));
        return Rebuild(current with { State = AdmissionCaseState.Completed, ResultFingerprint = resultFingerprint, ElapsedMilliseconds = elapsedMilliseconds, HeartbeatRenewals = heartbeatRenewals });
    }

    public AdmissionCaseLedger Fail(AdmissionCaseLedger current, string errorCode, long elapsedMilliseconds, int heartbeatRenewals)
    {
        ValidateTerminalInputs(current, elapsedMilliseconds, heartbeatRenewals);
        if (current.State is not (AdmissionCaseState.Running or AdmissionCaseState.CancelRequested)) throw new InvalidOperationException("CASE_NOT_RUNNING");
        if (string.IsNullOrWhiteSpace(errorCode)) throw new ArgumentException("Error code is required.", nameof(errorCode));
        return Rebuild(current with { State = AdmissionCaseState.Failed, ErrorCode = errorCode, ElapsedMilliseconds = elapsedMilliseconds, HeartbeatRenewals = heartbeatRenewals });
    }

    public AdmissionCaseLedger Cancel(AdmissionCaseLedger current, long elapsedMilliseconds, int heartbeatRenewals)
    {
        ValidateTerminalInputs(current, elapsedMilliseconds, heartbeatRenewals);
        if (current.State is not (AdmissionCaseState.Admitted or AdmissionCaseState.Running or AdmissionCaseState.CancelRequested)) throw new InvalidOperationException("CASE_CANNOT_CANCEL");
        return Rebuild(current with { State = AdmissionCaseState.Canceled, ErrorCode = "CANCELED", ElapsedMilliseconds = elapsedMilliseconds, HeartbeatRenewals = heartbeatRenewals });
    }

    public ConcurrencyBatchLedger Aggregate(string batchId, string batchFingerprint, IEnumerable<AdmissionCaseLedger> cases)
    {
        if (string.IsNullOrWhiteSpace(batchId)) throw new ArgumentException("Batch id is required.", nameof(batchId));
        if (string.IsNullOrWhiteSpace(batchFingerprint)) throw new ArgumentException("Batch fingerprint is required.", nameof(batchFingerprint));
        var items = cases?.OrderBy(x => x.ContextId, StringComparer.Ordinal).ToArray() ?? throw new ArgumentNullException(nameof(cases));
        if (items.Select(x => x.ContextId).Distinct(StringComparer.Ordinal).Count() != items.Length) throw new InvalidOperationException("DUPLICATE_CONTEXT");
        if (items.Any(x => x.State is AdmissionCaseState.Admitted or AdmissionCaseState.Running or AdmissionCaseState.CancelRequested))
            throw new InvalidOperationException("BATCH_NOT_TERMINAL");
        var aggregate = ResearchFingerprint.Sha256(string.Join("||", items.Select(x => x.StateFingerprint)));
        return new(batchId, batchFingerprint, items, items.Sum(x => x.ElapsedMilliseconds), items.Sum(x => x.HeartbeatRenewals), items.Count(x => x.State is AdmissionCaseState.Admitted or AdmissionCaseState.Running), aggregate);
    }

    private static void ValidateTerminalInputs(AdmissionCaseLedger current, long elapsedMilliseconds, int heartbeatRenewals)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (elapsedMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds));
        if (heartbeatRenewals < 0) throw new ArgumentOutOfRangeException(nameof(heartbeatRenewals));
    }

    private static AdmissionCaseLedger Rebuild(AdmissionCaseLedger value)
    {
        var fp = ResearchFingerprint.Sha256(string.Join("|", new[]
        {
            value.ContextId, value.JobId, value.State.ToString(), value.WorkerFingerprint,
            value.HeartbeatRenewals.ToString(), value.ElapsedMilliseconds.ToString(),
            value.ResultFingerprint ?? "", value.ErrorCode ?? ""
        }));
        return value with { StateFingerprint = fp };
    }
}
