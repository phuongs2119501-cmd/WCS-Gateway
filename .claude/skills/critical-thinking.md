---
name: critical-thinking
description: Adversarial PLC-safety review for WCS-Gateway. Invoke to attack a proposed change BEFORE dispatching it to Codex or merging — surfaces ways it could stall a shuttle/crane, break the heartbeat, mishandle reconnect, route to the wrong PLC, or violate the acceptedKey idempotency contract.
---

# critical-thinking (WCS-Gateway)

Assume the change is wrong until it survives this list. This is the gate between "looks done" and "safe to run on a machine that moves pallets."

## Safety checklist (answer each explicitly)

1. **Could this stall or strand a shuttle/crane?** If a read fails, a job never gets its DONE, or a flag never resets — does the state machine hang `_jobActive` forever? Is there a way out?
2. **Heartbeat intact?** Does any new branch, early return, or exception skip the `DB500.DBX74.0` write? The PLC may E-stop if the PC looks dead.
3. **Reconnect / throttle behavior** preserved? Did you accidentally treat an address error as a disconnect (link flaps) or a disconnect as an address error (link stays "up" while dead)?
4. **Wrong-PLC routing?** `Bin` and `Xin` decide PLC1 vs PLC2 — i.e. which physical machine moves. Re-derive the routing for the boundary cases (Bin=2 at Xin=13 vs 14, out-of-range Xin).
5. **Idempotency / contention:** does this respect `contract.crane-contention-lock` (`acceptedKey` set on bind, == X-Idempotency-Key, cleared on complete/fail)? Could a duplicate `/api/location/send` double-dispatch?
6. **Read-as-zero:** every `TryRead` bool checked? A `false` must not write a real `0`/`false` into `SystemState`.
7. **Cycle budget:** did anything blocking sneak into a 50ms `Update()`?
8. **Cancel window:** late cancel (after `acceptedKey` populated) must be rejected, not acted on (frozen with be-pm).

## Contract blast-radius
Any DB500 offset, command bit, `/status` field, or `LocationModel` field changed? Then BE and/or the PLC program are affected. Name the doc to update (`contract.gateway-db-keys`, `contract.plc.*`, `contract.inbound-location-send`) and the peer to notify. A change that passes code review but skips this is **not** done.

## Verdict
End with one of: **SAFE** (and why), **SAFE WITH NOTES** (list them for the REPORT), or **BLOCK** (the specific failure + the smallest fix).
