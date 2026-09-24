using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Governance;

public sealed record LiveOrderPreview(
    string PreviewId,
    DateTimeOffset CreatedAt,
    string Instrument,
    string Side,
    decimal Quantity,
    decimal? LimitPrice,
    string StrategyFingerprint,
    string RiskFingerprint,
    string BrokerCapabilityFingerprint,
    string AdmissionFingerprint,
    bool UserConfirmationRequired)
{
    public string OrderPayloadFingerprint => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        $"{Instrument}|{Side}|{Quantity}|{LimitPrice}|{StrategyFingerprint}|{RiskFingerprint}|{BrokerCapabilityFingerprint}"))).ToLowerInvariant();
}

public sealed class LiveOrderPreviewFactory
{
    public LiveOrderPreview Create(DateTimeOffset now, string instrument, string side, decimal quantity, decimal? limitPrice,
        string strategyFingerprint, string riskFingerprint, string brokerFingerprint, LiveOrderAdmissionResult admission)
    {
        if (!admission.Allowed) throw new InvalidOperationException("LIVE_ORDER_PREVIEW_REQUIRES_ADMISSION");
        var material = $"{now:O}|{instrument}|{side}|{quantity}|{limitPrice}|{strategyFingerprint}|{riskFingerprint}|{brokerFingerprint}";
        var fp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
        return new($"preview-{Guid.NewGuid():N}", now, instrument, side, quantity, limitPrice, strategyFingerprint, riskFingerprint, brokerFingerprint, fp, true);
    }
}
