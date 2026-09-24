namespace QuantForge.Governance;
public sealed record LiveAuditContinuityV22_65(string ChainHead,string VerifiedHead,bool SignatureValid,DateTimeOffset VerifiedAtUtc);
public static class LiveAuditContinuityGateV22_65 { public static bool IsCurrent(LiveAuditContinuityV22_65 a,DateTimeOffset nowUtc,TimeSpan maxAge)=>!string.IsNullOrWhiteSpace(a.ChainHead)&&a.ChainHead==a.VerifiedHead&&a.SignatureValid&&nowUtc>=a.VerifiedAtUtc&&nowUtc-a.VerifiedAtUtc<=maxAge; }
