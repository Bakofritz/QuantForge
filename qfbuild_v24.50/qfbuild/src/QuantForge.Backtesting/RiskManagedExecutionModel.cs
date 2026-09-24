using QuantForge.Core;

namespace QuantForge.Backtesting;

/// <summary>Deterministic research execution-cost/risk model. It never connects to a broker or places orders.</summary>
public sealed class RiskManagedExecutionModel : IExecutionModel
{
    private readonly TradingRiskSettings _settings;
    private decimal? _entryPrice;
    private decimal? _highestPrice;
    private int _position;

    public RiskManagedExecutionModel(TradingRiskSettings? settings = null)
    {
        _settings = settings ?? TradingRiskSettings.DefaultMes();
        if (_settings.MaxContracts != 1) throw new ArgumentOutOfRangeException(nameof(settings), "Initial governed mode is limited to one contract.");
        if (_settings.InitialAccountSize <= 0m) throw new ArgumentOutOfRangeException(nameof(settings));
        if (_settings.InitialStopPoints <= 0m || _settings.TrailingDistancePoints <= 0m) throw new ArgumentOutOfRangeException(nameof(settings));
        if (_settings.ProfitTargetPoints < 0m) throw new ArgumentOutOfRangeException(nameof(settings));
        if (_settings.SlippageTicksPerSide < 0m || _settings.CommissionPerSide < 0m) throw new ArgumentOutOfRangeException(nameof(settings));
    }

    public TradingRiskSettings Settings => _settings;

    public bool CanEnterSigned(int quantity, decimal referencePrice)
    {
        if (quantity == 0 || Math.Abs(quantity) > _settings.MaxContracts) return false;
        var stopRisk = _settings.InitialStopPoints * (_settings.TickValue / _settings.TickSize) * Math.Abs(quantity);
        return stopRisk <= _settings.MaxRiskPerTrade;
    }

    public bool ShouldExitShort(decimal high, decimal low) => CurrentExitReasonShort(high, low) != "NONE";

    public string CurrentExitReasonShort(decimal high, decimal low)
    {
        if (_entryPrice is null || _position >= 0) return "NO_POSITION";
        if (_settings.ProfitTargetPoints > 0m && low <= _entryPrice.Value - _settings.ProfitTargetPoints) return "PROFIT_TARGET";
        if (high >= _entryPrice.Value + _settings.InitialStopPoints) return "INITIAL_STOP";
        return "NONE";
    }

    public bool CanEnter(int quantity, decimal referencePrice)
    {
        if (quantity <= 0 || quantity > _settings.MaxContracts) return false;
        var stopRisk = _settings.InitialStopPoints * (_settings.TickValue / _settings.TickSize) * quantity;
        return stopRisk <= _settings.MaxRiskPerTrade;
    }

    public ExecutionFill Simulate(SimulationOrder order, MarketEvent marketEvent)
    {
        if (order.Quantity > 0)
        {
            if (!CanEnter(order.Quantity, marketEvent.Price)) throw new InvalidOperationException("Risk budget rejected simulated entry.");
            _entryPrice = marketEvent.Price;
            _highestPrice = marketEvent.Price;
            _position = order.Quantity;
        }
        else if (order.Quantity < 0)
        {
            _entryPrice = null; _highestPrice = null; _position = 0;
        }
        var direction = Math.Sign(order.Quantity);
        var slippage = _settings.SlippageTicksPerSide * _settings.TickSize * direction;
        var fillPrice = marketEvent.Price + slippage;
        return new ExecutionFill(order.OrderId, marketEvent.Timestamp, fillPrice, order.Quantity,
            $"RISK_MANAGED|latency={_settings.EstimatedRoundTripLatencyMs}ms|slippage={_settings.SlippageTicksPerSide}ticks|commission={_settings.CommissionPerSide:F2}/side|reason={order.Reason}");
    }

    public bool ShouldExitLong(decimal high, decimal low)
    {
        return CurrentExitReason(high, low) != "NONE";
    }

    public string CurrentExitReason(decimal high, decimal low)
    {
        if (_entryPrice is null || _position <= 0) return "NO_POSITION";
        _highestPrice = Math.Max(_highestPrice ?? high, high);
        if (_settings.ProfitTargetPoints > 0m && high >= _entryPrice.Value + _settings.ProfitTargetPoints) return "PROFIT_TARGET";
        if (low <= _entryPrice.Value - _settings.InitialStopPoints) return "INITIAL_STOP";
        if (_highestPrice.Value >= _entryPrice.Value + _settings.TrailingActivationPoints && low <= _highestPrice.Value - _settings.TrailingDistancePoints) return "TRAILING_STOP";
        return "NONE";
    }

    public decimal CommissionFor(int contracts) => Math.Abs(contracts) * _settings.CommissionPerSide;
    public decimal SlippageCostFor(int contracts) => Math.Abs(contracts) * _settings.SlippageTicksPerSide * _settings.TickValue;
    public decimal RoundTripEstimatedFrictionFor(int contracts = 1) => Math.Abs(contracts) * _settings.EstimatedRoundTripFriction;
}
