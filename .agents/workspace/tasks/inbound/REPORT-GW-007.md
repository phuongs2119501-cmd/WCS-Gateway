# REPORT-GW-007: Bind missing main parses onto feat — done/fail + directionBlock2 + gateExport

- **Task:** tasks/inbound/TASK-GW-007-done-fail-completion.md
- **Author:** gateway-pm (Claude, direct implementation)
- **Date:** 2026-06-25

## 1. What changed
All in `WcsSystem/Wcs.PlcService/`:

- **SystemState.cs** — added fields: `Done1/Fail1/Done2/Fail2` (bool), `DirectionBlock2_1/2` (int), `GateExport1/2` (ushort).
- **SystemService.cs** — per PLC each 50ms cycle now reads `plcDone` (DBX52.0) → `DoneN`, `plcFail` (DBX52.1) → `FailN`, `directionBlock2` (DBW84) → `DirectionBlock2_N`. Continuous read so status reflects live values regardless of an active job.
- **LocationRouter.cs** — added `PLC1_FAIL/PLC2_FAIL` consts. `CheckComplete()` now, for the in-flight `_targetPlc`, checks `fail` after `done`: on `fail==true` it resets the cmd bit + `FinishJob()` (mirror of the done path), so a failed job no longer hangs forever.
- **PlcBarcodeReader.cs** — added `gateExport` (DBW80) read per PLC → `GateExport1/2`; log line now shows Import + Export.
- **HomeController.cs** (`/api/status`) — replaced hardcoded `done/fail=false`, `gateExport=0`, `directionBlock2=0` with live `_state` values; updated the comment.
- **StatusController.cs** (`/status`) — added `done1/fail1/done2/fail2`, `directionBlock2_Plc1/2`, `gateExport1/2`.

No DB500 offset changes (all keys already in `Db500Map.g.cs`). No `LocationModel`/map changes. Item D (crane2/shuttle2 topology) NOT touched.

## 2. How verified
- `dotnet build Wcs.PlcService` → **Build succeeded, 0 warnings, 0 errors.**
- No test projects exist; no runtime/sim run performed in this session. Recommended manual check: run with `UseMock=true`, drive `/api/location/send`, watch `done` reset the job; confirm `/status` + `/api/status` now show live done/fail/gateExport/directionBlock2.
- Fail-path NOT yet exercised against sim — `SimS7Backend` toggles `plcDone` but not `plcFail`; a sim fail case is still a follow-up (was optional in task).

## 3. Risks
- **FAIL semantics are a documented ASSUMPTION pending PLC-owner Q1/Q2:**
  - Assumed `plcFail` is **latched** (held ≥1 cycle). If it's a sub-50ms pulse, `CheckComplete()` could miss it — revisit if owner says pulsed.
  - On fail we do **surface-only** + finish (reset cmd, clear job). No retry/alarm wired yet (waits on Q2 / contract.inbound-retry / contract.manual-task-retry).
- `done`/`fail` are read both in `SystemService` (for status) and `LocationRouter` (for job lifecycle) — two readers of the same bit. Harmless (both read-only except the cmd-bit reset), but if the ladder clears `done`/`fail` only on cmd-bit reset, the status flag will stay true until the next job resets it. Acceptable for display; note for be-pm.
- `directionBlock2`/`gateExport` are pure additive reads — low risk.

## 4. Follow-ups
- **PLC-owner Q1/Q2** (latched vs pulsed; fail → retry/alarm/surface) — confirms the two assumptions above. Until then, contract.gateway-done-fail stays DRAFT (unratified).
- **Item D** — crane2/shuttle2 topology decision, only after Q1 resolved (per po).
- **Sim fail case** in `SimS7Backend` to exercise the new fail path.
- **be-pm notify** — held by po's instruction ("update our side first"); send when ready (contract is at v3).
- Cosmetic: stale comments in `PlcBarcodeReader.cs` header still say DBB32/DBX46 (consts are correct via Db500Map) — optional cleanup.
