using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace QuantForge.Governance;

public interface ISecureKeyStore
{
    bool IsAvailable { get; }
    void Store(string alias, byte[] keyMaterial);
    byte[]? Retrieve(string alias);
    void Delete(string alias);
}

public sealed class UnsupportedNativeSecureKeyStore : ISecureKeyStore
{
    public bool IsAvailable => false;
    public void Store(string alias, byte[] keyMaterial) => throw new PlatformNotSupportedException("Native secure key storage adapter is not configured for this platform.");
    public byte[]? Retrieve(string alias) => throw new PlatformNotSupportedException("Native secure key storage adapter is not configured for this platform.");
    public void Delete(string alias) => throw new PlatformNotSupportedException("Native secure key storage adapter is not configured for this platform.");
}

public interface IPlatformAuthenticationBridge
{
    string Platform { get; }
    bool IsAvailable { get; }
    AuthenticationVerification Verify(AuthenticationChallenge challenge, string platformProof, DateTimeOffset now);
}

public sealed class NativeAuthenticationBridge : IPlatformAuthenticationBridge
{
    private readonly ILiveAuthenticationAdapter _adapter;
    public NativeAuthenticationBridge(string platform, ILiveAuthenticationAdapter adapter, bool available = false)
    {
        Platform = platform;
        _adapter = adapter;
        IsAvailable = available;
    }
    public string Platform { get; }
    public bool IsAvailable { get; }
    public AuthenticationVerification Verify(AuthenticationChallenge challenge, string platformProof, DateTimeOffset now)
    {
        if (!IsAvailable) return new(false, null, "NATIVE_AUTH_UNAVAILABLE", $"Native authentication bridge for {Platform} is not configured.");
        return _adapter.Verify(challenge, platformProof, now);
    }
}

public sealed record CanonicalBrokerOrderRequest(
    string IdempotencyKey,
    string PreviewId,
    string OrderPayloadFingerprint,
    string SecurityBindingFingerprint,
    DateTimeOffset RequestedAt)
{
    public string CanonicalJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = false });
    public string RequestFingerprint() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalJson()))).ToLowerInvariant();
}

public static class LiveBrokerRequestSerializer
{
    public static CanonicalBrokerOrderRequest Serialize(LiveBrokerSubmissionRequest request) =>
        new(request.IdempotencyKey, request.PreviewId, request.OrderPayloadFingerprint, request.SecurityBindingFingerprint, request.RequestedAt);
}

public sealed class SqliteLiveExecutionIntentStore : ILiveExecutionIntentStore, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _gate = new();
    private bool _disposed;

    public SqliteLiveExecutionIntentStore(string databasePath)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared };
        _connection = new SqliteConnection(builder.ConnectionString);
        _connection.Open();
        using var command = _connection.CreateCommand();
        command.CommandText = @"CREATE TABLE IF NOT EXISTS live_execution_intents (
            idempotency_key TEXT PRIMARY KEY,
            intent_id TEXT NOT NULL,
            preview_id TEXT NOT NULL,
            preview_fingerprint TEXT NOT NULL,
            security_binding_fingerprint TEXT NOT NULL,
            order_payload_fingerprint TEXT NOT NULL,
            created_at TEXT NOT NULL,
            state INTEGER NOT NULL,
            external_order_id TEXT NULL,
            broker_request_fingerprint TEXT NULL
        );";
        command.ExecuteNonQuery();
    }

    public LiveExecutionIntent? GetByIdempotencyKey(string idempotencyKey)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT intent_id, preview_id, preview_fingerprint, security_binding_fingerprint, order_payload_fingerprint, created_at, state, external_order_id, broker_request_fingerprint FROM live_execution_intents WHERE idempotency_key=$key";
            command.Parameters.AddWithValue("$key", idempotencyKey);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            return new LiveExecutionIntent(reader.GetString(0), idempotencyKey, reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), DateTimeOffset.Parse(reader.GetString(5)), (LiveExecutionIntentState)reader.GetInt32(6), reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8));
        }
    }

    public LiveExecutionIntent Save(LiveExecutionIntent intent)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            using var tx = _connection.BeginTransaction();
            using var command = _connection.CreateCommand();
            command.Transaction = tx;
            command.CommandText = @"INSERT INTO live_execution_intents(idempotency_key,intent_id,preview_id,preview_fingerprint,security_binding_fingerprint,order_payload_fingerprint,created_at,state,external_order_id,broker_request_fingerprint) VALUES($key,$id,$preview,$pf,$sf,$of,$created,$state,$external,$broker)";
            Add(command, intent);
            try { command.ExecuteNonQuery(); tx.Commit(); return intent; }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { tx.Rollback(); throw new InvalidOperationException("IDEMPOTENCY_KEY_ALREADY_EXISTS", ex); }
        }
    }

    public LiveExecutionIntent Update(LiveExecutionIntent intent)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            using var tx = _connection.BeginTransaction();
            using var command = _connection.CreateCommand();
            command.Transaction = tx;
            command.CommandText = @"UPDATE live_execution_intents SET intent_id=$id,preview_id=$preview,preview_fingerprint=$pf,security_binding_fingerprint=$sf,order_payload_fingerprint=$of,created_at=$created,state=$state,external_order_id=$external,broker_request_fingerprint=$broker WHERE idempotency_key=$key";
            Add(command, intent);
            if (command.ExecuteNonQuery() != 1) { tx.Rollback(); throw new KeyNotFoundException("EXECUTION_INTENT_NOT_FOUND"); }
            tx.Commit();
            return intent;
        }
    }

    private static void Add(SqliteCommand command, LiveExecutionIntent intent)
    {
        command.Parameters.AddWithValue("$key", intent.IdempotencyKey); command.Parameters.AddWithValue("$id", intent.IntentId); command.Parameters.AddWithValue("$preview", intent.PreviewId); command.Parameters.AddWithValue("$pf", intent.PreviewFingerprint); command.Parameters.AddWithValue("$sf", intent.SecurityBindingFingerprint); command.Parameters.AddWithValue("$of", intent.OrderPayloadFingerprint); command.Parameters.AddWithValue("$created", intent.CreatedAt.ToString("O")); command.Parameters.AddWithValue("$state", (int)intent.State); command.Parameters.AddWithValue("$external", (object?)intent.ExternalOrderId ?? DBNull.Value); command.Parameters.AddWithValue("$broker", (object?)intent.BrokerRequestFingerprint ?? DBNull.Value);
    }
    private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(SqliteLiveExecutionIntentStore)); }
    public void Dispose() { lock (_gate) { if (_disposed) return; _disposed = true; _connection.Dispose(); } }
}

public sealed record LiveExecutionRecoveryDecision(bool SafeToProceed, string Code, string Reason, int UnknownIntentCount);

public sealed class LiveExecutionRecoveryGate
{
    private readonly ILiveExecutionIntentStore _store;
    public LiveExecutionRecoveryGate(ILiveExecutionIntentStore store) => _store = store;

    public LiveExecutionRecoveryDecision Inspect(IEnumerable<string> knownIdempotencyKeys)
    {
        var unknown = knownIdempotencyKeys.Select(_store.GetByIdempotencyKey).Where(x => x?.State == LiveExecutionIntentState.Unknown).Cast<LiveExecutionIntent>().ToArray();
        return unknown.Length == 0
            ? new(true, "EXECUTION_RECOVERY_CLEAR", "No ambiguous live execution intents require reconciliation.", 0)
            : new(false, "EXECUTION_RECOVERY_REQUIRED", "Ambiguous execution intents must be reconciled before new live execution is admitted.", unknown.Length);
    }
}
