using QuantForge.Core;
using QuantForge.Data;

namespace QuantForge.Backtesting;

public sealed class EventDrivenBacktestEngine
{
    private readonly CausalTimeframeAggregator _aggregator = new();

    public OhlcvIntrabarAmbiguityPolicy IntrabarAmbiguityPolicy { get; init; } = OhlcvIntrabarAmbiguityPolicy.ConservativeStopFirst;

    public async Task RunAsync(
        IReadOnlyList<MarketBar> bars,
        IStrategy strategy,
        IExecutionModel executionModel,
        IReadOnlyList<TimeSpan> timeframes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bars); ArgumentNullException.ThrowIfNull(strategy); ArgumentNullException.ThrowIfNull(executionModel);
        DateTimeOffset? previous = null; var sequence = 0;
        foreach (var bar in bars)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (previous is not null && bar.Timestamp <= previous.Value)
                throw new InvalidOperationException("Market bars must be strictly causal and chronological.");
            previous = bar.Timestamp;
            var eventTime = bar.Timestamp;
            var marketEvent = new MarketEvent(eventTime, string.Empty, MarketEventKind.BarClose, bar.Close, bar.Volume);
            var tfViews = _aggregator.Snapshot(bars, eventTime, timeframes);
            var values = new Dictionary<string, decimal>(StringComparer.Ordinal)
            { ["PRICE"] = bar.Close, ["VOLUME"] = bar.Volume };
            var context = new StrategyContext(eventTime, values, sequence++, tfViews);
            var observation = strategy.Observe(context, marketEvent);
            if (executionModel is RiskManagedExecutionModel managed && managed.ShouldExitLong(bar.High, bar.Low))
                _ = managed.Simulate(new SimulationOrder($"SIM-{sequence}-RISK-EXIT", eventTime, marketEvent.Instrument, -1, bar.Close, managed.CurrentExitReason(bar.High, bar.Low)), marketEvent);
            if (observation.EnterLong)
            {
                if (executionModel is RiskManagedExecutionModel risk && risk.CanEnter(1, bar.Close))
                    _ = executionModel.Simulate(new SimulationOrder($"SIM-{sequence}-ENTRY", eventTime, marketEvent.Instrument, 1, bar.Close, observation.Reason ?? "ENTRY"), marketEvent);
            }
            if (observation.ExitLong) _ = executionModel.Simulate(new SimulationOrder($"SIM-{sequence}-EXIT", eventTime, marketEvent.Instrument, -1, bar.Close, observation.Reason ?? "EXIT"), marketEvent);
            await Task.Yield();
        }
    }
}
