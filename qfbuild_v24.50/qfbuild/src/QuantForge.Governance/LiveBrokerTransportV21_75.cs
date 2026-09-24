using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuantForge.Governance;

public enum LiveBrokerConnectionState { Disconnected, Connecting, Connected, Degraded, Closing, Failed }

public sealed record LiveBrokerTransportHealth(
    LiveBrokerConnectionState State,
    DateTimeOffset ObservedAt,
    DateTimeOffset? LastHeartbeatAt,
    string ConnectionFingerprint,
    string Code,
    string Reason);

public sealed record LiveBrokerTransportRequest(
    string RequestId,
    string IdempotencyKey,
    string CanonicalPayload,
    string RequestFingerprint,
    DateTimeOffset CreatedAt);

public sealed record LiveBrokerTransportResponse(
    bool Definitive,
    bool Accepted,
    string Code,
    string Message,
    string? ExternalOrderId,
    string ResponseFingerprint,
    DateTimeOffset ObservedAt);

/// <summary>Authenticated transport boundary. It contains no authorization policy and performs no retries implicitly.</summary>
public interface ILiveBrokerTransport
{
    LiveBrokerTransportHealth Health(DateTimeOffset now);
    LiveBrokerTransportResponse Send(LiveBrokerTransportRequest request);
    LiveBrokerTransportResponse QueryByIdempotencyKey(string idempotencyKey, DateTimeOffset now);
}

public sealed class UnconfiguredLiveBrokerTransport : ILiveBrokerTransport
{
    public LiveBrokerTransportHealth Health(DateTimeOffset now) => new(LiveBrokerConnectionState.Disconnected, now, null, "unconfigured", "BROKER_TRANSPORT_UNCONFIGURED", "No production broker transport has been configured.");
    public LiveBrokerTransportResponse Send(LiveBrokerTransportRequest request) => throw new InvalidOperationException("BROKER_TRANSPORT_UNCONFIGURED");
    public LiveBrokerTransportResponse QueryByIdempotencyKey(string idempotencyKey, DateTimeOffset now) => throw new InvalidOperationException("BROKER_TRANSPORT_UNCONFIGURED");
}

public sealed class LiveBrokerConnectionLifecycle
{
    private readonly object _gate = new();
    private LiveBrokerTransportHealth _health;

    public LiveBrokerConnectionLifecycle()
        => _health = new(LiveBrokerConnectionState.Disconnected, DateTimeOffset.UtcNow, null, "none", "INITIALIZED", "Broker connection is not configured.");

    public LiveBrokerTransportHealth BeginConnect(string connectionFingerprint, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(connectionFingerprint)) throw new ArgumentException("Connection fingerprint is required.", nameof(connectionFingerprint));
            _health = _health with { State = LiveBrokerConnectionState.Connecting, ObservedAt = now, ConnectionFingerprint = connectionFingerprint, Code = "CONNECTING", Reason = "Broker transport connection is being established." };
            return _health;
        }
    }

    public LiveBrokerTransportHealth MarkConnected(DateTimeOffset now)
    {
        lock (_gate)
        {
            _health = _health with { State = LiveBrokerConnectionState.Connected, ObservedAt = now, LastHeartbeatAt = now, Code = "CONNECTED", Reason = "Broker transport is connected and has a current heartbeat." };
            return _health;
        }
    }

    public LiveBrokerTransportHealth MarkHeartbeat(DateTimeOffset now)
    {
        lock (_gate)
        {
            _health = _health with { State = LiveBrokerConnectionState.Connected, ObservedAt = now, LastHeartbeatAt = now, Code = "HEARTBEAT_OK", Reason = "Broker heartbeat received." };
            return _health;
        }
    }

    public LiveBrokerTransportHealth MarkDegraded(string reason, DateTimeOffset now)
    {
        lock (_gate) { _health = _health with { State = LiveBrokerConnectionState.Degraded, ObservedAt = now, Code = "DEGRADED", Reason = reason }; return _health; }
    }

    public LiveBrokerTransportHealth MarkFailed(string reason, DateTimeOffset now)
    {
        lock (_gate) { _health = _health with { State = LiveBrokerConnectionState.Failed, ObservedAt = now, Code = "FAILED", Reason = reason }; return _health; }
    }

    public LiveBrokerTransportHealth Disconnect(DateTimeOffset now)
    {
        lock (_gate) { _health = _health with { State = LiveBrokerConnectionState.Disconnected, ObservedAt = now, Code = "DISCONNECTED", Reason = "Broker transport is disconnected; live execution must fail closed." }; return _health; }
    }

    public LiveBrokerTransportHealth Snapshot() { lock (_gate) return _health; }
}

public sealed class TransportBackedLiveBrokerExecutionAdapter : ILiveBrokerExecutionAdapter
{
    private readonly ILiveBrokerTransport _transport;
    private readonly LiveBrokerConnectionLifecycle _lifecycle;
    private readonly Func<DateTimeOffset> _clock;

    public TransportBackedLiveBrokerExecutionAdapter(ILiveBrokerTransport transport, LiveBrokerConnectionLifecycle lifecycle, Func<DateTimeOffset>? clock = null)
    { _transport = transport; _lifecycle = lifecycle; _clock = clock ?? (() => DateTimeOffset.UtcNow); }

    public LiveBrokerSubmissionResult Submit(LiveBrokerSubmissionRequest request)
    {
        var now = _clock();
        var health = _transport.Health(now);
        if (health.State != LiveBrokerConnectionState.Connected)
            throw new InvalidOperationException("BROKER_TRANSPORT_NOT_CONNECTED");

        var canonical = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });
        var fp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        var response = _transport.Send(new($"req-{Guid.NewGuid():N}", request.IdempotencyKey, canonical, fp, now));
        return new(response.Accepted, response.Definitive, response.Code, response.Message, response.ExternalOrderId, fp, response.ObservedAt);
    }

    public LiveBrokerSubmissionResult QueryByIdempotencyKey(string idempotencyKey, DateTimeOffset now)
    {
        var response = _transport.QueryByIdempotencyKey(idempotencyKey, now);
        return new(response.Accepted, response.Definitive, response.Code, response.Message, response.ExternalOrderId, response.ResponseFingerprint, response.ObservedAt);
    }
}
