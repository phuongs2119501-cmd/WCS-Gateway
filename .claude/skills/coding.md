---
name: coding
description: WCS-Gateway coding playbook. Invoke before writing or reviewing C# in WcsSystem (or TcpSocket) so changes respect the 50ms Worker loop, the DB500 memory map, the S7Connector Try* contract, and the singleton/SystemState architecture. Use whenever a task touches Services/, Plc/, Controllers/, or Worker.cs.
---

# coding (WCS-Gateway)

You are editing a real-time bridge to two Siemens S7-1200 PLCs. Code here moves physical cranes and shuttles. Correctness and timing beat cleverness.

## Non-negotiables

1. **The 50ms loop is a budget.** `Worker.ExecuteAsync` runs every 50ms in a fixed order: System → Crane → Barcode → **Router (writes Mode first)** → Shuttle. Never add blocking I/O, `Thread.Sleep`, `.Result`/`.Wait()`, or unbounded loops inside a per-cycle `Update()`. If work is slow, it does not belong in the loop.
2. **DB500 is THE contract.** Every offset (`DB500.DBXa.b`, `DBWn`, `DBBn`) is shared with the PLC program AND with BE. Changing an offset is a protocol change → update `contract.gateway-db-keys` / `contract.plc.*` on wcs-mcp and `post_message` be-pm. Never silently move an address.
3. **Always go through `S7Connector` Try* methods** (`TryRead<T>`, `TryReadChar`, `TryWriteBool`, `TryWriteInt16`). Never touch the raw `S7.Net.Plc`. They are lock-guarded, handle reconnect, and dedup error logs. Check the bool return — a `false` read must not be treated as a real `0`.
4. **Two PLCs, identical addresses.** PLC1 and PLC2 read the same DB500 offsets through different connectors. When you add a const for PLC1, the PLC2 twin is the *same string by design* — don't "fix" the duplication into one shared const without understanding it routes to a different physical machine.
5. **Heartbeat is sacred.** `DB500.DBX74.0` is written every cycle to prove the PC is alive. Don't gate it behind other logic or skip it on error paths.

## Patterns to match

- Read into locals, commit to `SystemState` only when the whole read group succeeded (see `CraneService.ReadState` — all three bools guard one assignment).
- State machines (like `LocationRouter`) are flag-driven (`_jobActive`/`_commandSent`/`_targetPlc`). Keep transitions explicit; reset every field in the Finish path.
- Log on *change only* (`_lastBarcode`, `_p1Auto…`) — never spam the 50ms loop with per-cycle Console writes.
- Comments are **Vietnamese** to match the codebase. Keep that.

## Before you finish
- Did any DB500 offset, command bit, or `/status` field change? → it's a contract change, flag it (see the `discussing` skill).
- Run `dotnet build` in `WcsSystem/`. No test projects exist — verify by reasoning + build, and note manual-test steps in the REPORT.
- Idempotency: the frozen `contract.crane-contention-lock` expects an `acceptedKey` on crane status — check whether your change interacts with it.
