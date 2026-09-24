# QuantForge User Manual — v20.46

## What changed
QuantForge now treats a repeated attempt to save the same finished research result as a safe retry instead of creating another copy.

## Normal behavior
If the same terminal result is submitted again with the same receipt identity and contents, the storage layer accepts the retry without adding a duplicate receipt.

If a different result is submitted for a job that already has a terminal result, QuantForge rejects it and records the condition as an integrity conflict.

## Why this matters
Workers can lose a connection or crash immediately after a save. On restart, they may not know whether the previous save succeeded. Idempotent terminal commits allow the worker to safely retry.

## Safety
This feature only concerns research-result storage. It does not grant trading or order authority.

## Current limitation
Native execution of the SQLite validation adapter requires the .NET runtime/SDK and was not available in the current build environment.
