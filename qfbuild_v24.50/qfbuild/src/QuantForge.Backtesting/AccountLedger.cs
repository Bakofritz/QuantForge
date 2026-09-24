using QuantForge.Core;

namespace QuantForge.Backtesting;

public sealed record LedgerTrade(
    string TradeId,
    DateTimeOffset EntryTime,
    DateTimeOffset ExitTime,
    int Quantity,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal GrossPnl,
    decimal Commission,
    decimal SlippageCost,
    decimal NetPnl,
    string ExitReason,
    string Fingerprint);

public sealed record AccountLedgerSnapshot(
    decimal InitialEquity,
    decimal RealizedPnl,
    decimal UnrealizedPnl,
    decimal Equity,
    decimal PeakEquity,
    decimal MaxDrawdown,
    decimal DailyRealizedPnl,
    bool DailyLossLocked,
    int OpenContracts,
    string Fingerprint);

public sealed record LedgerAccountingEvent(
    long Sequence, DateTimeOffset Timestamp, string SessionKey, string Type,
    int PositionBefore, int PositionAfter, decimal RealizedPnl, decimal DailyRealizedPnl,
    decimal Equity, bool DailyLossLocked, string ReferenceId, string Details, string Fingerprint);

/// <summary>Deterministic research-only account ledger. It has no broker or order authority.</summary>
public sealed class AccountLedger
{
    private readonly TradingRiskSettings _settings;
    private readonly decimal _pointValue;
    private decimal _equity;
    private decimal _peakEquity;
    private decimal _realized;
    private decimal _dailyRealized;
    private decimal? _entry;
    private DateTimeOffset _entryTime;
    private int _position;
    private DateTimeOffset _sessionDate;
    private decimal _sessionPeak;
    private long _eventSequence;
    private long _tradeSequence;
    private bool _lockEventRecorded;
    private readonly List<LedgerAccountingEvent> _events = new();

    public AccountLedger(TradingRiskSettings settings, decimal pointValue)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _pointValue = pointValue > 0m ? pointValue : throw new ArgumentOutOfRangeException(nameof(pointValue));
        _equity = settings.InitialAccountSize;
        _peakEquity = _equity;
        _sessionPeak = _equity;
        _sessionDate = DateTimeOffset.MinValue.Date;
    }

    public IReadOnlyList<LedgerAccountingEvent> Events => _events.AsReadOnly();

    public string CurrentSessionKey => _sessionDate.Date == DateTimeOffset.MinValue.Date ? string.Empty : _sessionDate.ToString("yyyy-MM-dd");

    public AccountLedgerSnapshot Snapshot(decimal markPrice = 0m)
    {
        var unrealized = _entry is null || _position == 0 ? 0m : (markPrice - _entry.Value) * _position * _pointValue;
        var equity = _equity + unrealized;
        var drawdown = Math.Max(0m, _peakEquity - equity);
        return new(_settings.InitialAccountSize, _realized, unrealized, equity, _peakEquity, drawdown,
            _dailyRealized, _dailyRealized <= -_settings.DailyLossLimit, Math.Abs(_position),
            ResearchFingerprint.Sha256($"{_settings.InitialAccountSize}|{_realized}|{unrealized}|{equity}|{_peakEquity}|{_dailyRealized}|{_position}"));
    }

    public bool CanOpen(DateTimeOffset time, int quantity, decimal entryPrice)
    {
        RollSession(time);
        if (quantity == 0 || Math.Abs(quantity) > _settings.MaxContracts || _position != 0) return false;
        if (_dailyRealized <= -_settings.DailyLossLimit) return false;
        var stopRisk = _settings.InitialStopPoints * _pointValue * Math.Abs(quantity);
        return stopRisk <= _settings.MaxRiskPerTrade && _equity - stopRisk >= 0m;
    }

    public void Open(DateTimeOffset time, int quantity, decimal entryPrice)
    {
        if (!CanOpen(time, quantity, entryPrice)) throw new InvalidOperationException("Account/risk ledger rejected simulated entry.");
        var before = _position;
        _entry = entryPrice; _entryTime = time; _position = quantity;
        RecordEvent(time, "POSITION_OPENED", before, _position, $"ENTRY|{entryPrice}|{quantity}");
    }

    public LedgerTrade Close(DateTimeOffset time, decimal exitPrice, string reason, decimal commission, decimal slippageCost)
    {
        RollSession(time);
        if (_entry is null || _position == 0) throw new InvalidOperationException("No open research position.");
        var gross = (exitPrice - _entry.Value) * _position * _pointValue;
        var net = gross - commission - slippageCost;
        _equity += net; _realized += net; _dailyRealized += net;
        _peakEquity = Math.Max(_peakEquity, _equity);
        var tradeSequence = ++_tradeSequence;
        var tradeFingerprint = ResearchFingerprint.Sha256($"LEDGER_TRADE_V20.25|{tradeSequence}|{_entry}|{_entryTime:O}|{time:O}|{exitPrice}|{_position}|{gross}|{commission}|{slippageCost}|{net}|{reason}");
        var tradeId = $"T-{tradeSequence:D8}-{tradeFingerprint[..16]}";
        var trade = new LedgerTrade(tradeId, _entryTime, time, _position, _entry.Value, exitPrice,
            gross, commission, slippageCost, net, reason, tradeFingerprint);
        var before = _position;
        _entry = null; _entryTime = default; _position = 0;
        RecordEvent(time, "TRADE_CLOSED", before, 0, $"{tradeId}|{reason}|NET={net}");
        if (IsDailyLossLocked && !_lockEventRecorded)
        {
            _lockEventRecorded = true;
            RecordEvent(time, "DAILY_LOSS_LOCKED", 0, 0, $"LIMIT={_settings.DailyLossLimit}|DAILY={_dailyRealized}");
        }
        return trade;
    }

    public bool IsDailyLossLocked => _dailyRealized <= -_settings.DailyLossLimit;

    private void RollSession(DateTimeOffset time)
    {
        var day = time.UtcDateTime.Date;
        if (_sessionDate == DateTimeOffset.MinValue.Date)
        {
            _sessionDate = day; _sessionPeak = _equity; _lockEventRecorded = false;
            RecordEvent(time, "SESSION_STARTED", _position, _position, $"MODE=UTC_CALENDAR_DAY|TO={day:yyyy-MM-dd}|EQUITY={_equity}");
            return;
        }
        if (day != _sessionDate)
        {
            var previousSession = _sessionDate.ToString("yyyy-MM-dd");
            _sessionDate = day; _dailyRealized = 0m; _sessionPeak = _equity; _lockEventRecorded = false;
            RecordEvent(time, "SESSION_ROLLED", _position, _position, $"MODE=UTC_CALENDAR_DAY|FROM={previousSession}|TO={day:yyyy-MM-dd}|EQUITY={_equity}");
        }
    }
    private void RecordEvent(DateTimeOffset time, string type, int before, int after, string details)
    {
        var session = CurrentSessionKey;
        var sequence = ++_eventSequence;
        var locked = IsDailyLossLocked;
        var fingerprint = ResearchFingerprint.Sha256($"LEDGER_EVENT_V20.25|{sequence}|{time:O}|{session}|{type}|{before}|{after}|{_realized}|{_dailyRealized}|{_equity}|{locked}|{details}");
        _events.Add(new LedgerAccountingEvent(sequence, time, session, type, before, after, _realized, _dailyRealized, _equity, locked, details.Split('|')[0], details, fingerprint));
    }

}

