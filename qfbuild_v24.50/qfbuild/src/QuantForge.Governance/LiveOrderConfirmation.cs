using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuantForge.Governance;

public sealed record LiveOrderConfirmationToken(
    string TokenId,
    string PreviewId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string PreviewFingerprint,
    string SecurityBindingFingerprint,
    string TokenFingerprint,
    bool Consumed);

public sealed record LiveOrderConfirmationResult(bool Confirmed, string Code, string Reason, LiveOrderConfirmationToken? Token);

/// <summary>Creates a short-lived, single-use confirmation bound to the exact order preview and security state.</summary>
public sealed class LiveOrderConfirmationService
{
    private readonly TimeSpan _lifetime;
    private readonly Dictionary<string, LiveOrderConfirmationToken> _tokens = new();
    private readonly object _gate = new();

    public LiveOrderConfirmationService(TimeSpan? lifetime = null)
    {
        _lifetime = lifetime ?? TimeSpan.FromMinutes(2);
        if (_lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime));
    }

    public LiveOrderConfirmationToken Issue(LiveOrderPreview preview, string securityBindingFingerprint, DateTimeOffset now)
    {
        if (!preview.UserConfirmationRequired) throw new InvalidOperationException("USER_CONFIRMATION_NOT_REQUIRED");
        if (string.IsNullOrWhiteSpace(securityBindingFingerprint)) throw new ArgumentException("Security binding fingerprint is required.", nameof(securityBindingFingerprint));
        var tokenId = $"confirm-{Guid.NewGuid():N}";
        var expires = now.Add(_lifetime);
        var tokenFp = Fingerprint(new { tokenId, preview.PreviewId, preview.AdmissionFingerprint, securityBindingFingerprint, issuedAt = now, expiresAt = expires });
        var token = new LiveOrderConfirmationToken(tokenId, preview.PreviewId, now, expires, preview.AdmissionFingerprint, securityBindingFingerprint, tokenFp, false);
        lock (_gate) _tokens[tokenId] = token;
        return token;
    }

    public LiveOrderConfirmationResult Confirm(LiveOrderPreview preview, LiveOrderAdmissionRequest currentAdmission, LiveOrderConfirmationToken token, string currentSecurityBindingFingerprint, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (!_tokens.TryGetValue(token.TokenId, out var stored)) return new(false, "CONFIRMATION_TOKEN_UNKNOWN", "The confirmation token is not recognized.", null);
            if (stored.Consumed) return new(false, "CONFIRMATION_TOKEN_CONSUMED", "The confirmation token has already been consumed.", stored);
            if (now > stored.ExpiresAt) return new(false, "CONFIRMATION_TOKEN_EXPIRED", "The confirmation token expired.", stored);
            if (!string.Equals(stored.PreviewId, preview.PreviewId, StringComparison.Ordinal) || !string.Equals(stored.PreviewFingerprint, preview.AdmissionFingerprint, StringComparison.Ordinal))
                return new(false, "PREVIEW_BINDING_MISMATCH", "The confirmation token is not bound to this exact preview.", stored);
            if (!string.Equals(stored.SecurityBindingFingerprint, currentSecurityBindingFingerprint, StringComparison.Ordinal))
                return new(false, "SECURITY_BINDING_CHANGED", "A security or authorization binding changed after confirmation was issued.", stored);

            var gate = new LiveOrderAdmissionGate().Evaluate(currentAdmission);
            if (!gate.Allowed) return new(false, "ADMISSION_RECHECK_FAILED", $"Final admission recheck failed: {gate.Code}", stored);
            var expectedTokenFp = Fingerprint(new { tokenId = stored.TokenId, preview.PreviewId, preview.AdmissionFingerprint, securityBindingFingerprint = currentSecurityBindingFingerprint, issuedAt = stored.IssuedAt, expiresAt = stored.ExpiresAt });
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expectedTokenFp), Convert.FromHexString(stored.TokenFingerprint)))
                return new(false, "CONFIRMATION_INTEGRITY_FAILED", "The confirmation token integrity check failed.", stored);

            var consumed = stored with { Consumed = true };
            _tokens[stored.TokenId] = consumed;
            return new(true, "LIVE_ORDER_CONFIRMED", "Human confirmation and final admission recheck passed. No broker order has been submitted by this service.", consumed);
        }
    }

    private static string Fingerprint(object value)
    {
        var canonical = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
