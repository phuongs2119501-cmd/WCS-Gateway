# SPEC-GW-007: Bind missing main-branch parses onto feat — done/fail + directionBlock2 + gateExport (PLC1/PLC2)

- **Status:** draft
- **Author:** gateway-pm
- **Date:** 2026-06-24
- **Related:** contract.gateway-done-fail (new) · contract.gateway-command-flow v1 · review.gateway-command-flow §C/§E · SPEC-GW-006 · be-pm msgs 697db91b / 1b93d0c1

## Goal (physical/operational outcome)
When a crane/shuttle job finishes on a PLC, WCS must learn the real outcome — **succeeded (done)** or **failed (fail)** — for each PLC independently, and must not hang when a job fails. Today only `done` is observed (for command-reset); `fail` is invisible, so a failed job waits forever for a `done` that never comes.

## Why now
be-pm's live bridge test (sim) confirmed WCS has no authoritative completion signal: `/api/status` hardcodes `done1/fail1/done2/fail2 = false`, and `plcFail` is never read. be-pm currently infers completion from crane busy→free (a heuristic). main already binds done/fail in `SystemService`; the feat branch regressed this. This unblocks the `contract.gateway-command-flow` freeze + real-PLC E2E.

## Scope
This task binds the parses that exist on `main` but are missing on the feat branch — three safe, same-intent missing-parse ports:
- **In (A) done/fail:** Read `plcFail` per PLC; handle fail in the job lifecycle (reset cmd + finish/mark failed); store done/fail per PLC in `SystemState`; surface real values in `/api/status` + `/status`.
- **In (B) directionBlock2 (`DBW84`):** main reads it into `state.DirectionBlock2` (SystemService). feat maps `Db500Map.directionBlock2` but never reads it and has no `SystemState` field. Add `DirectionBlock2_1/2` to `SystemState`, read per PLC in `SystemService`, surface in status.
- **In (C) gateExport (`DBW80`):** main reads both gateImport + gateExport. feat reads only `gate` (DBW78) and `/api/status` hardcodes `gateExport1/2 = 0`. Add `GateExport1/2` to `SystemState`, read `Db500Map.gateExport` in `PlcBarcodeReader`, replace the hardcoded `0` in `HomeController`.
- **Out (D) crane2/shuttle2 distinct offsets — DEFERRED, do NOT port here.** main reads a 2nd crane (`DBW62/64`) + 2nd shuttle (`DBW72/74/76`) from the SAME PLC; feat's topology reads one crane+shuttle per PLC via two connectors at the same offset, so those map entries are intentionally unused. Porting main's reads would be WRONG unless the real wiring is "2 units on 1 PLC." Blocked on a PLC-owner topology answer — handle in a follow-up task AFTER GW-007 ships (see Follow-up).
- **Out:** Per-crane `LastLocation` / serviced-cell echo (needs gateway-PLC-owner answer on whether crane.{X,Z} holds destination at busy→free). WCS→PLC heartbeat (DBX82.0) and PLC→WCS frozen-CPU lifebit (separate backlog).

## Behavior
**ONE-TIME ALIGNMENT — sync done/fail offsets to `main` once, then this (feat) branch is the source of truth:**

| Signal | main (`DataPlc1`/`DataPlc2`) | feat (`Db500Map.g.cs`) | Status |
|---|---|---|---|
| done | `DB500.DBX52.0` (`PLC1_DONE`/`PLC2_DONE`) | `DB500.DBX52.0` (`plcDone`) | aligned ✅ |
| fail | `DB500.DBX52.1` (`PLC1_FAIL`/`PLC2_FAIL`) | `DB500.DBX52.1` (`plcFail`) | aligned ✅ |

Same offset for every PLC; read via its own connector (`_plc1` / `_plc2`), matching main's `PLCn_DONE == PLCn_FAIL` same-offset pattern. Parser: `TryRead<bool>` (sufficient). We borrowed main's offsets only to confirm the starting point — already aligned. After this alignment **the feat branch (`Db500Map.g.cs`) is authoritative; we do NOT track main's later offset changes.**

