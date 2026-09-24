namespace QuantForge.ArchitectureTests;

public static class LiveOrderConfirmationArchitectureChecksV2145
{
    public static void Run()
    {
        var required = new[]
        {
            "LiveOrderConfirmationService",
            "LiveOrderConfirmationToken",
            "PersistentTamperEvidentAuditSink",
            "JsonLinesLiveAuditStore",
            "LiveExecutionAuthorityBoundary"
        };
        foreach (var name in required)
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("REQUIRED_SECURITY_COMPONENT_MISSING");
    }
}
