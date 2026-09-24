namespace QuantForge.Reporting;

public sealed record ResearchReport(string ReportId, string[] EvidenceIds, string[] Limitations, string EngineVersion);
public interface IReportBuilder { Task<ResearchReport> BuildAsync(string[] evidenceIds, CancellationToken cancellationToken = default); }
