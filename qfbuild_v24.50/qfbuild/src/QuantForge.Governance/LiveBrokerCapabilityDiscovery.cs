namespace QuantForge.Governance;

public enum LiveBrokerCapability
{
    MarketData,
    AccountState,
    PositionState,
    OrderSubmission,
    OrderCancellation,
    ExecutionReports,
    Heartbeat,
    RiskMetadata
}

public sealed record LiveBrokerCapabilitySnapshot(string BrokerId, string ConnectionId, DateTimeOffset ObservedAt, IReadOnlySet<LiveBrokerCapability> Capabilities, bool Healthy, string Fingerprint);

public interface ILiveBrokerCapabilityDiscovery
{
    LiveBrokerCapabilitySnapshot Discover(DateTimeOffset now);
}

public sealed class StaticBrokerCapabilityDiscovery : ILiveBrokerCapabilityDiscovery
{
    private readonly string _brokerId;
    private readonly string _connectionId;
    private readonly IReadOnlySet<LiveBrokerCapability> _capabilities;
    private readonly bool _healthy;

    public StaticBrokerCapabilityDiscovery(string brokerId, string connectionId, IEnumerable<LiveBrokerCapability> capabilities, bool healthy = true)
    {
        _brokerId = brokerId; _connectionId = connectionId; _capabilities = capabilities.ToHashSet(); _healthy = healthy;
    }

    public LiveBrokerCapabilitySnapshot Discover(DateTimeOffset now)
    {
        var fp = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{_brokerId}|{_connectionId}|{string.Join(',', _capabilities.Order())}|{_healthy}"))).ToLowerInvariant();
        return new(_brokerId, _connectionId, now, _capabilities, _healthy, fp);
    }
}
