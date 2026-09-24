using QuantForge.Backtesting;
using QuantForge.Core;

namespace QuantForge.ArchitectureTests;

public static class CanonicalStrategyAdapterChecks
{
    public static void Run()
    {
        var model = new CanonicalStrategyModel("canonical:test", "source:test", "sourcefp", new[] { "EMA/SMA indicator" }, new[] { "Cross signal" }, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), "model-fp", true, false);
        var adapter = new CanonicalStrategyAdapter();
        var plan = adapter.CreateResearchPlan(model);
        if (!plan.BacktestSupported) throw new InvalidOperationException("Recognized EMA crossover model should produce a supported research plan.");
        if (plan.Indicators.Count != 2 || plan.Signals.Count != 2) throw new InvalidOperationException("Canonical EMA crossover plan is incomplete.");
        if (plan.Indicators.Any(i => i.PeriodIsSourceDerived)) throw new InvalidOperationException("Adapter defaults must not be misrepresented as source-derived periods.");
        if (model.LiveDeploymentEligible) throw new InvalidOperationException("Canonical adapter must not enable live deployment.");
    }
}