public static class OhlcvIntrabarResolver
{
    public static IntrabarExitDecision ResolveLong(decimal open, decimal high, decimal low, decimal close,
        decimal entry, decimal? target, decimal stop, OhlcvIntrabarAmbiguityPolicy policy)
    {
        var hitTarget = target.HasValue && high >= target.Value;
        var hitStop = low <= stop;
        if (!hitTarget && !hitStop) return new("NONE", close, false, policy.ToString(), Fidelity());
        if (hitTarget && hitStop)
        {
            return policy switch
            {
                OhlcvIntrabarAmbiguityPolicy.TargetFirst => new("PROFIT_TARGET", target!.Value, true, policy.ToString(), Fidelity()),
                OhlcvIntrabarAmbiguityPolicy.CloseProximity => Math.Abs(close - target!.Value) <= Math.Abs(close - stop)
                    ? new("PROFIT_TARGET", target.Value, true, policy.ToString(), Fidelity())
                    : new("INITIAL_STOP", stop, true, policy.ToString(), Fidelity()),
                OhlcvIntrabarAmbiguityPolicy.RejectAmbiguousBar => new("AMBIGUOUS_REJECTED", close, true, policy.ToString(), Fidelity()),
                _ => new("INITIAL_STOP", stop, true, policy.ToString(), Fidelity())
            };
        }
        return hitTarget ? new("PROFIT_TARGET", target!.Value, false, policy.ToString(), Fidelity())
                         : new("INITIAL_STOP", stop, false, policy.ToString(), Fidelity());
    }

    public static IntrabarExitDecision ResolveShort(decimal open, decimal high, decimal low, decimal close,
        decimal entry, decimal? target, decimal stop, OhlcvIntrabarAmbiguityPolicy policy)
    {
        var hitTarget = target.HasValue && low <= target.Value;
        var hitStop = high >= stop;
        if (!hitTarget && !hitStop) return new("NONE", close, false, policy.ToString(), Fidelity());
        if (hitTarget && hitStop)
        {
            return policy switch
            {
                OhlcvIntrabarAmbiguityPolicy.TargetFirst => new("PROFIT_TARGET", target!.Value, true, policy.ToString(), Fidelity()),
                OhlcvIntrabarAmbiguityPolicy.CloseProximity => Math.Abs(close - target!.Value) <= Math.Abs(close - stop)
                    ? new("PROFIT_TARGET", target.Value, true, policy.ToString(), Fidelity())
                    : new("INITIAL_STOP", stop, true, policy.ToString(), Fidelity()),
                OhlcvIntrabarAmbiguityPolicy.RejectAmbiguousBar => new("AMBIGUOUS_REJECTED", close, true, policy.ToString(), Fidelity()),
                _ => new("INITIAL_STOP", stop, true, policy.ToString(), Fidelity())
            };
        }
        return hitTarget ? new("PROFIT_TARGET", target!.Value, false, policy.ToString(), Fidelity())
                         : new("INITIAL_STOP", stop, false, policy.ToString(), Fidelity());
    }

    private static string Fidelity() => "OHLCV cannot establish intrabar tick ordering; simultaneous high/low barrier hits are policy-resolved, not observed execution sequence.";
}
