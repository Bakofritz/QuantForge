using QuantForge.Core.Runtime;
using QuantForge.Data;
using QuantForge.Storage;

namespace QuantForge.ArchitectureTests;

public static class GovernedBacktestBatchArchitectureChecks
{
    public static void Run()
    {
        var result = new GovernedBacktestBatchArchitectureHarness().Run();
        if (!result.Passed) throw new InvalidOperationException("Governed backtest batch architecture validation failed.");
        if (result.Fingerprint.Length != 64) throw new InvalidOperationException("Governed backtest batch fingerprint is not SHA-256.");
        if (!result.Checks.Any(x => x.StartsWith("DATASET_FINGERPRINT_BOUND:", StringComparison.Ordinal)))
            throw new InvalidOperationException("Dataset fingerprint binding check is missing.");
    }
}

public static class GovernedBacktestBatchExecutionChecks
{
    public static async Task RunAsync()
    {
        var importer = new QuantForge.Data.DelimitedOhlcvImporter();
        var dataset = importer.Import(
            "Date;Open;High;Low;Close;Volume\n20260101 0000;100;101;99;100;10\n20260101 0001;100;102;99;101;10\n20260101 0002;101;103;100;102;10\n20260101 0003;102;104;101;103;10\n20260101 0004;103;105;102;104;10",
            "MES", "1m", "fixture-dataset", DateTimeOffset.UnixEpoch);
        var store = new InMemoryDatasetStore(dataset);
        var identity = new QuantForge.Core.ResearchContextIdentity("", "fixture-job", dataset.DatasetId,
            dataset.Fingerprint.ContentHash, "EMA_CROSS", QuantForge.Core.ResearchFingerprint.Sha256("EMA_CROSS|9|21"),
            dataset.Instrument, dataset.Timeframe, "fixture-config");
        identity = identity with { ContextId = QuantForge.Core.ResearchContextFingerprint.Create(identity) };
        var descriptor = new QuantForge.Core.ResearchContextDescriptor(identity, DateTimeOffset.UnixEpoch, "fixture-engine");
        var request = new GovernedBacktestBatchRequest(
            new MultiCaseExecutionRequest("fixture-batch", "fixture-device", TimeSpan.FromMinutes(5), 1, new[] { descriptor }),
            new[] { new GovernedBacktestCase(descriptor, QuantForge.Core.ResearchFingerprint.Sha256("EMA_CROSS|9|21"), 9, 21, 1m, 0m) },
            MultiCaseResourcePolicy.Conservative(TimeSpan.FromMinutes(5)), 1024, 4096, true, true);
        var result = await new GovernedBacktestBatchService(store).RunAsync(request);
        if (!result.Admitted || result.Results.Count != 1 || result.Results[0].ContextId != identity.ContextId)
            throw new InvalidOperationException("Governed backtest fixture did not execute deterministically.");
        if (result.ResultFingerprint.Length != 64) throw new InvalidOperationException("Governed backtest result fingerprint is not SHA-256.");
    }

    private sealed class InMemoryDatasetStore : QuantForge.Storage.IMarketDatasetStore
    {
        private readonly QuantForge.Data.ImportedMarketDataset _dataset;
        public InMemoryDatasetStore(QuantForge.Data.ImportedMarketDataset dataset) => _dataset = dataset;
        public Task SaveDatasetAsync(QuantForge.Data.ImportedMarketDataset dataset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<QuantForge.Data.ImportedMarketDataset?> LoadDatasetAsync(string datasetId, CancellationToken cancellationToken = default)
            => Task.FromResult<QuantForge.Data.ImportedMarketDataset?>(string.Equals(datasetId, _dataset.DatasetId, StringComparison.Ordinal) ? _dataset : null);
    }
}
