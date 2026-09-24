using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Governance;

public sealed record CanonicalLiveOrderV21_95(string Symbol, string Side, decimal Quantity, string OrderType, decimal? LimitPrice, decimal? StopPrice, string TimeInForce);
public sealed record NormalizedBrokerOrderV21_95(CanonicalLiveOrderV21_95 Order, string CanonicalPayload, string Fingerprint);

public static class LiveBrokerOrderNormalizerV21_95
{
    public static NormalizedBrokerOrderV21_95 Normalize(CanonicalLiveOrderV21_95 order)
    {
        if (string.IsNullOrWhiteSpace(order.Symbol) || string.IsNullOrWhiteSpace(order.Side) || order.Quantity <= 0)
            throw new ArgumentException("Order identity, side, and positive quantity are required.");
        var symbol = order.Symbol.Trim().ToUpperInvariant();
        var side = order.Side.Trim().ToUpperInvariant();
        var type = order.OrderType.Trim().ToUpperInvariant();
        var tif = order.TimeInForce.Trim().ToUpperInvariant();
        var payload = string.Join("|", symbol, side, order.Quantity.ToString("0.########", CultureInfo.InvariantCulture), type,
            order.LimitPrice?.ToString("0.########", CultureInfo.InvariantCulture) ?? "", order.StopPrice?.ToString("0.########", CultureInfo.InvariantCulture) ?? "", tif);
        var fp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return new(new(symbol, side, order.Quantity, type, order.LimitPrice, order.StopPrice, tif), payload, fp);
    }
}
