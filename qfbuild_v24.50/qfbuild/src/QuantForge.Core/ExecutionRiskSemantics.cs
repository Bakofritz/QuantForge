using System.Security.Cryptography;
using System.Text;

namespace QuantForge.Core;

public enum CanonicalPositionSizingKind { FixedContracts, RiskBudget }
public enum CanonicalStopKind { None, FixedPoints, FixedTicks, PercentOfPrice }
public enum CanonicalTrailingKind { None, ActivationDistance, ChandelierDistance }
public enum CanonicalProfitTargetKind { None, FixedPoints, FixedTicks, PercentOfPrice }

public sealed record CanonicalStopSemantics(
    CanonicalStopKind Kind,
    decimal Value,
    string Units,
    string Provenance,
    string Fingerprint);

public sealed record CanonicalTrailingSemantics(
    CanonicalTrailingKind Kind,
    decimal ActivationDistance,
    decimal TrailDistance,
    string Units,
    string Provenance,
    string Fingerprint);

public sealed record CanonicalProfitTargetSemantics(
    CanonicalProfitTargetKind Kind,
    decimal Value,
    string Units,
    string Provenance,
    string Fingerprint);

public sealed record CanonicalPositionSizingSemantics(
    CanonicalPositionSizingKind Kind,
    int MaxContracts,
    decimal MaxRiskPerTrade,
    string Provenance,
    string Fingerprint);

public sealed record CanonicalExecutionRiskSemantics(
    string Instrument,
    CanonicalStopSemantics Stop,
    CanonicalTrailingSemantics Trailing,
    CanonicalProfitTargetSemantics ProfitTarget,
    CanonicalPositionSizingSemantics PositionSizing,
    decimal CommissionPerSide,
    decimal SlippageTicksPerSide,
    QuantForgeConnectionProfile ConnectionProfile,
    int EstimatedRoundTripLatencyMs,
    string FidelityDeclaration,
    string Fingerprint)
{
    public static CanonicalExecutionRiskSemantics FromSettings(TradingRiskSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.MaxContracts < 1) throw new ArgumentOutOfRangeException(nameof(settings));
        if (settings.InitialStopPoints <= 0m) throw new ArgumentOutOfRangeException(nameof(settings));
        if (settings.TrailingActivationPoints < 0m || settings.TrailingDistancePoints <= 0m) throw new ArgumentOutOfRangeException(nameof(settings));
        if (settings.ProfitTargetPoints < 0m) throw new ArgumentOutOfRangeException(nameof(settings));
        var stop = new CanonicalStopSemantics(CanonicalStopKind.FixedPoints, settings.InitialStopPoints, "points", "governed-risk-default", Hash($"stop|{settings.InitialStopPoints}"));
        var trailing = new CanonicalTrailingSemantics(CanonicalTrailingKind.ActivationDistance, settings.TrailingActivationPoints, settings.TrailingDistancePoints, "points", "governed-risk-default", Hash($"trail|{settings.TrailingActivationPoints}|{settings.TrailingDistancePoints}"));
        var targetKind = settings.ProfitTargetPoints > 0m ? CanonicalProfitTargetKind.FixedPoints : CanonicalProfitTargetKind.None;
        var target = new CanonicalProfitTargetSemantics(targetKind, settings.ProfitTargetPoints, "points", "governed-risk-default", Hash($"target|{targetKind}|{settings.ProfitTargetPoints}"));
        var sizing = new CanonicalPositionSizingSemantics(CanonicalPositionSizingKind.FixedContracts, settings.MaxContracts, settings.MaxRiskPerTrade, "governed-risk-default", Hash($"size|{settings.MaxContracts}|{settings.MaxRiskPerTrade}"));
        var fingerprint = Hash(string.Join("|", settings.PrimaryInstrument, stop.Fingerprint, trailing.Fingerprint, target.Fingerprint, sizing.Fingerprint, settings.CommissionPerSide, settings.SlippageTicksPerSide, settings.ConnectionProfile, settings.EstimatedRoundTripLatencyMs));
        return new(settings.PrimaryInstrument, stop, trailing, target, sizing, settings.CommissionPerSide, settings.SlippageTicksPerSide, settings.ConnectionProfile, settings.EstimatedRoundTripLatencyMs, settings.FidelityDeclaration, fingerprint);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
