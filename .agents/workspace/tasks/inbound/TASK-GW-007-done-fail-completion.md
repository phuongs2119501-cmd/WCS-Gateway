# TASK-GW-007: Bind missing main parses onto feat — done/fail + directionBlock2 + gateExport (PLC1/PLC2)

- **Spec:** specs/SPEC-GW-007-done-fail-completion.md
- **Phase:** inbound
- **Dispatched:** 2026-06-25 by gateway-pm
- **Status:** done (committed 0cc8cd1, build clean — handed to po for live test 2026-06-25)
- **Owner answers (2026-06-25):** Q1/Q2 fail→PLC stops job, surface-only no retry (matches impl) · Q3 offsets confirmed · Q4 one crane+shuttle per PLC (item D closed) · Q5 PLC zeroes offsets on done → completion-latch follow-up = TASK-GW-008

## Objective (one line)
Port the parses that exist on `main` but are missing on feat — done/fail (stop hung jobs), directionBlock2 (DBW84), gateExport (DBW80) — and surface them in status.

## Files in scope (paths only — Codex reads the repo)
- `WcsSystem/Wcs.PlcService/Services/LocationRouter.cs`   (CheckComplete: add fail read + fail finish)
- `WcsSystem/Wcs.PlcService/Services/SystemService.cs`    (read directionBlock2 DBW84 per PLC)
- `WcsSystem/Wcs.PlcService/Services/PlcBarcodeReader.cs` (read gateExport DBW80 per PLC)
- `WcsSystem/Wcs.PlcService/Services/SystemState.cs`      (add Done1/Fail1/Done2/Fail2, DirectionBlock2_1/2, GateExport1/2)
- `WcsSystem/Wcs.PlcService/Controllers/HomeController.cs` (/api/status: replace hardcoded done/fail=false and gateExport=0)
- `WcsSystem/Wcs.PlcService/Controllers/StatusController.cs` (/status: add the new fields)
- `WcsSystem/Wcs.PlcService/Plc/SimS7Backend.cs`          (optional: fail-path sim case)

## Acceptance criteria
- [ ] `plcFail` read per PLC; failed job resets cmd bit + finishes (no hang)
- [ ] `SystemState.Done1/Fail1/Done2/Fail2` live; `/api/status` + `/status` emit them (no hardcoded false)
- [ ] `SystemState.DirectionBlock2_1/2` live from DBW84 (B)
- [ ] `SystemState.GateExport1/2` live from DBW80; `/api/status` no longer hardcodes `gateExport=0` (C)
- [ ] PLC1/PLC2 paths symmetric
- [ ] `dotnet build` (WcsSystem/) passes

## Do
- Match the existing Vietnamese comment style.
- Use `S7Connector` Try* methods; check their bool returns.
- Keep PLC1/PLC2 paths symmetric. Offsets are already in `Db500Map.g.cs` — reuse `Db500Map.plcDone` / `plcFail`.
- **done/fail offsets: one-time sync to `main` (done=DBX52.0, fail=DBX52.1), same offset for every PLC — already aligned, just confirm. After this, the feat branch (`Db500Map.g.cs`) is the source of truth; do NOT chase main's later changes.**

## Don't
- Change any DB500 offset / command bit without a contract update.
- Add blocking I/O, sleeps, or `.Result`/`.Wait()` to a 50ms `Update()`.
- Touch the heartbeat write (`Db500Map.wcsHeartbeat`, DBX82.0).
- Implement per-crane LastLocation / serviced-cell echo here (out of scope — gateway-PLC-owner pending).
- **Port main's crane2/shuttle2 distinct offsets (DBW62/64, DBW72/74/76) — DEFERRED (item D). Per po: handle only AFTER GW-007's done/fail (Q1) is resolved. feat's one-unit-per-PLC topology may be correct; do not touch.**

## Dispatch decision (2026-06-25)
Dispatched WITHOUT waiting on the PLC-owner answers — be-pm NOT notified yet (we update our side first).
- **B (directionBlock2) + C (gateExport): fully unblocked — implement now.** Pure missing-parse ports, no semantics in question.
- **A (done/fail): implement with a documented default, flag for confirmation.** Until PLC-owner answers Q1/Q2:
  - Treat `plcFail` as **latched** — read it each cycle for the in-flight `_targetPlc`; on `fail==true` reset the cmd bit + `FinishJob()` marked failed (mirror of the existing `done` path). This is correct whether the signal is latched or pulsed-but-held-≥1-cycle; revisit only if the owner says it's a sub-50ms pulse.
  - On fail, **surface-only** for now (set state + status, log). Do NOT wire retry/alarm yet — that waits on Q2 and ties into contract.inbound-retry / contract.manual-task-retry.
  - Mark these two assumptions in REPORT-GW-007 §Risks so they're revisited when the owner answers.
- **D (crane2/shuttle2 topology): NOT in this dispatch** — deferred until after A (Q1) is resolved.

## Report (mandatory)
When done, write `tasks/inbound/REPORT-GW-007.md`: **what changed / how verified / risks / follow-ups.**
