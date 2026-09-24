using System.Text.Json;

namespace QuantForge.Governance;

public interface IPersistentLiveAuditStore
{
    void Append(TamperEvidentAuditEnvelope envelope);
    IReadOnlyList<TamperEvidentAuditEnvelope> Load();
}

/// <summary>Append-only JSON-lines persistence boundary. Production implementations may replace the storage medium without changing audit semantics.</summary>
public sealed class JsonLinesLiveAuditStore : IPersistentLiveAuditStore
{
    private readonly string _path;
    private readonly object _gate = new();

    public JsonLinesLiveAuditStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Audit path is required.", nameof(path));
        _path = path;
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
    }

    public void Append(TamperEvidentAuditEnvelope envelope)
    {
        lock (_gate)
        {
            using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(JsonSerializer.Serialize(envelope));
            writer.Flush();
            stream.Flush(true);
        }
    }

    public IReadOnlyList<TamperEvidentAuditEnvelope> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path)) return [];
            return File.ReadLines(_path).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => JsonSerializer.Deserialize<TamperEvidentAuditEnvelope>(x) ?? throw new InvalidDataException("AUDIT_RECORD_INVALID"))
                .ToArray();
        }
    }
}

public sealed class PersistentTamperEvidentAuditSink : ITamperEvidentAuditSink
{
    private readonly JsonLinesLiveAuditStore _store;
    private readonly List<TamperEvidentAuditEnvelope> _entries;
    private readonly object _gate = new();

    public PersistentTamperEvidentAuditSink(JsonLinesLiveAuditStore store)
    {
        _store = store;
        _entries = store.Load().ToList();
        if (!Validate(_entries)) throw new InvalidDataException("AUDIT_CHAIN_CORRUPT");
    }

    public IReadOnlyList<TamperEvidentAuditEnvelope> Entries { get { lock (_gate) return _entries.ToArray(); } }

    public void Append(LiveTradingAuditRecord record)
    {
        lock (_gate)
        {
            var previous = _entries.Count == 0 ? "GENESIS" : _entries[^1].RecordHash;
            var sequence = _entries.Count + 1L;
            var canonical = JsonSerializer.Serialize(new { sequence, record, previous });
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
            var envelope = new TamperEvidentAuditEnvelope(sequence, record, previous, hash);
            _store.Append(envelope);
            _entries.Add(envelope);
        }
    }

    public bool ChainValid() { lock (_gate) return Validate(_entries); }

    private static bool Validate(IReadOnlyList<TamperEvidentAuditEnvelope> entries)
    {
        var previous = "GENESIS";
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.Sequence != i + 1 || e.PreviousHash != previous) return false;
            var canonical = JsonSerializer.Serialize(new { sequence = e.Sequence, record = e.Record, previous = e.PreviousHash });
            var expected = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
            if (!string.Equals(expected, e.RecordHash, StringComparison.Ordinal)) return false;
            previous = e.RecordHash;
        }
        return true;
    }
}
