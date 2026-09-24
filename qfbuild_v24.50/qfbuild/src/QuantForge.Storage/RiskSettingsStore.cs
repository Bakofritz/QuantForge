using QuantForge.Core;

namespace QuantForge.Storage;

public interface IRiskSettingsStore
{
    Task SaveAsync(string profileId, TradingRiskSettings settings, CancellationToken cancellationToken = default);
    Task<TradingRiskSettings?> LoadAsync(string profileId, CancellationToken cancellationToken = default);
}
