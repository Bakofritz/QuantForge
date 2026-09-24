namespace QuantForge.Governance;

public sealed record LiveStartupRecoveryResult(bool Safe, string Code, string Reason, IReadOnlyList<LiveExecutionIntent> BlockingIntents);

/// <summary>Runs before live pathway admission after process startup or recovery.</summary>
public sealed class LiveExecutionStartupRecovery
{
    private readonly ILiveExecutionIntentStore _store;
    private readonly LiveExecutionReconciler _reconciler;

    public LiveExecutionStartupRecovery(ILiveExecutionIntentStore store, LiveExecutionReconciler reconciler)
    { _store = store; _reconciler = reconciler; }

    public LiveStartupRecoveryResult Inspect(IEnumerable<string> knownIdempotencyKeys)
    {
        var blocking = knownIdempotencyKeys
            .Select(_store.GetByIdempotencyKey)
            .Where(x => x is not null && x.State == LiveExecutionIntentState.Unknown)
            .Cast<LiveExecutionIntent>()
            .ToArray();
        return blocking.Length == 0
            ? new(true, "STARTUP_RECOVERY_CLEAR", "No ambiguous live execution intents are blocking startup.", blocking)
            : new(false, "STARTUP_RECONCILIATION_REQUIRED", "Live execution remains blocked until every ambiguous execution intent is reconciled with the broker.", blocking);
    }

    public LiveExecutionReconciliationResult ReconcileOne(string idempotencyKey, DateTimeOffset now)
        => _reconciler.Reconcile(idempotencyKey, now);
}
