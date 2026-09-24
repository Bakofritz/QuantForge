# QuantForge v21.75 — Detailed User Manual

## Live execution remains opt-in
Do not treat the presence of broker transport interfaces as live trading being enabled. The live pathway remains OFF until the existing security and authorization workflow explicitly enables it.

## Broker connection states
- **Disconnected:** no broker execution is permitted.
- **Connecting:** setup is in progress; no execution is permitted.
- **Connected:** transport may be used by an already-authorized execution flow.
- **Degraded:** execution must be treated as unsafe until health is restored.
- **Failed:** execution is blocked.

## Startup recovery
After restart, QuantForge inspects persisted execution intents. Any intent with an ambiguous outcome (`Unknown`) blocks new live execution. The user/operator must reconcile the intent with the broker; the system does not blindly resubmit it.

## Platform secure storage
Android Keystore and Windows protected storage are represented by explicit adapters. Until a native bridge is configured, they report unavailable and fail closed. Do not substitute plain-text files for production key storage.

## Broker transport
The transport layer is deliberately separated from authorization. A connector is responsible for authenticated communication and normalized broker responses; the governance layer remains responsible for deciding whether execution is authorized.

## Existing confirmation workflow
The existing preview, security-binding, confirmation-token, final-admission, idempotency, and reconciliation controls remain mandatory.
