using QuantForge.Core;
using QuantForge.Data;

namespace QuantForge.Backtesting;

public sealed record SideAwareExecutionEvent(
    DateTimeOffset Timestamp,
    PositionIntent Intent,
    int PositionBefore,
    int PositionAfter,
    decimal ReferencePrice,
    decimal FillPrice,
    string Reason,
    string Fingerprint);

/// <summary>Pure research adapter for side-aware fill arithmetic. It never routes an order.</summary>
public static class SideAwareFillModel
{
    public static decimal EntryFill(decimal referencePrice, int signedQuantity, TradingRiskSettings settings)
    {
        if (signedQuantity == 0) throw new ArgumentOutOfRangeException(nameof(signedQuantity));
        var direction = Math.Sign(signedQuantity);
        // Adverse entry slippage: long pays up; short sells down.
        return referencePrice + (settings.SlippageTicksPerSide * settings.TickSize * direction);
    }

    public static decimal ExitFill(decimal referencePrice, int signedPosition, TradingRiskSettings settings)
    {
        if (signedPosition == 0) throw new ArgumentOutOfRangeException(nameof(signedPosition));
        var direction = Math.Sign(signedPosition);
        // Adverse exit slippage: long exits down; short exits up.
        return referencePrice - (settings.SlippageTicksPerSide * settings.TickSize * direction);
    }

    public static string Fingerprint(decimal referencePrice, int signedQuantity, decimal fillPrice, TradingRiskSettings settings) =>
        ResearchFingerprint.Sha256($"SIDE_FILL|{referencePrice}|{signedQuantity}|{fillPrice}|{settings.SlippageTicksPerSide}|{settings.TickSize}");
}

public static class SideAwareIntrabarResolver
{
    public static IntrabarExitDecision Resolve(
        MarketBar bar, int signedPosition, decimal entryPrice, TradingRiskSettings settings,
        OhlcvIntrabarAmbiguityPolicy policy, decimal extremeSinceEntry)
    {
        if (signedPosition > 0)
        {
            var target = settings.ProfitTargetPoints > 0m ? entryPrice + settings.ProfitTargetPoints : (decimal?)null;
            var stop = entryPrice - settings.InitialStopPoints;
            var baseDecision = OhlcvIntrabarResolver.ResolveLong(bar.Open, bar.High, bar.Low, bar.Close, entryPrice, target, stop, policy);
            if (baseDecision.ExitTriggered) return baseDecision;
            if (extremeSinceEntry >= entryPrice + settings.TrailingActivationPoints && bar.Low <= extremeSinceEntry - settings.TrailingDistancePoints)
                return new("TRAILING_STOP", extremeSinceEntry - settings.TrailingDistancePoints, false, policy.ToString(), "OHLCV trailing trigger; intrabar ordering is not observed.");
        }
        else if (signedPosition < 0)
        {
            var target = settings.ProfitTargetPoints > 0m ? entryPrice - settings.ProfitTargetPoints : (decimal?)null;
            var stop = entryPrice + settings.InitialStopPoints;
            var baseDecision = OhlcvIntrabarResolver.ResolveShort(bar.Open, bar.High, bar.Low, bar.Close, entryPrice, target, stop, policy);
            if (baseDecision.ExitTriggered) return baseDecision;
            if (extremeSinceEntry <= entryPrice - settings.TrailingActivationPoints && bar.High >= extremeSinceEntry + settings.TrailingDistancePoints)
                return new("TRAILING_STOP", extremeSinceEntry + settings.TrailingDistancePoints, false, policy.ToString(), "OHLCV trailing trigger; intrabar ordering is not observed.");
        }
        return new("NONE", bar.Close, false, policy.ToString(), "No exit trigger observed under the declared OHLCV policy.");
    }
}
