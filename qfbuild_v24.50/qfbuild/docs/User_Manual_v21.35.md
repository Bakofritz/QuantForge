# QuantForge v21.35 Detailed User Manual

## 1. Normal research operation
Keep the live pathway disabled. Research, backtesting, optimization, imported-strategy auditing, and read-only analysis continue independently of live trading.

## 2. Authentication layers
A future platform adapter creates a short-lived challenge for the device-bound authenticator and a separate challenge for the independent second factor. The coordinator requires both proofs, fresh challenges, user binding, and distinct bindings.

## 3. Audit integrity
Live security events are appended to a hash-linked audit chain. Each entry includes the previous record hash. The chain can be verified before a live order is admitted.

## 4. Broker capabilities
A broker adapter must expose a capability snapshot. Order admission requires order submission, cancellation, execution reports, and heartbeat capabilities, plus a connected state.

## 5. Risk heartbeat
The risk monitor records a heartbeat and a maximum allowed heartbeat age. Once the heartbeat becomes stale, the final order gate blocks new orders. The safety coordinator can automatically emergency-disarm the pathway.

## 6. Final order boundary
The system evaluates all live-order requirements together. Passing the gate creates an admission result and may create a user-facing order preview. The current build does not call a broker order-submission API.

## 7. Emergency behavior
If risk health becomes stale or failed, automatic emergency disarm is available through the safety coordinator. Communication dependency changes while armed remain subject to the pathway controller's existing emergency-disarm rules.

## 8. Important limitation
The platform authentication adapters in this build are architectural contracts/deferred adapters. Native Android keystore/biometric/passkey APIs and real broker protocols must be integrated and separately validated before production use.
