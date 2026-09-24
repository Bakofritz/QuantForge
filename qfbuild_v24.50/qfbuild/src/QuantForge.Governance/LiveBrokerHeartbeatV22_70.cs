namespace QuantForge.Governance;
public sealed record LiveBrokerHeartbeatV22_70(string SessionId,DateTimeOffset SentAtUtc,DateTimeOffset ReceivedAtUtc,bool Connected,string BrokerIdentity);
public static class LiveBrokerHeartbeatGateV22_70 { public static bool IsHealthy(LiveBrokerHeartbeatV22_70 h,DateTimeOffset nowUtc,TimeSpan maxAge,string expectedSessionId,string expectedBrokerIdentity)=>h.Connected&&h.SessionId==expectedSessionId&&h.BrokerIdentity==expectedBrokerIdentity&&h.ReceivedAtUtc>=h.SentAtUtc&&nowUtc-h.ReceivedAtUtc<=maxAge; }
