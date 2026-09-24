namespace QuantForge.Core;

public sealed record AndroidResearchTestCaseV24_35(string TestId, string CommandId, string ExpectedState, bool RequiresLiveAuthority);
public static class AndroidResearchTestHarnessV24_35
{
    public static bool IsSafeTest(AndroidResearchTestCaseV24_35? t) => t is not null && !string.IsNullOrWhiteSpace(t.TestId) && !string.IsNullOrWhiteSpace(t.CommandId) && !string.IsNullOrWhiteSpace(t.ExpectedState) && !t.RequiresLiveAuthority;
    public static bool SupportsResearchOnly(AndroidResearchTestCaseV24_35? t) => IsSafeTest(t);
}
