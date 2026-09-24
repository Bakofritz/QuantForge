namespace QuantForge.Data;

public sealed record MarketDataFingerprint(string DatasetId, string Instrument, string Timeframe, string ContentHash, DateTimeOffset CapturedAt);
public sealed record DataFidelityProfile(bool HasOhlcv, bool HasBidAsk, bool HasTickSequence, bool HasReplayEvents, string Declaration);

public interface IMarketDataSource
{
    DataFidelityProfile Fidelity { get; }
}
