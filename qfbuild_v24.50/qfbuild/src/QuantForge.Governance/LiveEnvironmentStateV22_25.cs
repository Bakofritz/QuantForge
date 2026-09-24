using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantForge.Governance;

public enum LiveEnvironmentStateV22_25 { Offline, PlatformReady, BrokerConnected, RecoveryRequired, Armed, Blocked }

public sealed record LiveEnvironmentCapabilityV22_25(string Name, bool Enabled, string AuthorityScope, string Reason);

public sealed record LiveEnvironmentStateSnapshotV22_25(
    DateTimeOffset ObservedAt,
    LiveEnvironmentStateV22_25 State,
    bool SecureRuntimeAvailable,
    bool StartupIntegrityVerified,
    bool AuditChainHealthy,
    bool BrokerConnected,
    bool RiskHealthy,
    bool RecoveryCleared,
    bool LivePathwayArmed,
    IReadOnlyList<LiveEnvironmentCapabilityV22_25> Capabilities)
{
    public bool CanRouteOrders => LivePathwayArmed && SecureRuntimeAvailable && StartupIntegrityVerified && AuditChainHealthy && BrokerConnected && RiskHealthy && RecoveryCleared;
}

public static class LiveEnvironmentStateBuilderV22_25
{
    public static LiveEnvironmentStateSnapshotV22_25 Build(
        bool secureRuntimeAvailable,
        bool startupIntegrityVerified,
        bool auditChainHealthy,
        bool brokerConnected,
        bool riskHealthy,
        bool recoveryCleared,
        bool livePathwayArmed,
        DateTimeOffset? observedAt = null)
    {
        var caps = new List<LiveEnvironmentCapabilityV22_25>
        {
            new("Read-only market data", brokerConnected, "READ_ONLY", brokerConnected ? "Broker market-data channel available." : "Broker connection unavailable."),
            new("Account/position observation", brokerConnected, "READ_ONLY", brokerConnected ? "Broker account-state channel may be observed." : "Broker connection unavailable."),
            new("Research/backtesting", true, "RESEARCH_ONLY", "Research authority is independent of live order authority."),
            new("Strategy audit/import", true, "QUARANTINE_AND_GOVERNANCE", "Imported code remains governed and quarantined until explicitly committed."),
            new("Live order routing", livePathwayArmed && brokerConnected && riskHealthy && recoveryCleared && secureRuntimeAvailable && startupIntegrityVerified && auditChainHealthy, "LIVE_ORDER", "Requires every final live-security gate."),
            new("Emergency disarm", true, "LIVE_SECURITY", "Emergency disarm remains available as a safety control.")
        };

        var state = !startupIntegrityVerified || !auditChainHealthy || !secureRuntimeAvailable ? LiveEnvironmentStateV22_25.Blocked
            : !recoveryCleared ? LiveEnvironmentStateV22_25.RecoveryRequired
            : livePathwayArmed ? LiveEnvironmentStateV22_25.Armed
            : brokerConnected ? LiveEnvironmentStateV22_25.BrokerConnected
            : LiveEnvironmentStateV22_25.PlatformReady;

        return new LiveEnvironmentStateSnapshotV22_25(observedAt ?? DateTimeOffset.UtcNow, state, secureRuntimeAvailable, startupIntegrityVerified, auditChainHealthy, brokerConnected, riskHealthy, recoveryCleared, livePathwayArmed, caps);
    }
}
