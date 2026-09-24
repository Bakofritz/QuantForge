using Microsoft.Data.Sqlite;

namespace QuantForge.Governance;

/// <summary>Transactional persistent audit store. It stores only audit records, never authentication secrets.</summary>
public sealed class TransactionalLiveAuditStoreV21_85 : ILiveTradingAuditSink, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _gate = new();
    public TransactionalLiveAuditStoreV21_85(string path)
    {
        _connection = new SqliteConnection($"Data Source={path}");
        _connection.Open();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS live_audit (event_id TEXT PRIMARY KEY, event TEXT NOT NULL, occurred_at TEXT NOT NULL, user_id TEXT NOT NULL, reason TEXT NOT NULL, settings_fp TEXT NOT NULL, session_id TEXT, correlation_id TEXT);";
        cmd.ExecuteNonQuery();
    }

    public void Append(LiveTradingAuditRecord record)
    {
        lock (_gate)
        {
            using var tx = _connection.BeginTransaction();
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO live_audit(event_id,event,occurred_at,user_id,reason,settings_fp,session_id,correlation_id) VALUES($id,$e,$a,$u,$r,$f,$s,$c)";
            cmd.Parameters.AddWithValue("$id", record.EventId);
            cmd.Parameters.AddWithValue("$e", record.Event.ToString());
            cmd.Parameters.AddWithValue("$a", record.OccurredAt.ToString("O"));
            cmd.Parameters.AddWithValue("$u", record.UserId);
            cmd.Parameters.AddWithValue("$r", record.Reason);
            cmd.Parameters.AddWithValue("$f", record.SettingsFingerprint);
            cmd.Parameters.AddWithValue("$s", (object?)record.SessionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$c", (object?)record.CorrelationId ?? DBNull.Value);
            cmd.ExecuteNonQuery();
            tx.Commit();
        }
    }

    public bool IsHealthy()
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand(); cmd.CommandText = "SELECT COUNT(*) FROM live_audit";
            _ = Convert.ToInt64(cmd.ExecuteScalar());
            return true;
        }
    }

    public void Dispose() => _connection.Dispose();
}
