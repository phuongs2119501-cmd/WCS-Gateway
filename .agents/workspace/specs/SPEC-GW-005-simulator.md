# SPEC-GW-005: Fake-PLC simulator mirroring DB500

- **Status:** ratified
- **Author:** gateway-pm
- **Date:** 2026-06-05
- **Related:** spec.gateway-simulator (published), contract.gateway-db-keys, be-pm thread 3.3, PO Q1 offline-LAN demo

## Goal (physical/operational outcome)
A fake-PLC backs the identical DB500 keyspace so the whole Gateway (50ms Worker loop, /status, command
echo) runs unchanged with no real hardware — enabling the offline-LAN demo (PO Q1) and dev without a PLC.
Swappable with a real device on the same `DB500.*` keys via config.

## Why now
PO Q1 (offline demo) is top of the priority stack; be-pm 3.3 asks Gateway to produce a simulator response
matching a real Task. Today mock↔real is an appsettings edit but there is no in-memory DB500 backend that
replays a task lifecycle.

## Scope
- **In:** `IS7Backend` seam behind `S7Connector`; `RealS7Backend` (today's S7.Net) + `SimS7Backend`
  (in-memory `byte[]` per DB, same address grammar); task echo loop; seedable fixtures; `UseMock` wiring.
- **Out:** error injection, intra-PLC task queueing, realistic kinematics (v2 if PO funds fault drills).

## Behavior
`S7Connector` delegates leaf I/O (TryRead/TryWrite/TryReadChar/Read) to `IS7Backend`; lock/reconnect/
error logic stays. `SimS7Backend` parses `DB500.DBXa.b / DBWn / DBBn` against a `byte[]` (>=72B, big-endian
words). Echo loop per tick: detect rising command bit → craneBusy=1/Free=0, latch target (xout,zout) →
advance craneX/Z + selected shuttle over N ticks (default ~40) → on arrival plcDone=1, Busy=0, Free=1 →
Gateway clears cmd bit & finishes → sim clears plcDone → idle. Fixtures (JSON) seed positions, battery,
barcode+ok/ng, gate, sysAuto/Running.

## Contract impact
- DB500 offsets touched: none (mirrors the canonical map).
- `/status` / `LocationModel`: unchanged — sim output is byte-identical in shape to real.
- Docs: spec.gateway-simulator published. Heads-up to be-pm when seam lands (nothing BE-visible changes).

## Acceptance criteria (checkable)
- [ ] `UseMock=true` boots with no PLC; `/status` returns seeded fixture state.
- [ ] POST /api/location/send drives a full echo: Busy→position advance→plcDone→job finished→idle.
- [ ] `RealS7Backend` path unchanged; switching backends needs only config.
- [ ] `dotnet build` (WcsSystem/) passes.
- [ ] Ran through `critical-thinking` skill → verdict recorded.

## Open questions
- N-tick duration + whether shuttle motion is required for the demo or crane-only suffices (ask PO at dispatch).
