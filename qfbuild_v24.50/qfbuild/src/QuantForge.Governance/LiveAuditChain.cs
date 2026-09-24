using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuantForge.Governance;

public sealed record TamperEvidentAuditEnvelope(long Sequence, LiveTradingAuditRecord Record, string PreviousHash, string RecordHash);

public interface ITamperEvidentAuditSink : ILiveTradingAuditSink
{
    IReadOnlyList<TamperEvidentAuditEnvelope> Entries { get; }
    bool ChainValid();
}

public sealed class TamperEvidentLiveTradingAuditSink : ITamperEvidentAuditSink
{
    private readonly List<TamperEvidentAuditEnvelope> _entries = [];
    private readonly object _gate = new();
    public IReadOnlyList<TamperEvidentAuditEnvelope> Entries { get { lock (_gate) return _entries.ToArray(); } }

    public void Append(LiveTradingAuditRecord record)
    {
        lock (_gate)
        {
            var previous = _entries.Count == 0 ? "GENESIS" : _entries[^1].RecordHash;
            var sequence = _entries.Count + 1L;
            var canonical = JsonSerializer.Serialize(new { sequence, record, previous });
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
            _entries.Add(new(sequence, record, previous, hash));
        }
    }

    public bool ChainValid()
    {
        lock (_gate)
        {
            var previous = "GENESIS";
            for (var i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.Sequence != i + 1 || e.PreviousHash != previous) return false;
                var canonical = JsonSerializer.Serialize(new { sequence = e.Sequence, record = e.Record, previous = e.PreviousHash });
                var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
                if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expected), Convert.FromHexString(e.RecordHash))) return false;
                previous = e.RecordHash;
            }
            return true;
        }
    }
}
