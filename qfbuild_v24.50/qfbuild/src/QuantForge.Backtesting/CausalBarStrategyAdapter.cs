using QuantForge.Core;
using QuantForge.Data;

namespace QuantForge.Backtesting;

public sealed class EmaCrossStrategyAdapter : IStrategy
{
    private readonly int _fastPeriod; private readonly int _slowPeriod; private decimal? _fast; private decimal? _slow;
    public EmaCrossStrategyAdapter(int fastPeriod = 9, int slowPeriod = 21) { if(fastPeriod<=0 || slowPeriod<=fastPeriod) throw new ArgumentOutOfRangeException(); _fastPeriod=fastPeriod; _slowPeriod=slowPeriod; }
    public string StrategyId => "EMA_CROSS";
    public string DefinitionHash => ResearchFingerprint.Sha256($"EMA_CROSS|fast={_fastPeriod}|slow={_slowPeriod}");
    public StrategyObservation Observe(in StrategyContext context, MarketEvent marketEvent)
    {
        var price=marketEvent.Price; var fa=2m/(_fastPeriod+1); var sa=2m/(_slowPeriod+1);
        var pf=_fast ?? price; var ps=_slow ?? price; _fast=((price-pf)*fa)+pf; _slow=((price-ps)*sa)+ps;
        return new StrategyObservation(pf<=ps && _fast>_slow, pf>=ps && _fast<_slow, "EMA_CROSS");
    }
}

public sealed class ClosePriceExecutionModel : IExecutionModel
{
    public ExecutionFill Simulate(SimulationOrder order, MarketEvent marketEvent) => new(order.OrderId, marketEvent.Timestamp, marketEvent.Price, order.Quantity, "BAR_CLOSE_REFERENCE");
}
