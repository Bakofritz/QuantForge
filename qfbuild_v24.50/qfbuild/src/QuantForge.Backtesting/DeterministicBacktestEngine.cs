using System.Security.Cryptography;
using System.Text;
using QuantForge.Data;

namespace QuantForge.Backtesting;

public sealed record BacktestMetrics(
    int Bars,
    int Trades,
    decimal NetPnlPoints,
    decimal GrossProfitPoints,
    decimal GrossLossPoints,
    decimal MaxDrawdownPoints,
    decimal WinRate);

public sealed record DeterministicBacktestResult(
    BacktestResult Result,
    BacktestMetrics Metrics,
    string StrategyDefinitionHash)
{
    public IReadOnlyList<decimal> TradePnlPoints { get; init; } = Array.Empty<decimal>();
}

public sealed class EmaCrossBacktestEngine
{
    public DeterministicBacktestResult Run(
        IReadOnlyList<MarketBar> bars,
        BacktestRequest request,
        int fastPeriod = 9,
        int slowPeriod = 21,
        decimal pointValue = 1m,
        decimal commissionPoints = 0m)
    {
        if (bars.Count == 0) throw new ArgumentException("At least one bar is required.", nameof(bars));
        if (fastPeriod <= 0 || slowPeriod <= fastPeriod) throw new ArgumentOutOfRangeException(nameof(slowPeriod));
        if (pointValue <= 0) throw new ArgumentOutOfRangeException(nameof(pointValue));
        if (commissionPoints < 0) throw new ArgumentOutOfRangeException(nameof(commissionPoints));
        for (var i = 1; i < bars.Count; i++)
            if (bars[i - 1].Timestamp >= bars[i].Timestamp)
                throw new ArgumentException("Bars must be strictly chronological with unique timestamps.", nameof(bars));

        var fastAlpha = 2m / (fastPeriod + 1);
        var slowAlpha = 2m / (slowPeriod + 1);
        decimal fast = bars[0].Close;
        decimal slow = bars[0].Close;
        int position = 0;
        decimal entry = 0m;
        decimal pnl = 0m, grossProfit = 0m, grossLoss = 0m, peak = 0m, maxDrawdown = 0m;
        int trades = 0, wins = 0;
        var tradePnlPoints = new List<decimal>();

        for (var i = 1; i < bars.Count; i++)
        {
            var bar = bars[i];
            var previousFast = fast;
            var previousSlow = slow;
            fast = ((bar.Close - fast) * fastAlpha) + fast;
            slow = ((bar.Close - slow) * slowAlpha) + slow;

            var crossUp = previousFast <= previousSlow && fast > slow;
            var crossDown = previousFast >= previousSlow && fast < slow;

            if (position == 0 && crossUp)
            {
                position = 1;
                entry = bar.Close;
            }
            else if (position == 1 && crossDown)
            {
                var trade = (bar.Close - entry) * pointValue - commissionPoints;
                pnl += trade;
                tradePnlPoints.Add(trade);
                if (trade >= 0) { grossProfit += trade; wins++; } else grossLoss += -trade;
                trades++;
                position = 0;
            }

            var unrealized = position == 1 ? (bar.Close - entry) * pointValue : 0m;
            var equity = pnl + unrealized;
            peak = Math.Max(peak, equity);
            maxDrawdown = Math.Max(maxDrawdown, peak - equity);
        }

        if (position == 1)
        {
            var finalTrade = (bars[^1].Close - entry) * pointValue - commissionPoints;
            pnl += finalTrade;
            tradePnlPoints.Add(finalTrade);
            if (finalTrade >= 0) { grossProfit += finalTrade; wins++; } else grossLoss += -finalTrade;
            trades++;
        }

        var strategyHash = Hash($"EMA_CROSS|fast={fastPeriod}|slow={slowPeriod}|point={pointValue}|commission={commissionPoints}");
        var requestHash = Hash($"{request.StrategyId}|{request.Instrument}|{request.Timeframe}|{request.DatasetFingerprint}|{request.EngineVersion}|{strategyHash}");
        var resultHash = Hash($"{requestHash}|bars={bars.Count}|trades={trades}|pnl={pnl}|dd={maxDrawdown}");
        var metrics = new BacktestMetrics(bars.Count, trades, pnl, grossProfit, grossLoss, maxDrawdown,
            trades == 0 ? 0 : (decimal)wins / trades);

        var runId = Hash($"{requestHash}|{resultHash}")[..32];
        return new DeterministicBacktestResult(
            new BacktestResult(runId, requestHash, resultHash, true,
                "OHLCV bar-driven simulation; decisions occur on completed bar closes; exact tick ordering, bid/ask, spread and intrabar sequencing are not available."),
            metrics,
            strategyHash) { TradePnlPoints = tradePnlPoints };
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
