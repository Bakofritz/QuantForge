namespace QuantForge.Core;

public sealed record FuturesInstrumentProfile(
    string Symbol,
    decimal TickSize,
    decimal TickValue,
    decimal PointValue,
    bool EnabledForInitialResearch,
    string Status,
    string Notes);

public static class QuantForgeInstrumentProfiles
{
    public static FuturesInstrumentProfile MES => new("MES", 0.25m, 1.25m, 5m, true, "PRIMARY", "Initial governed research instrument; one-contract limit.");
    public static FuturesInstrumentProfile MNQ => new("MNQ", 0.25m, 0.50m, 2m, false, "PLANNED", "Planned instrument; requires its own risk calibration before enablement.");
}
