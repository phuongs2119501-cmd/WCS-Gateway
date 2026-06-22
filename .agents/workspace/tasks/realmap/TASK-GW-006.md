# TASK-GW-006: Sim drives only the SELECTED shuttle; verify real-offset contract

- **Spec:** specs/SPEC-GW-006-realmap-sim-contract.md
- **Phase:** realmap
- **Dispatched:** 2026-06-22 by gateway-pm
- **Status:** dispatched

## Objective (one line)
Make `SimS7Backend` mark Busy + move ONLY the routed shuttle (per `modeCraneShuttle` DBW2), leaving the
other shuttle and crane-2 Free/stationary, so be-pm's per-device Busy→Free move-history is realistic; then
verify the whole SPEC-GW-006 real-offset contract (TC1–TC6) builds and behaves.

## Files in scope (paths only — Codex reads the repo)
- `WcsSystem/Wcs.PlcService/Plc/SimS7Backend.cs`   ← the change lives here
- `WcsSystem/Wcs.PlcService/Plc/Db500Map.g.cs`     ← read-only, source of every offset
- `WcsSystem/Wcs.PlcService/Services/LocationRouter.cs` ← read-only, shows the write ordering + mode selection
- `WcsSystem/Wcs.PlcService/Config/db500-keys.json` ← read-only, v3 canonical map
- `WcsSystem/Wcs.PlcService/Controllers/HomeController.cs` ← read-only, `/api/status` already done

## The change (what's wrong today)
`StartJob` sets BOTH `shuttle1Busy` and `shuttle2Busy`; `AdvanceJob` steps BOTH shuttle slots. Reality:
each job uses the crane + exactly ONE shuttle. Fix:
1. Resolve the selected shuttle from `Db500Map.modeCraneShuttle` (DBW2): `1`→shuttle-1, `2`→shuttle-2.
2. Resolve it **lazily** — `LocationRouter` writes the command bit BEFORE it writes `modeCraneShuttle`
   in the same loop pass, so DBW2 is not yet set at the cmd-bit rising edge. Read DBW2 on the first
   pump tick (or just-in-time in `AdvanceJob`), not inside `StartJob`. If DBW2 is still 0, default to
   shuttle-1.
3. Only the selected shuttle goes Busy (and back to Free on completion) and has its X/Z/B stepped/snapped.
   The other shuttle keeps its seeded Free state and position. crane-1 still always moves; crane-2 never
   toggles Busy for a single-PLC job.

## Acceptance criteria
- [ ] TC1 Seed/idle: `UseMock=true` boots with no PLC; `/status` + `/api/status` return seeded fixture state.
- [ ] TC2 Crane moves: command → `craneBusy=true`, `craneX/Z` (DBW58/60) step toward target, then `craneFree`+`plcDone`.
- [ ] TC3 Selected shuttle only: `modeCraneShuttle=1` → shuttle-1 Busy+moves, shuttle-2 stays Free/stationary; `=2` → reverse; crane-2 never Busy.
- [ ] TC4 System status: toggling `sysStop`/`sysError` flips `system1.stop/error`.
- [ ] TC5 Heartbeat: writing `wcsHeartbeat` (DBX82.0) is accepted with no side effect / no error.
- [ ] TC6 Barcode result: writing `barcodeOk`/`barcodeNg` reads back; barcode read at DBB36.
- [ ] No literal byte offsets remain in `SimS7Backend` (everything via `Db500Map.*`).
- [ ] `dotnet build` (WcsSystem/) passes.

## Do
- Match the existing Vietnamese/English comment style already in `SimS7Backend`.
- Keep every offset coming from `Db500Map.*` — no new literal byte numbers.
- Keep the crane motion and the `plcDone` handshake exactly as they work now.

## Don't
- Change any DB500 offset / command bit / the `db500-keys.json` map.
- Touch the `wcsHeartbeat` (DBX82.0) write handling.
- Add blocking I/O, sleeps, or `.Result`/`.Wait()` anywhere in the read/pump path.
- Move crane-2 or wire the deferred real-PLC keys (`gateExport`, `done/fail`, `directionBlock2`).
- Edit `HomeController` `/api/status` — it is already correct.

## Report (mandatory)
When done, write `tasks/realmap/REPORT-GW-006.md` containing:
**what changed / how verified / risks / follow-ups.**
