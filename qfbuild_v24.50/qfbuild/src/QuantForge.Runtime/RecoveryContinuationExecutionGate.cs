using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

/// <summary>
/// Final execution-side fail-closed guard. A continuation delegate is callable only when
/// the recovery authorization result is explicitly Ready. The guard never retries or
/// transforms a blocked authorization into execution authority.
/// </summary>
public static class RecoveryContinuationExecutionGate
{
    public static Task ExecuteAsync(
        RecoveryContinuationResult authorization,
        Func<CancellationToken, Task> continuation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(continuation);
        if (authorization.State != RecoveryContinuationState.Ready)
            return Task.FromException(new InvalidOperationException("RECOVERY_CONTINUATION_NOT_AUTHORIZED"));

        return continuation(cancellationToken);
    }
}
