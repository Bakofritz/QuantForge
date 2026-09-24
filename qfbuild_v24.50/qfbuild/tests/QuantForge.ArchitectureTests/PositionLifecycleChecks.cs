using QuantForge.Backtesting;
using QuantForge.Core;

namespace QuantForge.ArchitectureTests;

public static class PositionLifecycleChecks
{
    public static void Run()
    {
        var settings = TradingRiskSettings.DefaultMes();
        var longEntry = SideAwareFillModel.EntryFill(6000m, 1, settings);
        var shortEntry = SideAwareFillModel.EntryFill(6000m, -1, settings);
        if (longEntry <= 6000m || shortEntry >= 6000m)
            throw new InvalidOperationException("Entry fills are not adverse by side.");

        var longExit = SideAwareFillModel.ExitFill(6000m, 1, settings);
        var shortExit = SideAwareFillModel.ExitFill(6000m, -1, settings);
        if (longExit >= 6000m || shortExit <= 6000m)
            throw new InvalidOperationException("Exit fills are not adverse by side.");

        var longPosition = PositionIntent.ReverseToLong(1);
        var shortPosition = PositionIntent.ReverseToShort(1);
        if (longPosition.SignedQuantity != 1 || shortPosition.SignedQuantity != -1)
            throw new InvalidOperationException("Reversal intent sign mapping failed.");

        var ambiguous = OhlcvIntrabarResolver.ResolveShort(6000m, 6008m, 5980m, 6002m, 6000m, 5990m, 6006m,
            OhlcvIntrabarAmbiguityPolicy.ConservativeStopFirst);
        if (ambiguous.Reason != "INITIAL_STOP")
            throw new InvalidOperationException("Conservative short-side ambiguity precedence failed.");

        var ledger = new AccountLedger(settings, QuantForgeInstrumentProfiles.MES.PointValue);
        if (!ledger.CanOpen(DateTimeOffset.Parse("2026-01-02T10:00:00Z"), -1, shortEntry))
            throw new InvalidOperationException("Ledger rejected valid signed short research position.");
        ledger.Open(DateTimeOffset.Parse("2026-01-02T10:00:00Z"), -1, shortEntry);
        var trade = ledger.Close(DateTimeOffset.Parse("2026-01-02T10:01:00Z"), shortExit, "TEST_SHORT_EXIT", settings.CommissionPerSide, settings.SlippageTicksPerSide * settings.TickValue);
        if (trade.Quantity >= 0 || trade.NetPnl == decimal.MinValue)
            throw new InvalidOperationException("Signed short ledger lifecycle failed.");

        if (!trade.TradeId.StartsWith("T-00000001-", StringComparison.Ordinal) || !trade.Fingerprint.Contains("LEDGER_TRADE_V20.25", StringComparison.Ordinal))
            throw new InvalidOperationException("Trade ID must be deterministic and sequence-bound.");
        if (ledger.Events.Count < 3 || ledger.Events[0].Type != "SESSION_STARTED" || ledger.Events[1].Type != "POSITION_OPENED" || ledger.Events[2].Type != "TRADE_CLOSED")
            throw new InvalidOperationException("Ledger accounting event chain is incomplete.");
        if (ledger.Events.Any(e => e.Sequence <= 0))
            throw new InvalidOperationException("Ledger event sequence must be positive.");
        if (ledger.Events.Select(e => e.Sequence).Distinct().Count() != ledger.Events.Count)
            throw new InvalidOperationException("Ledger event sequences must be unique.");
        if (ledger.Events.Any(e => string.IsNullOrWhiteSpace(e.Fingerprint)))
            throw new InvalidOperationException("Ledger event fingerprints must be present.");
        if (!ledger.Events.Any(e => e.Type == "SESSION_STARTED" && e.Details.Contains("MODE=UTC_CALENDAR_DAY", StringComparison.Ordinal)))
            throw new InvalidOperationException("Initial accounting session declaration is missing.");


        var duplicateTime = new AccountLedger(settings, QuantForgeInstrumentProfiles.MES.PointValue);
        var t = DateTimeOffset.Parse("2026-01-02T10:00:00Z");
        duplicateTime.Open(t, 1, 6000m);
        var first = duplicateTime.Close(t, 6001m, "TEST_ONE", settings.CommissionPerSide, settings.SlippageTicksPerSide * settings.TickValue);
        duplicateTime.Open(t, 1, 6000m);
        var second = duplicateTime.Close(t, 6001m, "TEST_ONE", settings.CommissionPerSide, settings.SlippageTicksPerSide * settings.TickValue);
        if (first.TradeId == second.TradeId)
            throw new InvalidOperationException("Same-timestamp trades must remain deterministically unique.");

        var lockLedger = new AccountLedger(settings, QuantForgeInstrumentProfiles.MES.PointValue);
        lockLedger.Open(t, 1, 6000m);
        lockLedger.Close(t.AddMinutes(1), 5987.5m, "DAILY_LOCK_TEST", settings.CommissionPerSide, settings.SlippageTicksPerSide * settings.TickValue);
        if (!lockLedger.IsDailyLossLocked || lockLedger.Events.Count(e => e.Type == "DAILY_LOSS_LOCKED") != 1)
            throw new InvalidOperationException("Daily loss lock transition evidence was not recorded exactly once.");

        var rollover = new AccountLedger(settings, QuantForgeInstrumentProfiles.MES.PointValue);
        rollover.Open(DateTimeOffset.Parse("2026-01-02T23:59:00Z"), 1, 6000m);
        rollover.Close(DateTimeOffset.Parse("2026-01-02T23:59:30Z"), 5990m, "LOSS", settings.CommissionPerSide, settings.SlippageTicksPerSide * settings.TickValue);
        rollover.Open(DateTimeOffset.Parse("2026-01-03T00:01:00Z"), 1, 6000m);
        if (!rollover.Events.Any(e => e.Type == "SESSION_ROLLED" && e.SessionKey == "2026-01-03" && e.Details.Contains("MODE=UTC_CALENDAR_DAY", StringComparison.Ordinal)))
            throw new InvalidOperationException("UTC accounting session rollover event was not recorded.");
    }
}
