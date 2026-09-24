using QuantForge.Core;

namespace QuantForge.Data;

public enum NinjaTraderSourceKind { Unknown, NativeReplay, NativeHistorical, ExportText }
public enum NativeDataFidelity { Unknown, Day, Minute, TickLast, TickBidAsk, ReplayEventStream }

public sealed record NinjaTraderSourceFile(
    string FullPath,
    string RelativePath,
    NinjaTraderSourceKind SourceKind,
    long LengthBytes,
    string Sha256,
    DateTimeOffset LastWriteUtc);

public sealed record NinjaTraderSourceInventory(
    string RootPath,
    IReadOnlyList<NinjaTraderSourceFile> Files,
    string InventoryFingerprint);

public sealed record DataCrossValidationSample(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Instrument,
    string Resolution,
    string SourceA,
    string SourceB);

public sealed record DataCrossValidationResult(
    bool Passed,
    int ComparedBars,
    int TimestampMismatches,
    int OhlcMismatches,
    int VolumeMismatches,
    int TickSizeViolations,
    decimal MaxAbsolutePriceDifference,
    long MaxAbsoluteVolumeDifference,
    IReadOnlyList<string> Warnings,
    string ComparisonFingerprint);

public sealed record NativeValidationPlan(
    IReadOnlyList<DataCrossValidationSample> Samples,
    string Rationale,
    string FidelityBoundary);

public interface INinjaTraderNativeExtractor
{
    bool CanExtract(NinjaTraderSourceFile source);
    IReadOnlyList<MarketBar> ExtractBars(NinjaTraderSourceFile source, string instrument, string timeframe);
}
