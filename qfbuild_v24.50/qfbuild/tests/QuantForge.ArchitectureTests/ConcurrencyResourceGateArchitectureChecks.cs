using QuantForge.Core.Runtime;

namespace QuantForge.ArchitectureTests;

public static class ConcurrencyResourceGateArchitectureChecks
{
    public static void Run()
    {
        var gate = new ConcurrencyResourceGate();
        var blocked = gate.Evaluate(4, 4, 1024, 4096, false);
        if (blocked.Allowed || blocked.StopCode != "NATIVE_RUNTIME_NOT_QUALIFIED")
            throw new InvalidOperationException("Unqualified native runtime must block concurrency admission.");
        var unknown = gate.Evaluate(2, 2, 0, 4096, true);
        if (unknown.Allowed || unknown.StopCode != "RESOURCE_ESTIMATE_UNAVAILABLE")
            throw new InvalidOperationException("Unknown resource estimates must fail closed.");
        var over = gate.Evaluate(4, 4, 2048, 4096, true);
        if (over.Allowed || over.StopCode != "MEMORY_BUDGET_EXCEEDED")
            throw new InvalidOperationException("Memory budget violations must fail closed.");
        var admitted = gate.Evaluate(2, 2, 1024, 4096, true);
        if (!admitted.Allowed || admitted.EffectiveConcurrency != 2)
            throw new InvalidOperationException("Qualified, budget-safe concurrency should be admitted.");
    }
}
