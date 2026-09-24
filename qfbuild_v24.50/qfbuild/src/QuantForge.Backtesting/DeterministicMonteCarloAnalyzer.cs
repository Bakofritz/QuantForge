using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuantForge.Data;

namespace QuantForge.Backtesting;

/// <summary>Deterministic bootstrap analysis with resumable bounded chunks.</summary>
public sealed class DeterministicMonteCarloAnalyzer
{
    private readonly EmaCrossBacktestEngine _engine = new();

    public MonteCarloExecutionState CreateState(
        IReadOnlyList<MarketBar> bars, string datasetFingerprint, string instrument, string timeframe,
        string engineVersion, int fastPeriod, int slowPeriod, int iterations, ulong seed,
        decimal pointValue = 1m, decimal commissionPoints = 0m)
    {
        if (iterations is < 100 or > 100_000) throw new ArgumentOutOfRangeException(nameof(iterations));
        if (seed == 0) throw new ArgumentOutOfRangeException(nameof(seed));
        var request = new BacktestRequest("EMA_CROSS", instrument, timeframe, datasetFingerprint, engineVersion);
        var baseline = _engine.Run(bars, request, fastPeriod, slowPeriod, pointValue, commissionPoints);
        if (baseline.TradePnlPoints.Count == 0) throw new InvalidOperationException("Monte Carlo requires at least one observed completed trade.");
        return new MonteCarloExecutionState(0, iterations, seed, baseline.Result.ResultHash,
            baseline.Metrics.NetPnlPoints, baseline.Metrics.MaxDrawdownPoints,
            baseline.TradePnlPoints.ToArray(), new List<decimal>(), new List<decimal>());
    }

    public MonteCarloChunkResult RunChunk(MonteCarloExecutionState state, int chunkSize, CancellationToken cancellationToken = default)
    {
        if (chunkSize is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(chunkSize));
        if (state.Cursor < 0 || state.Cursor > state.TotalIterations) throw new InvalidOperationException("Invalid Monte Carlo cursor.");
        var rng = new DeterministicRng(state.RngState);
        var nets = new List<decimal>(state.NetSamples);
        var dds = new List<decimal>(state.DrawdownSamples);
        var end = Math.Min(state.TotalIterations, state.Cursor + chunkSize);
        for (var i = state.Cursor; i < end; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            decimal equity = 0m, peak = 0m, maxDd = 0m;
            for (var t = 0; t < state.ObservedTrades.Length; t++)
            {
                var sample = state.ObservedTrades[rng.NextInt(state.ObservedTrades.Length)];
                equity += sample; peak = Math.Max(peak, equity); maxDd = Math.Max(maxDd, peak - equity);
            }
            nets.Add(equity); dds.Add(maxDd);
        }
        var next = state with { Cursor = end, RngState = rng.State, NetSamples = nets, DrawdownSamples = dds };
        return new MonteCarloChunkResult(next, end == state.TotalIterations);
    }

    public MonteCarloReport FinalizeReport(string jobId, MonteCarloExecutionState state, string dataFidelityDeclaration)
    {
        if (state.Cursor != state.TotalIterations) throw new InvalidOperationException("Monte Carlo cannot finalize before all iterations are complete.");
        var net = state.NetSamples.OrderBy(x => x).ToArray();
        var dd = state.DrawdownSamples.OrderBy(x => x).ToArray();
        var seedFingerprint = Hash($"MC_BOOTSTRAP|seed={state.InitialSeed}|iterations={state.TotalIterations}|trades={state.ObservedTrades.Length}|baseline={state.BaselineResultHash}");
        var summary = new MonteCarloSummary(state.TotalIterations, state.ObservedTrades.Length, state.ObservedNetPnlPoints,
            net.Average(), Percentile(net, .50m), Percentile(net, .05m), Percentile(net, .95m), dd.Average(), Percentile(dd, .95m),
            net.Count(x => x <= 0m) / (decimal)state.TotalIterations,
            dd.Count(x => x >= state.ObservedMaxDrawdownPoints) / (decimal)state.TotalIterations, seedFingerprint);
        var reportFingerprint = Hash($"{jobId}|{state.BaselineResultHash}|{summary}|{seedFingerprint}");
        return new MonteCarloReport(jobId, state.BaselineResultHash, summary,
            "Deterministic bootstrap with replacement over observed completed-trade PnL; trade order is resampled and no new market observations are generated.",
            dataFidelityDeclaration, reportFingerprint);
    }

    public static string SerializeState(MonteCarloExecutionState state) => JsonSerializer.Serialize(state);
    public static MonteCarloExecutionState DeserializeState(string json) => JsonSerializer.Deserialize<MonteCarloExecutionState>(json) ?? throw new InvalidOperationException("Invalid Monte Carlo checkpoint state.");

    private static decimal Percentile(decimal[] sorted, decimal p)
    { var position=(sorted.Length-1)*p; var lower=(int)Math.Floor(position); var upper=(int)Math.Ceiling(position); if(lower==upper)return sorted[lower]; var w=position-lower; return sorted[lower]+(sorted[upper]-sorted[lower])*w; }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public sealed record MonteCarloExecutionState(int Cursor, int TotalIterations, ulong InitialSeed, ulong RngState, string BaselineResultHash,
        decimal ObservedNetPnlPoints, decimal ObservedMaxDrawdownPoints, decimal[] ObservedTrades,
        IReadOnlyList<decimal> NetSamples, IReadOnlyList<decimal> DrawdownSamples)
    {
        public MonteCarloExecutionState(int cursor, int totalIterations, ulong seed, string baselineResultHash, decimal observedNetPnlPoints,
            decimal observedMaxDrawdownPoints, decimal[] observedTrades, List<decimal> netSamples, List<decimal> drawdownSamples)
            : this(cursor,totalIterations,seed,seed,baselineResultHash,observedNetPnlPoints,observedMaxDrawdownPoints,observedTrades,netSamples,drawdownSamples) { }
    }

    public sealed record MonteCarloChunkResult(MonteCarloExecutionState State, bool Completed);

    private struct DeterministicRng
    {
        public ulong State { get; private set; }
        public DeterministicRng(ulong state) => State = state;
        private ulong NextU64(){ State += 0x9E3779B97F4A7C15UL; var z=State; z=(z^(z>>30))*0xBF58476D1CE4E5B9UL; z=(z^(z>>27))*0x94D049BB133111EBUL; return z^(z>>31); }
        public int NextInt(int exclusiveMax) => (int)(NextU64()%(ulong)exclusiveMax);
    }
}
