using QuantForge.Core;

namespace QuantForge.ArchitectureTests;

public static class TransactionalTerminalCommitArchitectureChecks
{
    public static void Validate()
    {
        if (!typeof(ITerminalCommitStore).GetMethods().Any(m => m.Name == nameof(ITerminalCommitStore.CommitTerminalOutcomeAsync)))
            throw new InvalidOperationException("Transactional terminal commit interface is missing.");
        var contract = typeof(TerminalCommitRequest);
        foreach (var name in new[] { nameof(TerminalCommitRequest.Job), nameof(TerminalCommitRequest.Receipt), nameof(TerminalCommitRequest.EvidenceId), nameof(TerminalCommitRequest.EventId) })
            if (contract.GetProperty(name) is null) throw new InvalidOperationException($"Terminal commit field missing: {name}");
    }
}
