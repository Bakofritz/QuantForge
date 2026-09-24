namespace QuantForge.Backtesting;

public sealed record DatasetPartitionV24_15(string DatasetId, string PartitionId, string Role, string Fingerprint);
public static class DatasetPartitionIntegrityV24_15
{
    public static bool IsValid(DatasetPartitionV24_15? p) => p is not null && !string.IsNullOrWhiteSpace(p.DatasetId) && !string.IsNullOrWhiteSpace(p.PartitionId) && !string.IsNullOrWhiteSpace(p.Fingerprint) && (p.Role is "InSample" or "OutOfSample" or "Validation");
    public static bool PreventsLeakage(DatasetPartitionV24_15? train, DatasetPartitionV24_15? test) => IsValid(train) && IsValid(test) && train!.DatasetId == test!.DatasetId && train.Fingerprint != test.Fingerprint && train.PartitionId != test.PartitionId;
}
