using Microsoft.Data.Sqlite;
using QuantForge.Data;
using QuantForge.Core;

namespace QuantForge.Storage;

public sealed class SqliteLocalStore : ILocalEvidenceStore, IResourceReceiptStore, ITerminalCommitStore, IRecoveryAuditStore, ILeaseInspectionStore, ILedgerEvidenceStore, IMarketDatasetStore, IJobLeaseStore, IResearchJobStore, IRiskSettingsStore, IConcurrencyValidationResultStore, IRecoveryPreflightStore, IRecoveryBindingReconciliationStore, IRecoveryBindingReconciliationVerificationStore, IRecoveryContinuationReplayStore, IRecoveryContinuationReconciliationStore, IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly StorageFaultInjector? _faultInjector;

    public SqliteLocalStore(string databasePath)
        : this(databasePath, null)
    {
    }

    internal SqliteLocalStore(string databasePath, StorageFaultInjector? faultInjector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        _connection = new SqliteConnection(builder.ConnectionString);
        _faultInjector = faultInjector;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await _connection.OpenAsync(cancellationToken);
            await using var command = _connection.CreateCommand();
            command.CommandText = """
                PRAGMA journal_mode=WAL;
                PRAGMA foreign_keys=ON;
                CREATE TABLE IF NOT EXISTS evidence (evidence_id TEXT PRIMARY KEY, content_hash TEXT NOT NULL, created_utc TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS research_events (event_id TEXT PRIMARY KEY, job_id TEXT NOT NULL, timestamp_utc TEXT NOT NULL, type TEXT NOT NULL, payload_hash TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS ledger_trade_evidence (job_id TEXT NOT NULL, trade_id TEXT NOT NULL, entry_utc TEXT NOT NULL, exit_utc TEXT NOT NULL, quantity INTEGER NOT NULL, entry_price TEXT NOT NULL, exit_price TEXT NOT NULL, gross_pnl TEXT NOT NULL, commission TEXT NOT NULL, slippage_cost TEXT NOT NULL, net_pnl TEXT NOT NULL, exit_reason TEXT NOT NULL, fingerprint TEXT NOT NULL, PRIMARY KEY(job_id, trade_id));
                CREATE TABLE IF NOT EXISTS ledger_accounting_events (job_id TEXT NOT NULL, sequence INTEGER NOT NULL, timestamp_utc TEXT NOT NULL, session_key TEXT NOT NULL, type TEXT NOT NULL, position_before INTEGER NOT NULL, position_after INTEGER NOT NULL, realized_pnl TEXT NOT NULL, daily_realized_pnl TEXT NOT NULL, equity TEXT NOT NULL, daily_loss_locked INTEGER NOT NULL, reference_id TEXT NOT NULL, details TEXT NOT NULL, fingerprint TEXT NOT NULL, PRIMARY KEY(job_id, sequence));
                CREATE TABLE IF NOT EXISTS job_leases (job_id TEXT PRIMARY KEY, device_id TEXT NOT NULL, acquired_utc TEXT NOT NULL, expires_utc TEXT NOT NULL, version INTEGER NOT NULL);
                CREATE TABLE IF NOT EXISTS market_datasets (dataset_id TEXT PRIMARY KEY, instrument TEXT NOT NULL, timeframe TEXT NOT NULL, content_hash TEXT NOT NULL, captured_utc TEXT NOT NULL, fidelity TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS market_bars (dataset_id TEXT NOT NULL, timestamp_utc TEXT NOT NULL, open TEXT NOT NULL, high TEXT NOT NULL, low TEXT NOT NULL, close TEXT NOT NULL, volume INTEGER NOT NULL, PRIMARY KEY(dataset_id, timestamp_utc), FOREIGN KEY(dataset_id) REFERENCES market_datasets(dataset_id) ON DELETE CASCADE);
                CREATE INDEX IF NOT EXISTS ix_market_bars_dataset_time ON market_bars(dataset_id, timestamp_utc);
                CREATE TABLE IF NOT EXISTS research_jobs (job_id TEXT PRIMARY KEY, state TEXT NOT NULL, dataset_fingerprint TEXT NOT NULL, configuration_fingerprint TEXT NOT NULL, engine_fingerprint TEXT NOT NULL, created_utc TEXT NOT NULL, updated_utc TEXT NOT NULL, failure_code TEXT NULL);
                CREATE TABLE IF NOT EXISTS research_checkpoints (job_id TEXT NOT NULL, sequence INTEGER NOT NULL, cursor INTEGER NOT NULL, dataset_fingerprint TEXT NOT NULL, configuration_fingerprint TEXT NOT NULL, engine_fingerprint TEXT NOT NULL, state_hash TEXT NOT NULL, saved_utc TEXT NOT NULL, PRIMARY KEY(job_id, sequence));
                CREATE TABLE IF NOT EXISTS recovery_continuation_operations (operation_fingerprint TEXT PRIMARY KEY, job_id TEXT NOT NULL, sequence INTEGER NOT NULL, cursor INTEGER NOT NULL, state_hash TEXT NOT NULL, state TEXT NOT NULL, next_cursor INTEGER NULL, result_state_hash TEXT NULL, completed INTEGER NULL, result_fingerprint TEXT NULL, created_utc TEXT NOT NULL, result_recorded_utc TEXT NULL);
                CREATE TABLE IF NOT EXISTS recovery_continuation_reconciliations (decision_fingerprint TEXT PRIMARY KEY, operation_fingerprint TEXT NOT NULL, job_id TEXT NOT NULL, sequence INTEGER NOT NULL, cursor INTEGER NOT NULL, state_hash TEXT NOT NULL, decision TEXT NOT NULL, evidence_fingerprint TEXT NOT NULL, reason TEXT NOT NULL, recorded_utc TEXT NOT NULL, resolved_by TEXT NULL, resolved_utc TEXT NULL);
                CREATE INDEX IF NOT EXISTS ix_recovery_continuation_reconciliations_operation ON recovery_continuation_reconciliations(operation_fingerprint);
                CREATE TABLE IF NOT EXISTS job_cancellations (job_id TEXT PRIMARY KEY, requested_utc TEXT NOT NULL, requested_by TEXT NOT NULL, reason TEXT NOT NULL);
                CREATE INDEX IF NOT EXISTS ix_research_checkpoints_job_sequence ON research_checkpoints(job_id, sequence DESC);
                CREATE TABLE IF NOT EXISTS research_progress (job_id TEXT NOT NULL, operation TEXT NOT NULL, cursor INTEGER NOT NULL, total INTEGER NOT NULL, dataset_fingerprint TEXT NOT NULL, configuration_fingerprint TEXT NOT NULL, engine_fingerprint TEXT NOT NULL, aggregation_state TEXT NOT NULL, progress_hash TEXT NOT NULL, saved_utc TEXT NOT NULL, PRIMARY KEY(job_id, operation));
                CREATE TABLE IF NOT EXISTS resource_execution_receipts (receipt_fingerprint TEXT PRIMARY KEY, batch_id TEXT NOT NULL, context_id TEXT NOT NULL, job_id TEXT NOT NULL, worker_id TEXT NOT NULL, worker_fingerprint TEXT NOT NULL, state TEXT NOT NULL, result_fingerprint TEXT NOT NULL, started_utc TEXT NOT NULL, completed_utc TEXT NOT NULL, elapsed_ms INTEGER NOT NULL, heartbeat_renewals INTEGER NOT NULL, resource_status TEXT NOT NULL, error_code TEXT NULL);
                CREATE TABLE IF NOT EXISTS batch_resource_records (batch_id TEXT PRIMARY KEY, worker_id TEXT NOT NULL, worker_fingerprint TEXT NOT NULL, batch_fingerprint TEXT NOT NULL, total_cases INTEGER NOT NULL, completed_cases INTEGER NOT NULL, failed_cases INTEGER NOT NULL, canceled_cases INTEGER NOT NULL, total_elapsed_ms INTEGER NOT NULL, total_heartbeat_renewals INTEGER NOT NULL, resource_status TEXT NOT NULL, record_fingerprint TEXT NOT NULL, updated_utc TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS recovery_audits (audit_fingerprint TEXT PRIMARY KEY, batch_id TEXT NOT NULL, context_id TEXT NOT NULL, job_id TEXT NOT NULL, worker_fingerprint TEXT NOT NULL, previous_state TEXT NOT NULL, reconciliation_state TEXT NOT NULL, reason TEXT NOT NULL, receipt_fingerprint TEXT NULL, recorded_utc TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS recovery_preflight_receipts (preflight_fingerprint TEXT PRIMARY KEY, batch_id TEXT NOT NULL, context_id TEXT NOT NULL, job_id TEXT NOT NULL, worker_device_id TEXT NOT NULL, worker_fingerprint TEXT NOT NULL, decision TEXT NOT NULL, reason TEXT NOT NULL, integrity_fingerprint TEXT NOT NULL, terminal_receipt_fingerprint TEXT NULL, checkpoint_sequence INTEGER NULL, checkpoint_cursor INTEGER NULL, checkpoint_hash TEXT NULL, accepted_lease_version INTEGER NULL, accepted_lease_fingerprint TEXT NULL, recorded_utc TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS concurrency_validation_results (result_fingerprint TEXT PRIMARY KEY, adapter_version TEXT NOT NULL, passed INTEGER NOT NULL, checks_json TEXT NOT NULL, uses_real_sqlite INTEGER NOT NULL, created_utc TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS strategy_sources (source_id TEXT PRIMARY KEY, language TEXT NOT NULL, content_hash TEXT NOT NULL, character_count INTEGER NOT NULL, acquired_utc TEXT NOT NULL, status TEXT NOT NULL, source_text TEXT NOT NULL, audit_json TEXT NOT NULL, audit_fingerprint TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS strategy_selections (source_id TEXT PRIMARY KEY, selection_fingerprint TEXT NOT NULL, selected_features_json TEXT NOT NULL, commit_requested INTEGER NOT NULL, created_utc TEXT NOT NULL, FOREIGN KEY(source_id) REFERENCES strategy_sources(source_id));
                CREATE TABLE IF NOT EXISTS canonical_strategy_models (strategy_id TEXT PRIMARY KEY, source_id TEXT NOT NULL, source_fingerprint TEXT NOT NULL, model_json TEXT NOT NULL, model_fingerprint TEXT NOT NULL, research_eligible INTEGER NOT NULL, live_deployment_eligible INTEGER NOT NULL, created_utc TEXT NOT NULL, FOREIGN KEY(source_id) REFERENCES strategy_sources(source_id));
                CREATE TABLE IF NOT EXISTS strategy_lineage (lineage_id TEXT PRIMARY KEY, source_id TEXT NOT NULL, event_type TEXT NOT NULL, subject_fingerprint TEXT NOT NULL, parent_fingerprint TEXT NOT NULL, created_utc TEXT NOT NULL, FOREIGN KEY(source_id) REFERENCES strategy_sources(source_id));
                CREATE TABLE IF NOT EXISTS canonical_strategy_semantics (strategy_id TEXT PRIMARY KEY, model_fingerprint TEXT NOT NULL, semantics_json TEXT NOT NULL, semantics_fingerprint TEXT NOT NULL, created_utc TEXT NOT NULL, FOREIGN KEY(strategy_id) REFERENCES canonical_strategy_models(strategy_id));
                CREATE TABLE IF NOT EXISTS risk_settings (profile_id TEXT PRIMARY KEY, settings_json TEXT NOT NULL, settings_fingerprint TEXT NOT NULL, created_utc TEXT NOT NULL);
                CREATE INDEX IF NOT EXISTS ix_strategy_lineage_source_time ON strategy_lineage(source_id, created_utc);
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
            await EnsureRecoveryPreflightColumnAsync("checkpoint_sequence", "INTEGER NULL", cancellationToken);
            await EnsureRecoveryPreflightColumnAsync("checkpoint_cursor", "INTEGER NULL", cancellationToken);
            await EnsureRecoveryPreflightColumnAsync("checkpoint_hash", "TEXT NULL", cancellationToken);
            await EnsureRecoveryPreflightColumnAsync("accepted_lease_version", "INTEGER NULL", cancellationToken);
            await EnsureRecoveryPreflightColumnAsync("accepted_lease_fingerprint", "TEXT NULL", cancellationToken);
        }
        finally { _gate.Release(); }
    }

    private async Task EnsureRecoveryPreflightColumnAsync(string columnName, string definition, CancellationToken cancellationToken)
    {
        await using var check = _connection.CreateCommand();
        check.CommandText = "SELECT 1 FROM pragma_table_info('recovery_preflight_receipts') WHERE name=$name;";
        check.Parameters.AddWithValue("$name", columnName);
        if (await check.ExecuteScalarAsync(cancellationToken) is not null) return;
        await using var alter = _connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE recovery_preflight_receipts ADD COLUMN {columnName} {definition};";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AppendEvidenceAsync(string evidenceId, string contentHash, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.EvidenceWrite);
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT content_hash FROM evidence WHERE evidence_id=$id;";
            check.Parameters.AddWithValue("$id", evidenceId);
            var existingHash = await check.ExecuteScalarAsync(cancellationToken);
            if (existingHash is string existing && !string.Equals(existing, contentHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Immutable evidence conflict detected for an existing evidence ID.");

            await using var command = _connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO evidence(evidence_id, content_hash, created_utc) VALUES($id,$hash,$utc);";
            command.Parameters.AddWithValue("$id", evidenceId);
            command.Parameters.AddWithValue("$hash", contentHash);
            command.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> ContainsEvidenceAsync(string evidenceId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM evidence WHERE evidence_id=$id LIMIT 1;";
            command.Parameters.AddWithValue("$id", evidenceId);
            return await command.ExecuteScalarAsync(cancellationToken) is not null;
        }
        finally { _gate.Release(); }
    }

    public async Task AppendEventAsync(string eventId, string jobId, string type, string payloadHash, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT payload_hash FROM research_events WHERE event_id=$id;";
            check.Parameters.AddWithValue("$id", eventId);
            var existingHash = await check.ExecuteScalarAsync(cancellationToken);
            if (existingHash is string existing && !string.Equals(existing, payloadHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Immutable research event conflict detected for an existing event ID.");

            await using var command = _connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO research_events(event_id,job_id,timestamp_utc,type,payload_hash) VALUES($id,$job,$utc,$type,$hash);";
            command.Parameters.AddWithValue("$id", eventId);
            command.Parameters.AddWithValue("$job", jobId);
            command.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
            command.Parameters.AddWithValue("$type", type);
            command.Parameters.AddWithValue("$hash", payloadHash);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task AppendTradeEvidenceAsync(string jobId, string tradeId, string entryUtc, string exitUtc, int quantity, decimal entryPrice, decimal exitPrice, decimal grossPnl, decimal commission, decimal slippageCost, decimal netPnl, string exitReason, string fingerprint, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT fingerprint FROM ledger_trade_evidence WHERE job_id=$job AND trade_id=$id;";
            check.Parameters.AddWithValue("$job", jobId); check.Parameters.AddWithValue("$id", tradeId);
            var existing = await check.ExecuteScalarAsync(cancellationToken);
            if (existing is string existingFingerprint && !string.Equals(existingFingerprint, fingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("Immutable trade evidence conflict detected for an existing job/trade ID.");

            await using var c = _connection.CreateCommand();
            c.CommandText = "INSERT OR IGNORE INTO ledger_trade_evidence(job_id,trade_id,entry_utc,exit_utc,quantity,entry_price,exit_price,gross_pnl,commission,slippage_cost,net_pnl,exit_reason,fingerprint) VALUES($job,$id,$entry,$exit,$qty,$ep,$xp,$gross,$comm,$slip,$net,$reason,$hash);";
            c.Parameters.AddWithValue("$job", jobId); c.Parameters.AddWithValue("$id", tradeId); c.Parameters.AddWithValue("$entry", entryUtc); c.Parameters.AddWithValue("$exit", exitUtc); c.Parameters.AddWithValue("$qty", quantity); c.Parameters.AddWithValue("$ep", entryPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)); c.Parameters.AddWithValue("$xp", exitPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)); c.Parameters.AddWithValue("$gross", grossPnl.ToString(System.Globalization.CultureInfo.InvariantCulture)); c.Parameters.AddWithValue("$comm", commission.ToString(System.Globalization.CultureInfo.InvariantCulture)); c.Parameters.AddWithValue("$slip", slippageCost.ToString(System.Globalization.CultureInfo.InvariantCulture)); c.Parameters.AddWithValue("$net", netPnl.ToString(System.Globalization.CultureInfo.InvariantCulture)); c.Parameters.AddWithValue("$reason", exitReason); c.Parameters.AddWithValue("$hash", fingerprint);
            await c.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task AppendLedgerAccountingEventAsync(string jobId, long sequence, string timestampUtc, string sessionKey, string type, int positionBefore, int positionAfter, decimal realizedPnl, decimal dailyRealizedPnl, decimal equity, bool dailyLossLocked, string referenceId, string details, string fingerprint, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT fingerprint FROM ledger_accounting_events WHERE job_id=$job AND sequence=$seq;";
            check.Parameters.AddWithValue("$job", jobId); check.Parameters.AddWithValue("$seq", sequence);
            var existing = await check.ExecuteScalarAsync(cancellationToken);
            if (existing is string existingFingerprint && !string.Equals(existingFingerprint, fingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("Immutable ledger event conflict detected for an existing job/event sequence.");

            await using var c = _connection.CreateCommand();
            c.CommandText = "INSERT OR IGNORE INTO ledger_accounting_events(job_id,sequence,timestamp_utc,session_key,type,position_before,position_after,realized_pnl,daily_realized_pnl,equity,daily_loss_locked,reference_id,details,fingerprint) VALUES($job,$seq,$utc,$session,$type,$before,$after,$realized,$daily,$equity,$locked,$ref,$details,$hash);";
            c.Parameters.AddWithValue("$job", jobId); c.Parameters.AddWithValue("$seq", sequence); c.Parameters.AddWithValue("$utc", timestampUtc); c.Parameters.AddWithValue("$session", sessionKey); c.Parameters.AddWithValue("$type", type); c.Parameters.AddWithValue("$before", positionBefore); c.Parameters.AddWithValue("$after", positionAfter); c.Parameters.AddWithValue("$realized", realizedPnl.ToString(System.Globalization.CultureInfo.InvariantCulture)); c.Parameters.AddWithValue("$daily", dailyRealizedPnl.ToString(System.Globalization.CultureInfo.InvariantCulture)); c.Parameters.AddWithValue("$equity", equity.ToString(System.Globalization.CultureInfo.InvariantCulture)); c.Parameters.AddWithValue("$locked", dailyLossLocked ? 1 : 0); c.Parameters.AddWithValue("$ref", referenceId); c.Parameters.AddWithValue("$details", details); c.Parameters.AddWithValue("$hash", fingerprint);
            await c.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }


    public async Task AppendConcurrencyValidationResultAsync(StorageConcurrencyValidationResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.ValidationResultWrite);
            var checksJson = System.Text.Json.JsonSerializer.Serialize(result.Checks);
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT adapter_version,passed,checks_json,uses_real_sqlite FROM concurrency_validation_results WHERE result_fingerprint=$id;";
            check.Parameters.AddWithValue("$id", result.ResultFingerprint);
            await using var reader = await check.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var same = string.Equals(reader.GetString(0), result.AdapterVersion, StringComparison.Ordinal) &&
                           reader.GetInt32(1) == (result.Passed ? 1 : 0) &&
                           string.Equals(reader.GetString(2), checksJson, StringComparison.Ordinal) &&
                           reader.GetInt32(3) == (result.UsesRealSqliteStorage ? 1 : 0);
                if (!same) throw new InvalidOperationException("Immutable concurrency validation result conflict detected.");
                return;
            }

            await using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO concurrency_validation_results(result_fingerprint,adapter_version,passed,checks_json,uses_real_sqlite,created_utc) VALUES($id,$version,$passed,$checks,$sqlite,$utc);";
            command.Parameters.AddWithValue("$id", result.ResultFingerprint);
            command.Parameters.AddWithValue("$version", result.AdapterVersion);
            command.Parameters.AddWithValue("$passed", result.Passed ? 1 : 0);
            command.Parameters.AddWithValue("$checks", checksJson);
            command.Parameters.AddWithValue("$sqlite", result.UsesRealSqliteStorage ? 1 : 0);
            command.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<StorageConcurrencyValidationResult?> LoadConcurrencyValidationResultAsync(string resultFingerprint, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = "SELECT adapter_version,passed,checks_json,uses_real_sqlite FROM concurrency_validation_results WHERE result_fingerprint=$id;";
            command.Parameters.AddWithValue("$id", resultFingerprint);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var checks = System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<StorageConcurrencyValidationCheck>>(reader.GetString(2)) ?? Array.Empty<StorageConcurrencyValidationCheck>();
            return new StorageConcurrencyValidationResult(reader.GetString(0), reader.GetInt32(1) == 1, checks, resultFingerprint, reader.GetInt32(3) == 1);
        }
        finally { _gate.Release(); }
    }

    public async Task AppendExecutionReceiptAsync(ResourceReceiptRecord receipt, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.ReceiptWrite);
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT batch_id,context_id,job_id,worker_id,worker_fingerprint,state,result_fingerprint,started_utc,completed_utc,elapsed_ms,heartbeat_renewals,resource_status,COALESCE(error_code,'') FROM resource_execution_receipts WHERE receipt_fingerprint=$id;";
            check.Parameters.AddWithValue("$id", receipt.ReceiptFingerprint);
            await using var reader = await check.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var existing = string.Join("|", Enumerable.Range(0, 13).Select(i => reader.GetValue(i)?.ToString() ?? ""));
                var incoming = string.Join("|", new object?[] { receipt.BatchId, receipt.ContextId, receipt.JobId, receipt.WorkerId, receipt.WorkerFingerprint, receipt.State, receipt.ResultFingerprint, receipt.StartedAt.ToString("O"), receipt.CompletedAt.ToString("O"), receipt.ElapsedMilliseconds, receipt.HeartbeatRenewals, receipt.ResourceStatus, receipt.ErrorCode ?? "" });
                if (!string.Equals(existing, incoming, StringComparison.Ordinal)) throw new InvalidOperationException("Immutable execution receipt conflict detected.");
                return;
            }
            await using var c = _connection.CreateCommand();
            c.CommandText = "INSERT INTO resource_execution_receipts(receipt_fingerprint,batch_id,context_id,job_id,worker_id,worker_fingerprint,state,result_fingerprint,started_utc,completed_utc,elapsed_ms,heartbeat_renewals,resource_status,error_code) VALUES($id,$batch,$context,$job,$worker,$wf,$state,$result,$start,$end,$elapsed,$beats,$status,$error);";
            c.Parameters.AddWithValue("$id", receipt.ReceiptFingerprint); c.Parameters.AddWithValue("$batch", receipt.BatchId); c.Parameters.AddWithValue("$context", receipt.ContextId); c.Parameters.AddWithValue("$job", receipt.JobId); c.Parameters.AddWithValue("$worker", receipt.WorkerId); c.Parameters.AddWithValue("$wf", receipt.WorkerFingerprint); c.Parameters.AddWithValue("$state", receipt.State); c.Parameters.AddWithValue("$result", receipt.ResultFingerprint); c.Parameters.AddWithValue("$start", receipt.StartedAt.ToString("O")); c.Parameters.AddWithValue("$end", receipt.CompletedAt.ToString("O")); c.Parameters.AddWithValue("$elapsed", receipt.ElapsedMilliseconds); c.Parameters.AddWithValue("$beats", receipt.HeartbeatRenewals); c.Parameters.AddWithValue("$status", receipt.ResourceStatus); c.Parameters.AddWithValue("$error", (object?)receipt.ErrorCode ?? DBNull.Value);
            await c.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<ResourceReceiptRecord?> LoadExecutionReceiptAsync(string receiptFingerprint, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var c = _connection.CreateCommand();
            c.CommandText = "SELECT batch_id,context_id,job_id,worker_id,worker_fingerprint,state,result_fingerprint,started_utc,completed_utc,elapsed_ms,heartbeat_renewals,resource_status,error_code FROM resource_execution_receipts WHERE receipt_fingerprint=$id;";
            c.Parameters.AddWithValue("$id", receiptFingerprint);
            await using var r = await c.ExecuteReaderAsync(cancellationToken);
            if (!await r.ReadAsync(cancellationToken)) return null;
            return new ResourceReceiptRecord(receiptFingerprint, r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6), DateTimeOffset.Parse(r.GetString(7)), DateTimeOffset.Parse(r.GetString(8)), r.GetInt64(9), r.GetInt32(10), r.GetString(11), r.IsDBNull(12) ? null : r.GetString(12));
        }
        finally { _gate.Release(); }
    }

    public async Task AppendBatchResourceRecordAsync(BatchResourceRecord record, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var c = _connection.CreateCommand();
            c.CommandText = "INSERT INTO batch_resource_records(batch_id,worker_id,worker_fingerprint,batch_fingerprint,total_cases,completed_cases,failed_cases,canceled_cases,total_elapsed_ms,total_heartbeat_renewals,resource_status,record_fingerprint,updated_utc) VALUES($id,$worker,$wf,$bf,$total,$complete,$failed,$canceled,$elapsed,$beats,$status,$hash,$utc) ON CONFLICT(batch_id) DO UPDATE SET worker_id=excluded.worker_id,worker_fingerprint=excluded.worker_fingerprint,batch_fingerprint=excluded.batch_fingerprint,total_cases=excluded.total_cases,completed_cases=excluded.completed_cases,failed_cases=excluded.failed_cases,canceled_cases=excluded.canceled_cases,total_elapsed_ms=excluded.total_elapsed_ms,total_heartbeat_renewals=excluded.total_heartbeat_renewals,resource_status=excluded.resource_status,record_fingerprint=excluded.record_fingerprint,updated_utc=excluded.updated_utc;";
            c.Parameters.AddWithValue("$id", record.BatchId); c.Parameters.AddWithValue("$worker", record.WorkerId); c.Parameters.AddWithValue("$wf", record.WorkerFingerprint); c.Parameters.AddWithValue("$bf", record.BatchFingerprint); c.Parameters.AddWithValue("$total", record.TotalCases); c.Parameters.AddWithValue("$complete", record.CompletedCases); c.Parameters.AddWithValue("$failed", record.FailedCases); c.Parameters.AddWithValue("$canceled", record.CanceledCases); c.Parameters.AddWithValue("$elapsed", record.TotalElapsedMilliseconds); c.Parameters.AddWithValue("$beats", record.TotalHeartbeatRenewals); c.Parameters.AddWithValue("$status", record.ResourceStatus); c.Parameters.AddWithValue("$hash", record.RecordFingerprint); c.Parameters.AddWithValue("$utc", record.UpdatedAt.ToString("O"));
            await c.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<ResourceReceiptRecord>> LoadExecutionReceiptsForJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var list = new List<ResourceReceiptRecord>();
            await using var c = _connection.CreateCommand();
            c.CommandText = "SELECT receipt_fingerprint,batch_id,context_id,job_id,worker_id,worker_fingerprint,state,result_fingerprint,started_utc,completed_utc,elapsed_ms,heartbeat_renewals,resource_status,error_code FROM resource_execution_receipts WHERE job_id=$job ORDER BY completed_utc;";
            c.Parameters.AddWithValue("$job", jobId);
            await using var r = await c.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
                list.Add(new ResourceReceiptRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6), r.GetString(7), DateTimeOffset.Parse(r.GetString(8)), DateTimeOffset.Parse(r.GetString(9)), r.GetInt64(10), r.GetInt32(11), r.GetString(12), r.IsDBNull(13) ? null : r.GetString(13)));
            return list;
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<ResourceReceiptRecord>> LoadExecutionReceiptsForBatchAsync(string batchId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var list = new List<ResourceReceiptRecord>();
            await using var c = _connection.CreateCommand();
            c.CommandText = "SELECT receipt_fingerprint,batch_id,context_id,job_id,worker_id,worker_fingerprint,state,result_fingerprint,started_utc,completed_utc,elapsed_ms,heartbeat_renewals,resource_status,error_code FROM resource_execution_receipts WHERE batch_id=$batch ORDER BY completed_utc, receipt_fingerprint;";
            c.Parameters.AddWithValue("$batch", batchId);
            await using var r = await c.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
                list.Add(new ResourceReceiptRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6), r.GetString(7), DateTimeOffset.Parse(r.GetString(8)), DateTimeOffset.Parse(r.GetString(9)), r.GetInt64(10), r.GetInt32(11), r.GetString(12), r.IsDBNull(13) ? null : r.GetString(13)));
            return list;
        }
        finally { _gate.Release(); }
    }

    public async Task<BatchResourceRecord?> LoadBatchResourceRecordAsync(string batchId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var c = _connection.CreateCommand(); c.CommandText = "SELECT worker_id,worker_fingerprint,batch_fingerprint,total_cases,completed_cases,failed_cases,canceled_cases,total_elapsed_ms,total_heartbeat_renewals,resource_status,record_fingerprint,updated_utc FROM batch_resource_records WHERE batch_id=$id;"; c.Parameters.AddWithValue("$id", batchId);
            await using var r = await c.ExecuteReaderAsync(cancellationToken); if (!await r.ReadAsync(cancellationToken)) return null;
            return new BatchResourceRecord(batchId, r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.GetInt32(4), r.GetInt32(5), r.GetInt32(6), r.GetInt64(7), r.GetInt32(8), r.GetString(9), r.GetString(10), DateTimeOffset.Parse(r.GetString(11)));
        }
        finally { _gate.Release(); }
    }

    public async Task SaveDatasetAsync(ImportedMarketDataset dataset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await _connection.BeginTransactionAsync(cancellationToken);
            await using (var existing = _connection.CreateCommand())
            {
                existing.Transaction = (SqliteTransaction)transaction;
                existing.CommandText = "SELECT content_hash,instrument,timeframe FROM market_datasets WHERE dataset_id=$id;";
                existing.Parameters.AddWithValue("$id", dataset.DatasetId);
                await using var reader = await existing.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var storedHash = reader.GetString(0);
                    var storedInstrument = reader.GetString(1);
                    var storedTimeframe = reader.GetString(2);
                    if (!string.Equals(storedHash, dataset.Fingerprint.ContentHash, StringComparison.Ordinal) ||
                        !string.Equals(storedInstrument, dataset.Instrument, StringComparison.Ordinal) ||
                        !string.Equals(storedTimeframe, dataset.Timeframe, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Dataset '{dataset.DatasetId}' already exists with a different immutable fingerprint or identity.");
                }
            }
            await using (var meta = _connection.CreateCommand())
            {
                meta.Transaction = (SqliteTransaction)transaction;
                meta.CommandText = "INSERT OR IGNORE INTO market_datasets(dataset_id,instrument,timeframe,content_hash,captured_utc,fidelity) VALUES($id,$instrument,$timeframe,$hash,$captured,$fidelity);";
                meta.Parameters.AddWithValue("$id", dataset.DatasetId);
                meta.Parameters.AddWithValue("$instrument", dataset.Instrument);
                meta.Parameters.AddWithValue("$timeframe", dataset.Timeframe);
                meta.Parameters.AddWithValue("$hash", dataset.Fingerprint.ContentHash);
                meta.Parameters.AddWithValue("$captured", dataset.Fingerprint.CapturedAt.ToString("O"));
                meta.Parameters.AddWithValue("$fidelity", dataset.Fidelity.Declaration);
                await meta.ExecuteNonQueryAsync(cancellationToken);
            }
            foreach (var bar in dataset.Bars)
            {
                await using var row = _connection.CreateCommand();
                row.Transaction = (SqliteTransaction)transaction;
                row.CommandText = "INSERT OR IGNORE INTO market_bars(dataset_id,timestamp_utc,open,high,low,close,volume) VALUES($id,$ts,$o,$h,$l,$c,$v);";
                row.Parameters.AddWithValue("$id", dataset.DatasetId);
                row.Parameters.AddWithValue("$ts", bar.Timestamp.ToString("O"));
                row.Parameters.AddWithValue("$o", bar.Open.ToString(System.Globalization.CultureInfo.InvariantCulture));
                row.Parameters.AddWithValue("$h", bar.High.ToString(System.Globalization.CultureInfo.InvariantCulture));
                row.Parameters.AddWithValue("$l", bar.Low.ToString(System.Globalization.CultureInfo.InvariantCulture));
                row.Parameters.AddWithValue("$c", bar.Close.ToString(System.Globalization.CultureInfo.InvariantCulture));
                row.Parameters.AddWithValue("$v", bar.Volume);
                await row.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<ImportedMarketDataset?> LoadDatasetAsync(string datasetId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            string? instrument = null, timeframe = null, hash = null, capturedText = null, fidelityText = null;
            await using (var meta = _connection.CreateCommand())
            {
                meta.CommandText = "SELECT instrument,timeframe,content_hash,captured_utc,fidelity FROM market_datasets WHERE dataset_id=$id;";
                meta.Parameters.AddWithValue("$id", datasetId);
                await using var reader = await meta.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken)) return null;
                instrument = reader.GetString(0); timeframe = reader.GetString(1); hash = reader.GetString(2); capturedText = reader.GetString(3); fidelityText = reader.GetString(4);
            }
            var bars = new List<MarketBar>();
            await using (var rows = _connection.CreateCommand())
            {
                rows.CommandText = "SELECT timestamp_utc,open,high,low,close,volume FROM market_bars WHERE dataset_id=$id ORDER BY timestamp_utc;";
                rows.Parameters.AddWithValue("$id", datasetId);
                await using var reader = await rows.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    bars.Add(new MarketBar(DateTimeOffset.Parse(reader.GetString(0), null, System.Globalization.DateTimeStyles.RoundtripKind),
                        decimal.Parse(reader.GetString(1), System.Globalization.CultureInfo.InvariantCulture),
                        decimal.Parse(reader.GetString(2), System.Globalization.CultureInfo.InvariantCulture),
                        decimal.Parse(reader.GetString(3), System.Globalization.CultureInfo.InvariantCulture),
                        decimal.Parse(reader.GetString(4), System.Globalization.CultureInfo.InvariantCulture),
                        reader.GetInt64(5)));
                }
            }
            var captured = DateTimeOffset.Parse(capturedText!, null, System.Globalization.DateTimeStyles.RoundtripKind);
            var normalized = string.Join('\n', bars.Select(b =>
                $"{b.Timestamp:O}|{b.Open.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{b.High.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{b.Low.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{b.Close.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{b.Volume}"));
            var reconstructedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
            if (!string.Equals(reconstructedHash, hash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Stored dataset '{datasetId}' failed fingerprint verification.");
            return new ImportedMarketDataset(datasetId, instrument!, timeframe!, bars, new MarketDataFingerprint(datasetId,instrument!,timeframe!,hash!,captured), new DataFidelityProfile(true,false,false,false,fidelityText!));
        }
        finally { _gate.Release(); }
    }

    public async Task<JobLeaseRecord?> LoadLeaseAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var c = _connection.CreateCommand();
            c.CommandText = "SELECT device_id,acquired_utc,expires_utc,version FROM job_leases WHERE job_id=$job;";
            c.Parameters.AddWithValue("$job", jobId);
            await using var r = await c.ExecuteReaderAsync(cancellationToken);
            if (!await r.ReadAsync(cancellationToken)) return null;
            return new JobLeaseRecord(jobId, r.GetString(0), DateTimeOffset.Parse(r.GetString(1)), DateTimeOffset.Parse(r.GetString(2)), r.GetInt64(3));
        }
        finally { _gate.Release(); }
    }

    public async Task AppendRecoveryPreflightAsync(RecoveryPreflightReceipt receipt, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT batch_id,context_id,job_id,worker_device_id,worker_fingerprint,decision,reason,integrity_fingerprint,COALESCE(terminal_receipt_fingerprint,''),checkpoint_sequence,checkpoint_cursor,COALESCE(checkpoint_hash,''),accepted_lease_version,COALESCE(accepted_lease_fingerprint,''),recorded_utc FROM recovery_preflight_receipts WHERE preflight_fingerprint=$id;";
            check.Parameters.AddWithValue("$id", receipt.PreflightFingerprint);
            await using var r = await check.ExecuteReaderAsync(cancellationToken);
            if (await r.ReadAsync(cancellationToken))
            {
                var existing = string.Join("|", Enumerable.Range(0, 14).Select(i => r.GetValue(i)?.ToString() ?? ""));
                var incoming = string.Join("|", new object?[] { receipt.BatchId, receipt.ContextId, receipt.JobId, receipt.WorkerDeviceId, receipt.WorkerFingerprint, receipt.Decision, receipt.Reason, receipt.IntegrityFingerprint, receipt.TerminalReceiptFingerprint ?? "", receipt.CheckpointSequence?.ToString() ?? "", receipt.CheckpointCursor?.ToString() ?? "", receipt.CheckpointStateHash ?? "", receipt.AcceptedLeaseVersion?.ToString() ?? "", receipt.AcceptedLeaseFingerprint ?? "" });
                if (!string.Equals(existing, incoming, StringComparison.Ordinal)) throw new InvalidOperationException("Immutable recovery preflight conflict detected.");
                return;
            }
            await using var c = _connection.CreateCommand();
            c.CommandText = "INSERT INTO recovery_preflight_receipts(preflight_fingerprint,batch_id,context_id,job_id,worker_device_id,worker_fingerprint,decision,reason,integrity_fingerprint,terminal_receipt_fingerprint,checkpoint_sequence,checkpoint_cursor,checkpoint_hash,accepted_lease_version,accepted_lease_fingerprint,recorded_utc) VALUES($id,$batch,$context,$job,$device,$wf,$decision,$reason,$integrity,$terminal,$checkpoint_sequence,$checkpoint_cursor,$checkpoint_hash,$lease_version,$lease_fingerprint,$utc);";
            c.Parameters.AddWithValue("$id", receipt.PreflightFingerprint); c.Parameters.AddWithValue("$batch", receipt.BatchId); c.Parameters.AddWithValue("$context", receipt.ContextId); c.Parameters.AddWithValue("$job", receipt.JobId); c.Parameters.AddWithValue("$device", receipt.WorkerDeviceId); c.Parameters.AddWithValue("$wf", receipt.WorkerFingerprint); c.Parameters.AddWithValue("$decision", receipt.Decision); c.Parameters.AddWithValue("$reason", receipt.Reason); c.Parameters.AddWithValue("$integrity", receipt.IntegrityFingerprint); c.Parameters.AddWithValue("$terminal", (object?)receipt.TerminalReceiptFingerprint ?? DBNull.Value); c.Parameters.AddWithValue("$checkpoint_sequence", (object?)receipt.CheckpointSequence ?? DBNull.Value); c.Parameters.AddWithValue("$checkpoint_cursor", (object?)receipt.CheckpointCursor ?? DBNull.Value); c.Parameters.AddWithValue("$checkpoint_hash", (object?)receipt.CheckpointStateHash ?? DBNull.Value); c.Parameters.AddWithValue("$lease_version", (object?)receipt.AcceptedLeaseVersion ?? DBNull.Value); c.Parameters.AddWithValue("$lease_fingerprint", (object?)receipt.AcceptedLeaseFingerprint ?? DBNull.Value); c.Parameters.AddWithValue("$utc", receipt.RecordedAt.ToString("O"));
            await c.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<RecoveryPreflightReceipt?> LoadRecoveryPreflightAsync(string preflightFingerprint, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var c = _connection.CreateCommand();
            c.CommandText = "SELECT batch_id,context_id,job_id,worker_device_id,worker_fingerprint,decision,reason,integrity_fingerprint,terminal_receipt_fingerprint,checkpoint_sequence,checkpoint_cursor,checkpoint_hash,accepted_lease_version,accepted_lease_fingerprint,recorded_utc FROM recovery_preflight_receipts WHERE preflight_fingerprint=$id;";
            c.Parameters.AddWithValue("$id", preflightFingerprint);
            await using var r = await c.ExecuteReaderAsync(cancellationToken);
            if (!await r.ReadAsync(cancellationToken)) return null;
            return new RecoveryPreflightReceipt(preflightFingerprint, r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6), r.GetString(7), r.IsDBNull(8) ? null : r.GetString(8), r.IsDBNull(9) ? null : r.GetInt64(9), r.IsDBNull(10) ? null : r.GetInt32(10), r.IsDBNull(11) ? null : r.GetString(11), r.IsDBNull(12) ? null : r.GetInt64(12), r.IsDBNull(13) ? null : r.GetString(13), DateTimeOffset.Parse(r.GetString(14)));
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<RecoveryPreflightReceipt>> LoadRecoveryPreflightsForJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var list = new List<RecoveryPreflightReceipt>();
            await using var c = _connection.CreateCommand();
            c.CommandText = "SELECT preflight_fingerprint,batch_id,context_id,job_id,worker_device_id,worker_fingerprint,decision,reason,integrity_fingerprint,terminal_receipt_fingerprint,checkpoint_sequence,checkpoint_cursor,checkpoint_hash,accepted_lease_version,accepted_lease_fingerprint,recorded_utc FROM recovery_preflight_receipts WHERE job_id=$job ORDER BY recorded_utc, preflight_fingerprint;";
            c.Parameters.AddWithValue("$job", jobId);
            await using var r = await c.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken)) list.Add(new RecoveryPreflightReceipt(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6), r.GetString(7), r.GetString(8), r.IsDBNull(9) ? null : r.GetString(9), r.IsDBNull(10) ? null : r.GetInt64(10), r.IsDBNull(11) ? null : r.GetInt32(11), r.IsDBNull(12) ? null : r.GetString(12), r.IsDBNull(13) ? null : r.GetInt64(13), r.IsDBNull(14) ? null : r.GetString(14), DateTimeOffset.Parse(r.GetString(15))));
            return list;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> VerifyRecoveryBindingReconciliationAsync(
        string auditFingerprint,
        string evidenceId,
        string eventId,
        string eventPayloadHash,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var audit = _connection.CreateCommand();
            audit.CommandText = "SELECT COUNT(1) FROM recovery_audits WHERE audit_fingerprint=$id;";
            audit.Parameters.AddWithValue("$id", auditFingerprint);
            var auditExists = Convert.ToInt64(await audit.ExecuteScalarAsync(cancellationToken)) == 1;

            await using var evidence = _connection.CreateCommand();
            evidence.CommandText = "SELECT content_hash FROM evidence WHERE evidence_id=$id;";
            evidence.Parameters.AddWithValue("$id", evidenceId);
            var evidenceHash = await evidence.ExecuteScalarAsync(cancellationToken);
            var evidenceExists = evidenceHash is string;

            await using var evt = _connection.CreateCommand();
            evt.CommandText = "SELECT payload_hash FROM research_events WHERE event_id=$id;";
            evt.Parameters.AddWithValue("$id", eventId);
            var storedPayload = await evt.ExecuteScalarAsync(cancellationToken);
            var eventExists = storedPayload is string stored && string.Equals(stored, eventPayloadHash, StringComparison.Ordinal);

            return auditExists && evidenceExists && eventExists;
        }
        finally { _gate.Release(); }
    }

    public async Task AppendRecoveryBindingReconciliationAtomicallyAsync(
        RecoveryAuditRecord audit,
        string evidenceId,
        string evidenceHash,
        string eventId,
        string eventJobId,
        string eventType,
        string eventPayloadHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventJobId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventPayloadHash);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await _connection.BeginTransactionAsync(cancellationToken);
            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.RecoveryAuditWrite);
            await using (var auditCheck = _connection.CreateCommand())
            {
                auditCheck.Transaction = (SqliteTransaction)transaction;
                auditCheck.CommandText = "SELECT batch_id,context_id,job_id,worker_fingerprint,previous_state,reconciliation_state,reason,COALESCE(receipt_fingerprint,''),recorded_utc FROM recovery_audits WHERE audit_fingerprint=$id;";
                auditCheck.Parameters.AddWithValue("$id", audit.AuditFingerprint);
                await using var r = await auditCheck.ExecuteReaderAsync(cancellationToken);
                if (await r.ReadAsync(cancellationToken))
                {
                    var existing = string.Join("|", Enumerable.Range(0,9).Select(i => r.GetValue(i)?.ToString() ?? ""));
                    var incoming = string.Join("|", new object?[] { audit.BatchId, audit.ContextId, audit.JobId, audit.WorkerFingerprint, audit.PreviousState, audit.ReconciliationState, audit.Reason, audit.ReceiptFingerprint ?? "", audit.RecordedAt.ToString("O") });
                    if (!string.Equals(existing, incoming, StringComparison.Ordinal))
                        throw new InvalidOperationException("Immutable recovery audit conflict detected.");
                }
                else
                {
                    await r.DisposeAsync();
                    await using var insert = _connection.CreateCommand();
                    insert.Transaction = (SqliteTransaction)transaction;
                    insert.CommandText = "INSERT INTO recovery_audits(audit_fingerprint,batch_id,context_id,job_id,worker_fingerprint,previous_state,reconciliation_state,reason,receipt_fingerprint,recorded_utc) VALUES($id,$batch,$context,$job,$wf,$prev,$state,$reason,$receipt,$utc);";
                    insert.Parameters.AddWithValue("$id", audit.AuditFingerprint); insert.Parameters.AddWithValue("$batch", audit.BatchId); insert.Parameters.AddWithValue("$context", audit.ContextId); insert.Parameters.AddWithValue("$job", audit.JobId); insert.Parameters.AddWithValue("$wf", audit.WorkerFingerprint); insert.Parameters.AddWithValue("$prev", audit.PreviousState); insert.Parameters.AddWithValue("$state", audit.ReconciliationState); insert.Parameters.AddWithValue("$reason", audit.Reason); insert.Parameters.AddWithValue("$receipt", (object?)audit.ReceiptFingerprint ?? DBNull.Value); insert.Parameters.AddWithValue("$utc", audit.RecordedAt.ToString("O"));
                    await insert.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.EvidenceWrite);
            await using (var evidenceCheck = _connection.CreateCommand())
            {
                evidenceCheck.Transaction = (SqliteTransaction)transaction;
                evidenceCheck.CommandText = "SELECT content_hash FROM evidence WHERE evidence_id=$id;";
                evidenceCheck.Parameters.AddWithValue("$id", evidenceId);
                var existingHash = await evidenceCheck.ExecuteScalarAsync(cancellationToken);
                if (existingHash is string existing && !string.Equals(existing, evidenceHash, StringComparison.Ordinal))
                    throw new InvalidOperationException("Immutable evidence conflict detected for an existing evidence ID.");
                await using var insert = _connection.CreateCommand();
                insert.Transaction = (SqliteTransaction)transaction;
                insert.CommandText = "INSERT OR IGNORE INTO evidence(evidence_id,content_hash,created_utc) VALUES($id,$hash,$utc);";
                insert.Parameters.AddWithValue("$id", evidenceId); insert.Parameters.AddWithValue("$hash", evidenceHash); insert.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.TerminalEventWrite);
            await using (var eventCheck = _connection.CreateCommand())
            {
                eventCheck.Transaction = (SqliteTransaction)transaction;
                eventCheck.CommandText = "SELECT payload_hash FROM research_events WHERE event_id=$id;";
                eventCheck.Parameters.AddWithValue("$id", eventId);
                var existingHash = await eventCheck.ExecuteScalarAsync(cancellationToken);
                if (existingHash is string existing && !string.Equals(existing, eventPayloadHash, StringComparison.Ordinal))
                    throw new InvalidOperationException("Immutable research event conflict detected for an existing event ID.");
                await using var insert = _connection.CreateCommand();
                insert.Transaction = (SqliteTransaction)transaction;
                insert.CommandText = "INSERT OR IGNORE INTO research_events(event_id,job_id,timestamp_utc,type,payload_hash) VALUES($id,$job,$utc,$type,$hash);";
                insert.Parameters.AddWithValue("$id", eventId); insert.Parameters.AddWithValue("$job", eventJobId); insert.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O")); insert.Parameters.AddWithValue("$type", eventType); insert.Parameters.AddWithValue("$hash", eventPayloadHash);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // The transaction is disposed without commit, so partial reconciliation writes are rolled back.
            throw;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> ContainsResearchEventAsync(string eventId, string payloadHash, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = "SELECT payload_hash FROM research_events WHERE event_id=$id LIMIT 1;";
            command.Parameters.AddWithValue("$id", eventId);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            return value is string existing && string.Equals(existing, payloadHash, StringComparison.Ordinal);
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> ContainsRecoveryAuditAsync(string auditFingerprint, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM recovery_audits WHERE audit_fingerprint=$id LIMIT 1;";
            command.Parameters.AddWithValue("$id", auditFingerprint);
            return await command.ExecuteScalarAsync(cancellationToken) is not null;
        }
        finally { _gate.Release(); }
    }

    public async Task AppendRecoveryAuditAsync(RecoveryAuditRecord record, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT batch_id,context_id,job_id,worker_fingerprint,previous_state,reconciliation_state,reason,COALESCE(receipt_fingerprint,''),recorded_utc FROM recovery_audits WHERE audit_fingerprint=$id;";
            check.Parameters.AddWithValue("$id", record.AuditFingerprint);
            await using var r = await check.ExecuteReaderAsync(cancellationToken);
            if (await r.ReadAsync(cancellationToken))
            {
                var existing = string.Join("|", Enumerable.Range(0,9).Select(i => r.GetValue(i)?.ToString() ?? ""));
                var incoming = string.Join("|", new object?[] { record.BatchId, record.ContextId, record.JobId, record.WorkerFingerprint, record.PreviousState, record.ReconciliationState, record.Reason, record.ReceiptFingerprint ?? "", record.RecordedAt.ToString("O") });
                if (!string.Equals(existing, incoming, StringComparison.Ordinal)) throw new InvalidOperationException("Immutable recovery audit conflict detected.");
                return;
            }
            await using var c = _connection.CreateCommand();
            c.CommandText = "INSERT INTO recovery_audits(audit_fingerprint,batch_id,context_id,job_id,worker_fingerprint,previous_state,reconciliation_state,reason,receipt_fingerprint,recorded_utc) VALUES($id,$batch,$context,$job,$wf,$prev,$state,$reason,$receipt,$utc);";
            c.Parameters.AddWithValue("$id", record.AuditFingerprint); c.Parameters.AddWithValue("$batch", record.BatchId); c.Parameters.AddWithValue("$context", record.ContextId); c.Parameters.AddWithValue("$job", record.JobId); c.Parameters.AddWithValue("$wf", record.WorkerFingerprint); c.Parameters.AddWithValue("$prev", record.PreviousState); c.Parameters.AddWithValue("$state", record.ReconciliationState); c.Parameters.AddWithValue("$reason", record.Reason); c.Parameters.AddWithValue("$receipt", (object?)record.ReceiptFingerprint ?? DBNull.Value); c.Parameters.AddWithValue("$utc", record.RecordedAt.ToString("O"));
            await c.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> TryAcquireAsync(string jobId, string deviceId, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        => await MutateLeaseAsync(jobId, deviceId, leaseDuration, false, cancellationToken);

    public async Task<bool> RenewAsync(string jobId, string deviceId, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        => await MutateLeaseAsync(jobId, deviceId, leaseDuration, true, cancellationToken);

    private async Task<bool> MutateLeaseAsync(string jobId, string deviceId, TimeSpan leaseDuration, bool renewOnly, CancellationToken cancellationToken)
    {
        if (leaseDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await _connection.BeginTransactionAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow; var expiry = now.Add(leaseDuration);
            await using var command = _connection.CreateCommand(); command.Transaction=(SqliteTransaction)transaction;
            command.CommandText = renewOnly
                ? "UPDATE job_leases SET expires_utc=$expires,version=version+1 WHERE job_id=$job AND device_id=$device AND expires_utc>$now;"
                : "INSERT INTO job_leases(job_id,device_id,acquired_utc,expires_utc,version) VALUES($job,$device,$now,$expires,1) ON CONFLICT(job_id) DO UPDATE SET device_id=$device,acquired_utc=$now,expires_utc=$expires,version=job_leases.version+1 WHERE job_leases.expires_utc<=$now OR job_leases.device_id=$device;";
            command.Parameters.AddWithValue("$job",jobId); command.Parameters.AddWithValue("$device",deviceId); command.Parameters.AddWithValue("$now",now.ToString("O")); command.Parameters.AddWithValue("$expires",expiry.ToString("O"));
            var changed=await command.ExecuteNonQueryAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return changed>0;
        }
        finally { _gate.Release(); }
    }

    public async Task ReleaseAsync(string jobId, string deviceId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { await using var command=_connection.CreateCommand(); command.CommandText="DELETE FROM job_leases WHERE job_id=$job AND device_id=$device;"; command.Parameters.AddWithValue("$job",jobId); command.Parameters.AddWithValue("$device",deviceId); await command.ExecuteNonQueryAsync(cancellationToken); }
        finally { _gate.Release(); }
    }


    public async Task CommitTerminalOutcomeAsync(TerminalCommitRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await _connection.BeginTransactionAsync(cancellationToken);
            var tx = (SqliteTransaction)transaction;
            var now = DateTimeOffset.UtcNow.ToString("O");

            if (!TerminalCommitRules.IsTerminal(request.Receipt.State))
                throw new TerminalCommitConflictException("TERMINAL_COMMIT_NONTERMINAL_RECEIPT", "Terminal commit requires a terminal receipt state.");

            // Idempotency/conflict gate: the same durable outcome may be safely retried,
            // but a different outcome for the same job is an integrity conflict.
            await using (var existing = _connection.CreateCommand())
            {
                existing.Transaction = tx;
                existing.CommandText = "SELECT receipt_fingerprint,batch_id,context_id,job_id,worker_id,worker_fingerprint,state,result_fingerprint,started_utc,completed_utc,elapsed_ms,heartbeat_renewals,resource_status,COALESCE(error_code,'') FROM resource_execution_receipts WHERE receipt_fingerprint=$id OR job_id=$job ORDER BY CASE WHEN receipt_fingerprint=$id THEN 0 ELSE 1 END LIMIT 1;";
                existing.Parameters.AddWithValue("$id", request.Receipt.ReceiptFingerprint);
                existing.Parameters.AddWithValue("$job", request.Receipt.JobId);
                await using var reader = await existing.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var stored = new TerminalCommitReceipt(
                        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                        reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
                        DateTimeOffset.Parse(reader.GetString(8)), DateTimeOffset.Parse(reader.GetString(9)),
                        reader.GetInt64(10), reader.GetInt32(11), reader.GetString(12), reader.IsDBNull(13) ? null : reader.GetString(13));
                    if (TerminalCommitRules.ReceiptMatches(stored, request.Receipt))
                        return;
                    throw new TerminalCommitConflictException(
                        "TERMINAL_COMMIT_RESULT_CONFLICT",
                        $"A different terminal outcome is already committed for job '{request.Receipt.JobId}'.");
                }
            }

            var existingJobState = await LoadJobStateForTerminalCommitAsync(tx, request.Job.JobId, cancellationToken);
            if (existingJobState is null)
                throw new TerminalCommitConflictException("TERMINAL_COMMIT_JOB_MISSING", "Terminal commit requires an existing research job.");
            if (TerminalCommitRules.IsTerminal(existingJobState) && !string.Equals(existingJobState, request.Job.State.ToString(), StringComparison.Ordinal))
                throw new TerminalCommitConflictException("TERMINAL_COMMIT_STATE_CONFLICT", "The job is already terminal with a different state.");

            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.JobStateWrite);
            await using (var state = _connection.CreateCommand())
            {
                state.Transaction = tx;
                state.CommandText = "UPDATE research_jobs SET state=$state,updated_utc=$updated,failure_code=$failure WHERE job_id=$job AND EXISTS (SELECT 1 FROM job_leases WHERE job_id=$job AND device_id=$device AND expires_utc>$now);";
                state.Parameters.AddWithValue("$state", request.Job.State.ToString());
                state.Parameters.AddWithValue("$updated", request.Job.UpdatedAt.ToString("O"));
                state.Parameters.AddWithValue("$failure", (object?)request.Job.FailureCode ?? DBNull.Value);
                state.Parameters.AddWithValue("$job", request.Job.JobId);
                state.Parameters.AddWithValue("$device", request.Receipt.WorkerId);
                state.Parameters.AddWithValue("$now", now);
                if (await state.ExecuteNonQueryAsync(cancellationToken) != 1)
                    throw new InvalidOperationException("TERMINAL_COMMIT_LEASE_OR_JOB_STATE_INVALID");
            }

            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.ReceiptWrite);
            await using (var receipt = _connection.CreateCommand())
            {
                receipt.Transaction = tx;
                receipt.CommandText = "INSERT INTO resource_execution_receipts(receipt_fingerprint,batch_id,context_id,job_id,worker_id,worker_fingerprint,state,result_fingerprint,started_utc,completed_utc,elapsed_ms,heartbeat_renewals,resource_status,error_code) VALUES($id,$batch,$context,$job,$worker,$wf,$state,$result,$start,$end,$elapsed,$beats,$status,$error) ON CONFLICT(receipt_fingerprint) DO NOTHING;";
                receipt.Parameters.AddWithValue("$id", request.Receipt.ReceiptFingerprint);
                receipt.Parameters.AddWithValue("$batch", request.Receipt.BatchId); receipt.Parameters.AddWithValue("$context", request.Receipt.ContextId); receipt.Parameters.AddWithValue("$job", request.Receipt.JobId); receipt.Parameters.AddWithValue("$worker", request.Receipt.WorkerId); receipt.Parameters.AddWithValue("$wf", request.Receipt.WorkerFingerprint); receipt.Parameters.AddWithValue("$state", request.Receipt.State); receipt.Parameters.AddWithValue("$result", request.Receipt.ResultFingerprint); receipt.Parameters.AddWithValue("$start", request.Receipt.StartedAt.ToString("O")); receipt.Parameters.AddWithValue("$end", request.Receipt.CompletedAt.ToString("O")); receipt.Parameters.AddWithValue("$elapsed", request.Receipt.ElapsedMilliseconds); receipt.Parameters.AddWithValue("$beats", request.Receipt.HeartbeatRenewals); receipt.Parameters.AddWithValue("$status", request.Receipt.ResourceStatus); receipt.Parameters.AddWithValue("$error", (object?)request.Receipt.ErrorCode ?? DBNull.Value);
                await receipt.ExecuteNonQueryAsync(cancellationToken);
            }

            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.EvidenceWrite);
            await using (var evidence = _connection.CreateCommand())
            {
                evidence.Transaction = tx;
                evidence.CommandText = "INSERT OR IGNORE INTO evidence(evidence_id,content_hash,created_utc) VALUES($id,$hash,$utc);";
                evidence.Parameters.AddWithValue("$id", request.EvidenceId); evidence.Parameters.AddWithValue("$hash", request.EvidenceHash); evidence.Parameters.AddWithValue("$utc", now);
                await evidence.ExecuteNonQueryAsync(cancellationToken);
            }

            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.TerminalEventWrite);
            await using (var evt = _connection.CreateCommand())
            {
                evt.Transaction = tx;
                evt.CommandText = "INSERT OR IGNORE INTO research_events(event_id,job_id,timestamp_utc,type,payload_hash) VALUES($id,$job,$utc,$type,$hash);";
                evt.Parameters.AddWithValue("$id", request.EventId); evt.Parameters.AddWithValue("$job", request.Job.JobId); evt.Parameters.AddWithValue("$utc", now); evt.Parameters.AddWithValue("$type", request.EventType); evt.Parameters.AddWithValue("$hash", request.EventPayloadHash);
                await evt.ExecuteNonQueryAsync(cancellationToken);
            }
            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.TerminalCommitBeforeCommit);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // Dispose without Commit rolls the transaction back. No terminal state is reported as durable.
            throw;
        }
        finally { _gate.Release(); }
    }

    private async Task<string?> LoadJobStateForTerminalCommitAsync(SqliteTransaction transaction, string jobId, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT state FROM research_jobs WHERE job_id=$job;";
        command.Parameters.AddWithValue("$job", jobId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null || value is DBNull ? null : (string)value;
    }

    public async Task SaveJobAsync(ResearchJob job, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { _faultInjector?.ThrowIfConfigured(StorageFaultPoint.JobStateWrite); await using var c=_connection.CreateCommand(); c.CommandText="INSERT INTO research_jobs(job_id,state,dataset_fingerprint,configuration_fingerprint,engine_fingerprint,created_utc,updated_utc,failure_code) VALUES($id,$state,$df,$cf,$ef,$created,$updated,$failure) ON CONFLICT(job_id) DO UPDATE SET state=$state,dataset_fingerprint=$df,configuration_fingerprint=$cf,engine_fingerprint=$ef,updated_utc=$updated,failure_code=$failure;"; c.Parameters.AddWithValue("$id",job.JobId); c.Parameters.AddWithValue("$state",job.State.ToString()); c.Parameters.AddWithValue("$df",job.DatasetFingerprint); c.Parameters.AddWithValue("$cf",job.ConfigurationFingerprint); c.Parameters.AddWithValue("$ef",job.EngineFingerprint); c.Parameters.AddWithValue("$created",job.CreatedAt.ToString("O")); c.Parameters.AddWithValue("$updated",job.UpdatedAt.ToString("O")); c.Parameters.AddWithValue("$failure",(object?)job.FailureCode ?? DBNull.Value); await c.ExecuteNonQueryAsync(cancellationToken); }
        finally { _gate.Release(); }
    }

    public async Task<ResearchJob?> LoadJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { await using var c=_connection.CreateCommand(); c.CommandText="SELECT state,dataset_fingerprint,configuration_fingerprint,engine_fingerprint,created_utc,updated_utc,failure_code FROM research_jobs WHERE job_id=$id;"; c.Parameters.AddWithValue("$id",jobId); await using var r=await c.ExecuteReaderAsync(cancellationToken); if(!await r.ReadAsync(cancellationToken)) return null; return new ResearchJob(jobId,Enum.Parse<DurableJobState>(r.GetString(0)),r.GetString(1),r.GetString(2),r.GetString(3),DateTimeOffset.Parse(r.GetString(4)),DateTimeOffset.Parse(r.GetString(5)),r.IsDBNull(6)?null:r.GetString(6)); }
        finally { _gate.Release(); }
    }

    async Task<RecoveryContinuationReconciliationRecord?> IRecoveryContinuationReconciliationStore.LoadAsync(string operationFingerprint, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var c = _connection.CreateCommand();
            c.CommandText = "SELECT decision_fingerprint,job_id,sequence,cursor,state_hash,decision,evidence_fingerprint,reason,recorded_utc,resolved_by,resolved_utc FROM recovery_continuation_reconciliations WHERE operation_fingerprint=$op ORDER BY recorded_utc DESC LIMIT 1;";
            c.Parameters.AddWithValue("$op", operationFingerprint);
            await using var r = await c.ExecuteReaderAsync(cancellationToken);
            if (!await r.ReadAsync(cancellationToken)) return null;
            return new RecoveryContinuationReconciliationRecord(r.GetString(0), operationFingerprint, r.GetString(1), r.GetInt64(2), r.GetInt32(3), r.GetString(4), Enum.Parse<RecoveryContinuationReconciliationDecision>(r.GetString(5)), r.GetString(6), r.GetString(7), DateTimeOffset.Parse(r.GetString(8)), r.IsDBNull(9) ? null : r.GetString(9), r.IsDBNull(10) ? null : DateTimeOffset.Parse(r.GetString(10)));
        }
        finally { _gate.Release(); }
    }

    public async Task RecordAmbiguousAsync(RecoveryContinuationReconciliationRecord record, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var c = _connection.CreateCommand();
            c.CommandText = "INSERT OR IGNORE INTO recovery_continuation_reconciliations(decision_fingerprint,operation_fingerprint,job_id,sequence,cursor,state_hash,decision,evidence_fingerprint,reason,recorded_utc,resolved_by,resolved_utc) VALUES($fp,$op,$job,$seq,$cursor,$hash,$decision,$evidence,$reason,$utc,$by,$resolved);";
            c.Parameters.AddWithValue("$fp", record.DecisionFingerprint); c.Parameters.AddWithValue("$op", record.OperationFingerprint); c.Parameters.AddWithValue("$job", record.JobId); c.Parameters.AddWithValue("$seq", record.Sequence); c.Parameters.AddWithValue("$cursor", record.Cursor); c.Parameters.AddWithValue("$hash", record.StateHash); c.Parameters.AddWithValue("$decision", record.Decision.ToString()); c.Parameters.AddWithValue("$evidence", record.EvidenceFingerprint); c.Parameters.AddWithValue("$reason", record.Reason); c.Parameters.AddWithValue("$utc", record.RecordedAt.ToString("O")); c.Parameters.AddWithValue("$by", (object?)record.ResolvedBy ?? DBNull.Value); c.Parameters.AddWithValue("$resolved", record.ResolvedAt is null ? DBNull.Value : record.ResolvedAt.Value.ToString("O"));
            await c.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    async Task<RecoveryContinuationOperation?> IRecoveryContinuationReplayStore.LoadAsync(string operationFingerprint, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var c = _connection.CreateCommand();
            c.CommandText = "SELECT job_id,sequence,cursor,state_hash,state,next_cursor,result_state_hash,completed,result_fingerprint,created_utc,result_recorded_utc FROM recovery_continuation_operations WHERE operation_fingerprint=$fp;";
            c.Parameters.AddWithValue("$fp", operationFingerprint);
            await using var r = await c.ExecuteReaderAsync(cancellationToken);
            if (!await r.ReadAsync(cancellationToken)) return null;
            return new RecoveryContinuationOperation(operationFingerprint, r.GetString(0), r.GetInt64(1), r.GetInt32(2), r.GetString(3), Enum.Parse<RecoveryContinuationOperationState>(r.GetString(4)), r.IsDBNull(5) ? null : r.GetInt32(5), r.IsDBNull(6) ? null : r.GetString(6), r.IsDBNull(7) ? null : r.GetInt32(7) != 0, r.IsDBNull(8) ? null : r.GetString(8), DateTimeOffset.Parse(r.GetString(9)), r.IsDBNull(10) ? null : DateTimeOffset.Parse(r.GetString(10)));
        }
        finally { _gate.Release(); }
    }

    public async Task PrepareAsync(RecoveryContinuationOperation operation, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT job_id,sequence,cursor,state_hash,state FROM recovery_continuation_operations WHERE operation_fingerprint=$fp;";
            check.Parameters.AddWithValue("$fp", operation.OperationFingerprint);
            await using var reader = await check.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var same = string.Equals(reader.GetString(0), operation.JobId, StringComparison.Ordinal) && reader.GetInt64(1) == operation.Sequence && reader.GetInt32(2) == operation.Cursor && string.Equals(reader.GetString(3), operation.StateHash, StringComparison.Ordinal);
                if (!same) throw new InvalidOperationException("RECOVERY_CONTINUATION_OPERATION_FINGERPRINT_CONFLICT");
                return;
            }
            await using var c = _connection.CreateCommand();
            c.CommandText = "INSERT INTO recovery_continuation_operations(operation_fingerprint,job_id,sequence,cursor,state_hash,state,created_utc) VALUES($fp,$job,$seq,$cursor,$hash,$state,$utc);";
            c.Parameters.AddWithValue("$fp", operation.OperationFingerprint); c.Parameters.AddWithValue("$job", operation.JobId); c.Parameters.AddWithValue("$seq", operation.Sequence); c.Parameters.AddWithValue("$cursor", operation.Cursor); c.Parameters.AddWithValue("$hash", operation.StateHash); c.Parameters.AddWithValue("$state", operation.State.ToString()); c.Parameters.AddWithValue("$utc", operation.CreatedAt.ToString("O"));
            await c.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task RecordResultAsync(string operationFingerprint, int nextCursor, string stateHash, bool completed, string? resultFingerprint, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT state,next_cursor,result_state_hash,completed,result_fingerprint FROM recovery_continuation_operations WHERE operation_fingerprint=$fp;";
            check.Parameters.AddWithValue("$fp", operationFingerprint);
            await using var reader = await check.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("RECOVERY_CONTINUATION_OPERATION_NOT_PREPARED");
            if (Enum.Parse<RecoveryContinuationOperationState>(reader.GetString(0)) == RecoveryContinuationOperationState.ResultRecorded)
            {
                var same = reader.GetInt32(1) == nextCursor && string.Equals(reader.GetString(2), stateHash, StringComparison.Ordinal) && reader.GetInt32(3) != 0 && completed || reader.GetInt32(1) == nextCursor && string.Equals(reader.GetString(2), stateHash, StringComparison.Ordinal) && reader.GetInt32(3) == 0 && !completed;
                if (!same || !string.Equals(reader.IsDBNull(4) ? null : reader.GetString(4), resultFingerprint, StringComparison.Ordinal)) throw new InvalidOperationException("RECOVERY_CONTINUATION_RESULT_CONFLICT");
                return;
            }
            await using var c = _connection.CreateCommand();
            c.CommandText = "UPDATE recovery_continuation_operations SET state=$state,next_cursor=$cursor,result_state_hash=$hash,completed=$completed,result_fingerprint=$result, result_recorded_utc=$utc WHERE operation_fingerprint=$fp;";
            c.Parameters.AddWithValue("$state", RecoveryContinuationOperationState.ResultRecorded.ToString()); c.Parameters.AddWithValue("$cursor", nextCursor); c.Parameters.AddWithValue("$hash", stateHash); c.Parameters.AddWithValue("$completed", completed ? 1 : 0); c.Parameters.AddWithValue("$result", (object?)resultFingerprint ?? DBNull.Value); c.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O")); c.Parameters.AddWithValue("$fp", operationFingerprint);
            await c.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task CommitResultAndCheckpointAsync(
        string operationFingerprint,
        int nextCursor,
        string stateHash,
        bool completed,
        string? resultFingerprint,
        ResearchCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        Microsoft.Data.Sqlite.SqliteTransaction? transaction = null;
        try
        {
            transaction = (Microsoft.Data.Sqlite.SqliteTransaction)await _connection.BeginTransactionAsync(cancellationToken);
            await using (var operationCheck = _connection.CreateCommand())
            {
                operationCheck.Transaction = transaction;
                operationCheck.CommandText = "SELECT state,job_id,sequence,cursor,state_hash,next_cursor,result_state_hash,completed,result_fingerprint FROM recovery_continuation_operations WHERE operation_fingerprint=$fp;";
                operationCheck.Parameters.AddWithValue("$fp", operationFingerprint);
                await using var reader = await operationCheck.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException("RECOVERY_CONTINUATION_OPERATION_NOT_PREPARED");

                var state = Enum.Parse<RecoveryContinuationOperationState>(reader.GetString(0));
                if (!string.Equals(reader.GetString(1), checkpoint.JobId, StringComparison.Ordinal) || reader.GetInt64(2) != checkpoint.Sequence - 1 || reader.GetInt32(3) < 0 || string.IsNullOrWhiteSpace(reader.GetString(4)))
                    throw new InvalidOperationException("RECOVERY_CONTINUATION_ATOMIC_COMMIT_CONTEXT_CONFLICT");

                if (state == RecoveryContinuationOperationState.ResultRecorded)
                {
                    var existingNext = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5);
                    var existingHash = reader.IsDBNull(6) ? null : reader.GetString(6);
                    var existingCompleted = reader.IsDBNull(7) ? (bool?)null : reader.GetInt32(7) != 0;
                    var existingFingerprint = reader.IsDBNull(8) ? null : reader.GetString(8);
                    if (existingNext != nextCursor || !string.Equals(existingHash, stateHash, StringComparison.Ordinal) || existingCompleted != completed || !string.Equals(existingFingerprint, resultFingerprint, StringComparison.Ordinal))
                        throw new InvalidOperationException("RECOVERY_CONTINUATION_RESULT_CONFLICT");
                }
            }

            await using (var update = _connection.CreateCommand())
            {
                update.Transaction = transaction;
                update.CommandText = "UPDATE recovery_continuation_operations SET state=$state,next_cursor=$cursor,result_state_hash=$hash,completed=$completed,result_fingerprint=$result,result_recorded_utc=COALESCE(result_recorded_utc,$utc) WHERE operation_fingerprint=$fp;";
                update.Parameters.AddWithValue("$state", RecoveryContinuationOperationState.ResultRecorded.ToString());
                update.Parameters.AddWithValue("$cursor", nextCursor);
                update.Parameters.AddWithValue("$hash", stateHash);
                update.Parameters.AddWithValue("$completed", completed ? 1 : 0);
                update.Parameters.AddWithValue("$result", (object?)resultFingerprint ?? DBNull.Value);
                update.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
                update.Parameters.AddWithValue("$fp", operationFingerprint);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var checkpointCheck = _connection.CreateCommand())
            {
                checkpointCheck.Transaction = transaction;
                checkpointCheck.CommandText = "SELECT cursor,dataset_fingerprint,configuration_fingerprint,engine_fingerprint,state_hash FROM research_checkpoints WHERE job_id=$job AND sequence=$seq;";
                checkpointCheck.Parameters.AddWithValue("$job", checkpoint.JobId);
                checkpointCheck.Parameters.AddWithValue("$seq", checkpoint.Sequence);
                await using var reader = await checkpointCheck.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var same = reader.GetInt32(0) == checkpoint.Cursor &&
                               string.Equals(reader.GetString(1), checkpoint.DatasetFingerprint, StringComparison.Ordinal) &&
                               string.Equals(reader.GetString(2), checkpoint.ConfigurationFingerprint, StringComparison.Ordinal) &&
                               string.Equals(reader.GetString(3), checkpoint.EngineFingerprint, StringComparison.Ordinal) &&
                               string.Equals(reader.GetString(4), checkpoint.StateHash, StringComparison.Ordinal);
                    if (!same) throw new InvalidOperationException("Immutable checkpoint conflict detected for an existing job/sequence.");
                }
                else
                {
                    await using var insert = _connection.CreateCommand();
                    insert.Transaction = transaction;
                    insert.CommandText = "INSERT INTO research_checkpoints(job_id,sequence,cursor,dataset_fingerprint,configuration_fingerprint,engine_fingerprint,state_hash,saved_utc) VALUES($job,$seq,$cursor,$df,$cf,$ef,$hash,$utc);";
                    insert.Parameters.AddWithValue("$job", checkpoint.JobId);
                    insert.Parameters.AddWithValue("$seq", checkpoint.Sequence);
                    insert.Parameters.AddWithValue("$cursor", checkpoint.Cursor);
                    insert.Parameters.AddWithValue("$df", checkpoint.DatasetFingerprint);
                    insert.Parameters.AddWithValue("$cf", checkpoint.ConfigurationFingerprint);
                    insert.Parameters.AddWithValue("$ef", checkpoint.EngineFingerprint);
                    insert.Parameters.AddWithValue("$hash", checkpoint.StateHash);
                    insert.Parameters.AddWithValue("$utc", checkpoint.SavedAt.ToString("O"));
                    await insert.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveCheckpointAsync(ResearchCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _faultInjector?.ThrowIfConfigured(StorageFaultPoint.CheckpointWrite);
            await using var check = _connection.CreateCommand();
            check.CommandText = "SELECT cursor,dataset_fingerprint,configuration_fingerprint,engine_fingerprint,state_hash,saved_utc FROM research_checkpoints WHERE job_id=$job AND sequence=$seq;";
            check.Parameters.AddWithValue("$job", checkpoint.JobId);
            check.Parameters.AddWithValue("$seq", checkpoint.Sequence);
            await using var reader = await check.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var same = reader.GetInt32(0) == checkpoint.Cursor &&
                           string.Equals(reader.GetString(1), checkpoint.DatasetFingerprint, StringComparison.Ordinal) &&
                           string.Equals(reader.GetString(2), checkpoint.ConfigurationFingerprint, StringComparison.Ordinal) &&
                           string.Equals(reader.GetString(3), checkpoint.EngineFingerprint, StringComparison.Ordinal) &&
                           string.Equals(reader.GetString(4), checkpoint.StateHash, StringComparison.Ordinal);
                if (!same) throw new InvalidOperationException("Immutable checkpoint conflict detected for an existing job/sequence.");
                return;
            }

            await using var c = _connection.CreateCommand();
            c.CommandText = "INSERT INTO research_checkpoints(job_id,sequence,cursor,dataset_fingerprint,configuration_fingerprint,engine_fingerprint,state_hash,saved_utc) VALUES($job,$seq,$cursor,$df,$cf,$ef,$hash,$utc);";
            c.Parameters.AddWithValue("$job", checkpoint.JobId);
            c.Parameters.AddWithValue("$seq", checkpoint.Sequence);
            c.Parameters.AddWithValue("$cursor", checkpoint.Cursor);
            c.Parameters.AddWithValue("$df", checkpoint.DatasetFingerprint);
            c.Parameters.AddWithValue("$cf", checkpoint.ConfigurationFingerprint);
            c.Parameters.AddWithValue("$ef", checkpoint.EngineFingerprint);
            c.Parameters.AddWithValue("$hash", checkpoint.StateHash);
            c.Parameters.AddWithValue("$utc", checkpoint.SavedAt.ToString("O"));
            await c.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<ResearchCheckpoint?> LoadLatestCheckpointAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { await using var c=_connection.CreateCommand(); c.CommandText="SELECT sequence,cursor,dataset_fingerprint,configuration_fingerprint,engine_fingerprint,state_hash,saved_utc FROM research_checkpoints WHERE job_id=$job ORDER BY sequence DESC LIMIT 1;"; c.Parameters.AddWithValue("$job",jobId); await using var r=await c.ExecuteReaderAsync(cancellationToken); if(!await r.ReadAsync(cancellationToken)) return null; return new ResearchCheckpoint(jobId,r.GetInt64(0),r.GetInt32(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),DateTimeOffset.Parse(r.GetString(6))); }
        finally { _gate.Release(); }
    }

    public async Task RequestCancellationAsync(JobCancellation request, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { await using var c=_connection.CreateCommand(); c.CommandText="INSERT INTO job_cancellations(job_id,requested_utc,requested_by,reason) VALUES($job,$utc,$by,$reason) ON CONFLICT(job_id) DO UPDATE SET requested_utc=$utc,requested_by=$by,reason=$reason;"; c.Parameters.AddWithValue("$job",request.JobId); c.Parameters.AddWithValue("$utc",request.RequestedAt.ToString("O")); c.Parameters.AddWithValue("$by",request.RequestedBy); c.Parameters.AddWithValue("$reason",request.Reason); await c.ExecuteNonQueryAsync(cancellationToken); }
        finally { _gate.Release(); }
    }

    public async Task SaveProgressAsync(ResearchProgress progress, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { await using var c=_connection.CreateCommand(); c.CommandText="INSERT INTO research_progress(job_id,operation,cursor,total,dataset_fingerprint,configuration_fingerprint,engine_fingerprint,aggregation_state,progress_hash,saved_utc) VALUES($job,$op,$cursor,$total,$df,$cf,$ef,$state,$hash,$utc) ON CONFLICT(job_id,operation) DO UPDATE SET cursor=$cursor,total=$total,dataset_fingerprint=$df,configuration_fingerprint=$cf,engine_fingerprint=$ef,aggregation_state=$state,progress_hash=$hash,saved_utc=$utc;"; c.Parameters.AddWithValue("$job",progress.JobId); c.Parameters.AddWithValue("$op",progress.Operation); c.Parameters.AddWithValue("$cursor",progress.Cursor); c.Parameters.AddWithValue("$total",progress.Total); c.Parameters.AddWithValue("$df",progress.DatasetFingerprint); c.Parameters.AddWithValue("$cf",progress.ConfigurationFingerprint); c.Parameters.AddWithValue("$ef",progress.EngineFingerprint); c.Parameters.AddWithValue("$state",progress.AggregationState); c.Parameters.AddWithValue("$hash",progress.ProgressHash); c.Parameters.AddWithValue("$utc",progress.SavedAt.ToString("O")); await c.ExecuteNonQueryAsync(cancellationToken); }
        finally { _gate.Release(); }
    }

    public async Task<ResearchProgress?> LoadProgressAsync(string jobId, string operation, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { await using var c=_connection.CreateCommand(); c.CommandText="SELECT cursor,total,dataset_fingerprint,configuration_fingerprint,engine_fingerprint,aggregation_state,progress_hash,saved_utc FROM research_progress WHERE job_id=$job AND operation=$op;"; c.Parameters.AddWithValue("$job",jobId); c.Parameters.AddWithValue("$op",operation); await using var r=await c.ExecuteReaderAsync(cancellationToken); if(!await r.ReadAsync(cancellationToken)) return null; return new ResearchProgress(jobId,operation,r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),DateTimeOffset.Parse(r.GetString(7))); }
        finally { _gate.Release(); }
    }

    public async Task<JobCancellation?> LoadCancellationAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { await using var c=_connection.CreateCommand(); c.CommandText="SELECT requested_utc,requested_by,reason FROM job_cancellations WHERE job_id=$job;"; c.Parameters.AddWithValue("$job",jobId); await using var r=await c.ExecuteReaderAsync(cancellationToken); if(!await r.ReadAsync(cancellationToken)) return null; return new JobCancellation(jobId,DateTimeOffset.Parse(r.GetString(0)),r.GetString(1),r.GetString(2)); }
        finally { _gate.Release(); }
    }


    internal async Task ExecuteGovernanceWriteAsync(Func<SqliteConnection, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await _gate.WaitAsync(cancellationToken);
        try { await action(_connection); } finally { _gate.Release(); }
    }

    internal async Task<T> ExecuteGovernanceReadAsync<T>(Func<SqliteConnection, Task<T>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await _gate.WaitAsync(cancellationToken);
        try { return await action(_connection); } finally { _gate.Release(); }
    }

    public async Task SaveAsync(string profileId, TradingRiskSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentNullException.ThrowIfNull(settings);
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        var fingerprint = ResearchFingerprint.Sha256(json);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO risk_settings(profile_id,settings_json,settings_fingerprint,created_utc) VALUES($id,$json,$hash,$utc) ON CONFLICT(profile_id) DO UPDATE SET settings_json=$json,settings_fingerprint=$hash,created_utc=$utc;";
            command.Parameters.AddWithValue("$id", profileId);
            command.Parameters.AddWithValue("$json", json);
            command.Parameters.AddWithValue("$hash", fingerprint);
            command.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<TradingRiskSettings?> LoadAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = "SELECT settings_json,settings_fingerprint FROM risk_settings WHERE profile_id=$id;";
            command.Parameters.AddWithValue("$id", profileId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var json = reader.GetString(0);
            var storedHash = reader.GetString(1);
            if (!string.Equals(storedHash, ResearchFingerprint.Sha256(json), StringComparison.Ordinal)) throw new InvalidOperationException("Risk settings fingerprint mismatch.");
            return System.Text.Json.JsonSerializer.Deserialize<TradingRiskSettings>(json);
        }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync() { _gate.Dispose(); await _connection.DisposeAsync(); }
}
