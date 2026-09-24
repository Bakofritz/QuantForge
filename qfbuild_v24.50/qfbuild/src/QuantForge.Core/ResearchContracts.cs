namespace QuantForge.Core;

public enum DurableJobState { Queued, Running, Paused, Completed, Failed, Canceled, RecoveryRequired }

public sealed record ResearchJob(
    string JobId,
    DurableJobState State,
    string DatasetFingerprint,
    string ConfigurationFingerprint,
    string EngineFingerprint,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? FailureCode = null);

public sealed record ResearchCheckpoint(
    string JobId,
    long Sequence,
    int Cursor,
    string DatasetFingerprint,
    string ConfigurationFingerprint,
    string EngineFingerprint,
    string StateHash,
    DateTimeOffset SavedAt);

public sealed record JobCancellation(string JobId, DateTimeOffset RequestedAt, string RequestedBy, string Reason);

public sealed record MarketEvent(DateTimeOffset Timestamp, string Instrument, MarketEventKind Kind, decimal Price, long Volume);
public enum MarketEventKind { BarClose, Trade, Bid, Ask, Replay }

public interface IStrategy
{
    string StrategyId { get; }
    string DefinitionHash { get; }
    StrategyObservation Observe(in StrategyContext context, MarketEvent marketEvent);
}

public sealed record StrategyContext(
    DateTimeOffset EventTime,
    IReadOnlyDictionary<string, decimal> Values,
    int EventSequence,
    IReadOnlyDictionary<string, CausalBarView> Timeframes);

public sealed record StrategyObservation(bool EnterLong, bool ExitLong, string? Reason);

public sealed record SimulationOrder(string OrderId, DateTimeOffset SignalTime, string Instrument, int Quantity, decimal ReferencePrice, string Reason);
public sealed record ExecutionFill(string OrderId, DateTimeOffset FillTime, decimal FillPrice, int Quantity, string ExecutionModel);
public sealed record ResearchLedgerEntry(DateTimeOffset Timestamp, string Type, string ReferenceId, decimal? Value);

public interface IExecutionModel
{
    ExecutionFill Simulate(SimulationOrder order, MarketEvent marketEvent);
}

public interface IResearchEventEngine
{
    Task RunAsync(IEnumerable<MarketEvent> events, IStrategy strategy, IExecutionModel executionModel, CancellationToken cancellationToken = default);
}
