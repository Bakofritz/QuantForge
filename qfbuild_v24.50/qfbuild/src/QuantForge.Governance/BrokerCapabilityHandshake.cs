using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Governance;

public enum BrokerCapability { LiveMarketData, AccountState, Positions, OrderSubmission, OrderCancellation, ExecutionReports, Heartbeat, ServerRiskStatus }
public enum BrokerConnectionState { Disconnected, Connecting, Connected, Degraded, Failed }

public sealed record BrokerCapabilityHandshakeSnapshot(
    string BrokerAdapterId,
    string ConnectionId,
    BrokerConnectionState State,
    IReadOnlySet<BrokerCapability> Capabilities,
    DateTimeOffset ObservedAt,
    string Fingerprint);

public interface IBrokerCapabilityProbe
{
    BrokerCapabilityHandshakeSnapshot Probe(DateTimeOffset now);
}

public sealed class DeterministicBrokerCapabilityProbe : IBrokerCapabilityProbe
{
    private readonly string _adapterId;
    private readonly IReadOnlySet<BrokerCapability> _capabilities;
    public DeterministicBrokerCapabilityProbe(string adapterId, IEnumerable<BrokerCapability> capabilities)
    { _adapterId = adapterId; _capabilities = capabilities.ToHashSet(); }
    public BrokerCapabilityHandshakeSnapshot Probe(DateTimeOffset now)
    {
        var connectionId = $"broker-{_adapterId}";
        var fp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{_adapterId}|{connectionId}|{string.Join(',', _capabilities.Order())}"))).ToLowerInvariant();
        return new(_adapterId, connectionId, BrokerConnectionState.Connected, _capabilities, now, fp);
    }
}

public sealed class BrokerCapabilityGate
{
    public GovernanceDecision Require(BrokerCapabilityHandshakeSnapshot snapshot, params BrokerCapability[] required)
    {
        if (snapshot.State != BrokerConnectionState.Connected) return new(false, "BROKER_NOT_CONNECTED", "Broker connection is not healthy and connected.");
        var missing = required.Where(x => !snapshot.Capabilities.Contains(x)).ToArray();
        return missing.Length != 0
            ? new(false, "BROKER_CAPABILITY_MISSING", $"Required broker capabilities are missing: {string.Join(',', missing)}")
            : new(true, "BROKER_CAPABILITIES_MATCH", "Broker capability handshake satisfies the requested operation.");
    }
}
