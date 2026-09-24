using QuantForge.Core;

namespace QuantForge.Data;

public sealed class CanonicalDataGate
{
    public CanonicalDataGateResult Validate(ImportedMarketDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        var errors = new List<string>();
        var warnings = new List<string>();
        if (dataset.Bars.Count == 0) errors.Add("DATASET_EMPTY");
        if (string.IsNullOrWhiteSpace(dataset.Instrument)) errors.Add("INSTRUMENT_MISSING");
        if (string.IsNullOrWhiteSpace(dataset.Timeframe)) errors.Add("TIMEFRAME_MISSING");
        if (dataset.Bars.Any(b => b.High < Math.Max(b.Open, b.Close) || b.Low > Math.Min(b.Open, b.Close) || b.Low > b.High)) errors.Add("OHLC_INVALID");
        for (var i = 1; i < dataset.Bars.Count; i++)
        {
            if (dataset.Bars[i].Timestamp <= dataset.Bars[i - 1].Timestamp)
                errors.Add("CHRONOLOGY_INVALID");
        }
        if (!dataset.Fidelity.HasOhlcv) errors.Add("OHLCV_REQUIRED");
        if (!dataset.Fidelity.HasBidAsk || !dataset.Fidelity.HasTickSequence)
            warnings.Add("EXECUTION_FIDELITY_LIMITED_TO_DECLARED_OHLCV");
        return new CanonicalDataGateResult(errors.Count == 0, dataset.Fingerprint.ContentHash, errors, warnings);
    }
}
