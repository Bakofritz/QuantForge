using System.Text.Json;

namespace QuantForge.Governance;

/// <summary>Crash-resistant single-file execution-intent store. Writes are committed by temp-file replacement.</summary>
public sealed class AtomicLiveExecutionIntentStore : ILiveExecutionIntentStore
{
    private readonly string _path;
    private readonly Dictionary<string, LiveExecutionIntent> _items = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private bool _loaded;

    public AtomicLiveExecutionIntentStore(string path) => _path = path;

    public LiveExecutionIntent? GetByIdempotencyKey(string idempotencyKey)
    {
        lock (_gate) { EnsureLoaded(); return _items.TryGetValue(idempotencyKey, out var item) ? item : null; }
    }

    public LiveExecutionIntent Save(LiveExecutionIntent intent)
    {
        lock (_gate)
        {
            EnsureLoaded();
            if (_items.ContainsKey(intent.IdempotencyKey)) throw new InvalidOperationException("IDEMPOTENCY_KEY_ALREADY_EXISTS");
            _items[intent.IdempotencyKey] = intent;
            Commit();
            return intent;
        }
    }

    public LiveExecutionIntent Update(LiveExecutionIntent intent)
    {
        lock (_gate)
        {
            EnsureLoaded();
            if (!_items.ContainsKey(intent.IdempotencyKey)) throw new KeyNotFoundException("EXECUTION_INTENT_NOT_FOUND");
            _items[intent.IdempotencyKey] = intent;
            Commit();
            return intent;
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        if (!File.Exists(_path)) return;
        var text = File.ReadAllText(_path);
        var loaded = JsonSerializer.Deserialize<List<LiveExecutionIntent>>(text) ?? [];
        foreach (var item in loaded) _items[item.IdempotencyKey] = item;
    }

    private void Commit()
    {
        var full = Path.GetFullPath(_path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var temp = full + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_items.Values.OrderBy(x => x.IdempotencyKey).ToArray()));
        if (OperatingSystem.IsWindows() && File.Exists(full)) File.Replace(temp, full, null);
        else File.Move(temp, full, true);
    }
}
