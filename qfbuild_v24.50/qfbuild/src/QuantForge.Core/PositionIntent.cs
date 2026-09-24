namespace QuantForge.Core;

/// <summary>Canonical research position intent. This contract has no broker/order-routing authority.</summary>
public enum PositionIntentKind
{
    Hold,
    EnterLong,
    EnterShort,
    Exit,
    ReverseToLong,
    ReverseToShort
}

public sealed record PositionIntent(
    PositionIntentKind Kind,
    int Quantity,
    string Reason)
{
    public static PositionIntent Hold() => new(PositionIntentKind.Hold, 0, "HOLD");
    public static PositionIntent EnterLong(int quantity, string reason = "ENTRY_LONG") => new(PositionIntentKind.EnterLong, Math.Abs(quantity), reason);
    public static PositionIntent EnterShort(int quantity, string reason = "ENTRY_SHORT") => new(PositionIntentKind.EnterShort, -Math.Abs(quantity), reason);
    public static PositionIntent Exit(string reason = "EXIT") => new(PositionIntentKind.Exit, 0, reason);
    public static PositionIntent ReverseToLong(int quantity, string reason = "REVERSAL_LONG") => new(PositionIntentKind.ReverseToLong, Math.Abs(quantity), reason);
    public static PositionIntent ReverseToShort(int quantity, string reason = "REVERSAL_SHORT") => new(PositionIntentKind.ReverseToShort, -Math.Abs(quantity), reason);

    public int SignedQuantity => Kind switch
    {
        PositionIntentKind.EnterLong or PositionIntentKind.ReverseToLong => Math.Abs(Quantity),
        PositionIntentKind.EnterShort or PositionIntentKind.ReverseToShort => -Math.Abs(Quantity),
        _ => 0
    };
}

public interface ISideAwareStrategy : IStrategy
{
    PositionIntent ObservePosition(in StrategyContext context, MarketEvent marketEvent, int currentPosition);
}

public static class StrategyObservationAdapter
{
    public static PositionIntent ToPositionIntent(StrategyObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (observation.EnterLong && observation.ExitLong)
            throw new InvalidOperationException("A legacy strategy cannot request entry and exit simultaneously.");
        if (observation.EnterLong) return PositionIntent.EnterLong(1, observation.Reason ?? "ENTRY_LONG");
        if (observation.ExitLong) return PositionIntent.Exit(observation.Reason ?? "EXIT_LONG");
        return PositionIntent.Hold();
    }
}
