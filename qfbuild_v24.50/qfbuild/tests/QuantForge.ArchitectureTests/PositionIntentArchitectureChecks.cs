using QuantForge.Backtesting;
using QuantForge.Core;

namespace QuantForge.ArchitectureTests;

public static class PositionIntentArchitectureChecks
{
    public static void Run()
    {
        var longIntent = PositionIntent.EnterLong(1);
        var shortIntent = PositionIntent.EnterShort(1);
        if (longIntent.SignedQuantity != 1 || shortIntent.SignedQuantity != -1)
            throw new InvalidOperationException("Signed position intent mapping failed.");
        if (PositionIntent.Hold().SignedQuantity != 0)
            throw new InvalidOperationException("Hold intent must not carry position quantity.");

        var settings = TradingRiskSettings.DefaultMes();
        var longFill = SideAwareFillModel.EntryFill(6000m, 1, settings);
        var shortFill = SideAwareFillModel.EntryFill(6000m, -1, settings);
        if (longFill <= 6000m || shortFill >= 6000m)
            throw new InvalidOperationException("Entry slippage must be adverse by side.");

        var longExit = SideAwareFillModel.ExitFill(6000m, 1, settings);
        var shortExit = SideAwareFillModel.ExitFill(6000m, -1, settings);
        if (longExit >= 6000m || shortExit <= 6000m)
            throw new InvalidOperationException("Exit slippage must be adverse by side.");

        var legacy = StrategyObservationAdapter.ToPositionIntent(new StrategyObservation(true, false, "LEGACY"));
        if (legacy.Kind != PositionIntentKind.EnterLong)
            throw new InvalidOperationException("Legacy observation adapter failed.");
    }
}