- **LocationRouter.CheckComplete()** (the active job consumer): for the in-flight `_targetPlc`, after reading `done`, ALSO read `fail`.
  - `done == true` → reset cmd bit, `FinishJob()` (success) — unchanged.
  - `fail == true` → reset cmd bit, finish job marked **failed** (do NOT keep waiting). Log it.
  - Keep PLC1/PLC2 paths symmetric.
- **SystemState**: add `Done1/Fail1/Done2/Fail2` (bool), `DirectionBlock2_1/2` (int), `GateExport1/2` (ushort) — set where read.
- **Status surfaces**: `/api/status` (HomeController) replace hardcoded `false`/`0` with the real `SystemState` values; add the same fields to `/status` (StatusController).
- **directionBlock2 (B)**: read `Db500Map.directionBlock2` (DBW84) per PLC in `SystemService` → `SystemState.DirectionBlock2_1/2`.
- **gateExport (C)**: read `Db500Map.gateExport` (DBW80) per PLC in `PlcBarcodeReader` → `SystemState.GateExport1/2`.

## Contract impact
- DB500 offsets touched: **none** (DBX52.0/.1, DBW84, DBW80 all already mapped in `Db500Map.g.cs`).
- `/status` + `/api/status` fields changed: `done1/fail1/done2/fail2` become live (were hardcoded false / absent); `gateExport1/2` become live (were hardcoded 0); `directionBlock2_*` added.
- `LocationModel` changed: none.
- Docs to update + peer to notify: **contract.gateway-done-fail** → be-pm; reference from contract.gateway-command-flow.

## Acceptance criteria (checkable)
- [ ] `plcFail` is read per PLC; a failed job resets its cmd bit and finishes (no hang).
- [ ] `SystemState.Done1/Fail1/Done2/Fail2` reflect live PLC reads.
- [ ] `SystemState.DirectionBlock2_1/2` reflect live DBW84 reads (B).
- [ ] `SystemState.GateExport1/2` reflect live DBW80 reads; `/api/status` no longer hardcodes `gateExport=0` (C).
- [ ] `/api/status` and `/status` emit the live done/fail + directionBlock2 + gateExport values (no hardcoded false/0).
- [ ] PLC1 and PLC2 paths symmetric; no blocking I/O / sleeps / `.Result` added to the 50ms loop.
- [ ] `dotnet build` (WcsSystem/) passes.
- [ ] Behavior validated against `SimS7Backend` (it already toggles `plcDone`); add/confirm a fail-path sim case if feasible.

## Open questions — ANSWERED by PLC owner 2026-06-25
1. **(Q1/Q2) On fail:** the PLC fails the flow and **stops the job itself**. → WCS detects fail → reset cmd → FinishJob, **surface-only, NO auto-retry**. Treated as latched (persists since the job is stopped). Current implementation matches. ✅
2. **(Q3) Offsets confirmed:** done=DBX52.0, fail=DBX52.1, same for both PLCs. ✅
3. **(Q4) Topology:** ONE crane + ONE shuttle per PLC. → Item D RESOLVED, feat is correct, do NOT port main's crane2/shuttle2 offsets. ✅
4. **(Q5) On done, the PLC zeroes ALL model offsets** (positions reset to 0 when completion is reported to the Gateway).

## Follow-up — driven by Q5 (NEW)
The PLC zeroing offsets on done means:
- **Serviced cell must come from `LastLocation` (dispatch echo), not from reading `crane.{X,Z}` after done** (which is 0). Confirms dispatch-echo LastLocation is the authoritative cell source; a per-crane LastLocation reading positions would be wrong.
- **`done` is transient → `/status` pollers (be-pm, 1s) can miss it.** Recommend latching the last completion in `FinishJob()`: `{ cell (LastLocation snapshot), outcome (done|fail), timestamp }`, persisted until next dispatch, exposed in status. This becomes the authoritative completion signal that replaces be-pm's busy→free heuristic.
- **Positions read 0 after a job = idle/reset, not "at origin."** Document for be-pm/FE.

## Resolved / closed
- **D — crane2/shuttle2 topology:** CLOSED by Q4 (one each per PLC). No code; main's extra offsets intentionally unused.
