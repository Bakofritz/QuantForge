namespace QuantForge.Bots;

public sealed record BotManifest(string BotId, IReadOnlySet<string> Capabilities, IReadOnlySet<string> ProhibitedAuthorities);
public interface IBotRunner
{
    Task<string> RunGovernedAsync(BotManifest manifest, string command, CancellationToken cancellationToken = default);
}
