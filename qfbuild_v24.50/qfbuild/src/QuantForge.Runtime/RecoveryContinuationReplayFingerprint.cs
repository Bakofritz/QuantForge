using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public static class RecoveryContinuationReplayFingerprint
{
    public static string For(RecoveryContinuationRequest request, CheckpointContinuationCursor cursor) =>
        ResearchFingerprint.Sha256($"QF-RECOVERY-CONTINUATION-OP-1|{request.BatchId}|{request.ContextId}|{request.JobId}|{request.Worker.IdentityFingerprint}|{request.ExpectedDatasetFingerprint}|{request.ExpectedConfigurationFingerprint}|{request.ExpectedEngineFingerprint}|{cursor.Sequence}|{cursor.Cursor}|{cursor.StateHash}");
}
