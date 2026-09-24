namespace QuantForge.AI;

public sealed record AiDirective(string DirectiveId, string Operation, string EvidenceScope, string Rationale);
public sealed record AiAnalysis(string AnalysisId, string EvidenceScope, string Text, bool NonAuthoritative = true);

public interface IAiGateway
{
    Task<AiAnalysis> AnalyzeAsync(AiDirective directive, CancellationToken cancellationToken = default);
}
