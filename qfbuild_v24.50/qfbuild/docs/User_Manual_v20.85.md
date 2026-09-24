# QuantForge v20.85 User Manual

## Purpose
v20.85 provides the first governed adapter for submitting research cases to the existing lease-aware execution pipeline.

## Admission requirements
A batch must have:
1. a qualified native runtime;
2. a positive memory estimate per case;
3. a memory budget large enough for the admitted concurrency;
4. read-only research authority;
5. a valid resource policy;
6. existing lease-aware execution infrastructure.

## Fail-closed behavior
If any required admission condition is missing, execution is not started and a deterministic stop code is returned.

## Authority boundary
The adapter is for research/backtesting execution. It does not authorize live orders, live trading, arbitrary imported-code execution, canonical data mutation, or undeclared application mutation.

## Operational note
The current coordinator intentionally remains conservative and executes one case at a time. Parallel execution will require a later qualified scheduler/resource implementation and runtime testing.
