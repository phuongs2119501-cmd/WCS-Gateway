# SPEC-GW-006: Real-PLC map alignment, sim re-point + dual response contract

- **Status:** approved-for-dispatch (2026-06-22)
- **Author:** gateway-pm
- **Date:** 2026-06-21 (rev 2026-06-22)
- **Related:** SPEC-GW-005 (simulator), SPEC-GW-002 (db-keys catalog), docs/reconciliation.md, db500-keys v3, PO pre-plan 2026-06-21, `contract.plc.device-bits` v1 (offsets confirmed to be-pm 2026-06-22)

## Goal (physical/operational outcome)
With no real hardware, the Gateway must report the live warehouse truthfully on the **real** DB500
layout: a crane that moves shows its X/Z advancing, a shuttle that moves shows its X/Z/B advancing, and
a system status change (Auto/Stop/Error) shows up immediately. WCS sees this via the response contract.
The simulator must mirror the **real** offsets so what we preview offline equals what the real PLC will
report — no repeat of the Gemini wrong-offset drift.

## Why now
db500-keys was re-pointed to the real, hardware-verified offsets (v3). The simulator + fixture still use
the OLD hard-coded offsets, so a fresh run now reports wrong crane/shuttle/status — the exact symptom we
are eliminating. PO also wants WCS to consume a stable contract across gateway versions.

## Scope
- **In (all in OUR code, no Gemini files imported):**
  1. Re-point `SimS7Backend` + `sim-fixture.json` to derive every offset from `Db500Map` (no literals).
  2. Add **shuttle** motion to the sim job (today only the crane moves).
  3. Expose **two** response contracts: keep `GET /status` (our nested shape) and add
     `GET /api/status` (owner-compatible flat shape) — same `SystemState`, two projections.
  4. Contract test cases TC1–TC6 (below) as the acceptance gate.
- **Out (deferred):**
  - crane2/shuttle2 → PLC binding semantics (which connector exposes crane2). **Pending PO's gateway-dev
    confirmation** per pre-plan rule 1; do not finalize.
  - Real-PLC field wiring for the NEW keys (`crane2*`, `gateExport`, `plcFail`, `directionBlock2`,
    `reqAutoRun/reqStop`) into services — map-only for now.
  - WMS inbound command path (`POST /api/wms/send-command`) — separate spec if PO wants it.

## Behavior
- **Sim re-point:** `SimS7Backend.Seed/HandleBitWrite/StartJob/FinishJob/AdvanceJob/Pump` stop using
  literal byte offsets and use `Db500Map.*ByteStart`/parsed addresses. DbLength raised to cover DBW84
  (>=86). Heartbeat accepted at `wcsHeartbeat` (DBX82.0). Done at `plcDone` (DBX52.0).
- **Crane + shuttle motion:** on a rising command bit (DBX0.0–0.2) the job latches target (xout,zout,bout)
  and steps the crane (DBW58/60) plus **only the SELECTED shuttle** from start→target over `TicksPerMove`;
  on arrival sets busy→free + `plcDone`. Gateway clears the cmd bit → sim clears done → idle.
  - **Selected shuttle = `modeCraneShuttle` (DBW2):** `1`→shuttle-1 (DBW66/68/70), `2`→shuttle-2
    (DBW72/74/76). Only that shuttle goes Busy and moves; the OTHER shuttle (and crane-2) stay Free and
    stationary. Rationale: be-pm derives per-task move-history from each device's Busy→Free transition
    (`contract.plc.device-bits`), so a device that isn't part of the job must NOT toggle Busy.
  - **Ordering caveat:** `LocationRouter.SendToPlcN` writes the command bit *before* `modeCraneShuttle` in
    the same 50 ms loop pass, so the selector is not yet set at the cmd-bit rising edge. The sim MUST resolve
    the selected shuttle lazily (on the first pump tick, reading DBW2), not eagerly inside `StartJob`. If DBW2
    is still 0/stale, default to shuttle-1.
- **Dual contract (already implemented, keep correct):** `StatusController` (`/status`) unchanged. The flat
  owner-compatible `GET /api/status` lives on `HomeController` (NOT a separate `ApiStatusController`) and
  projects the same `SystemState` into the owner's flat key names (`crane1`, `shuttle1`, `barcodeOk1`,
  `gateImport1`, `done1/fail1`, …). One state, two read-only views; no Gemini code copied. Deferred real-PLC
  keys (`gateExport`, `done/fail`, `directionBlock2`) are emitted as documented defaults until bound.

## Contract impact
- DB500 offsets touched: none new (consumes db500-keys v3); fixes sim to match it.
- `/status` fields: unchanged. New `/api/status` endpoint added (additive, owner-compatible).
- Docs to update + peer to notify: docs/reconciliation.md (mark sim aligned). `fe-pm` NOT notified
  (PO pre-plan rule: fe-pm out of this project).

## Acceptance criteria (checkable)
- [ ] **TC1 Seed/idle:** `UseMock=true` boots with no PLC; `/status` + `/api/status` return seeded fixture
      state (crane free, sys auto+running) at the REAL offsets.
- [ ] **TC2 Crane moves:** command → `crane1.busy=true`, `crane1.x` steps toward target across polls, then
      `busy=false` + done; reads come from DBW58/60.
- [ ] **TC3 Shuttle moves (selected only):** with `modeCraneShuttle=1`, shuttle-1 (DBW66/68/70) goes
      Busy and X/Z/B advance toward target while shuttle-2 stays Free and stationary; with `=2`, the reverse.
      crane-2 never toggles Busy for a single-PLC job.
- [ ] **TC4 System status:** toggling `sysStop`/`sysError` (DBX54.2/.3) flips `system1.stop/error`.
- [ ] **TC5 Heartbeat:** Gateway writes DBX82.0 each loop; sim accepts without error.
- [ ] **TC6 Barcode result:** Gateway writes DBX50.0/.1 → reads back `barcodeOk1/Ng1`; barcode read at DBB36.
- [ ] Offset-integrity check: for every `/status` field there is exactly one `Db500Map` key, and the sim
      writes that same offset (no literal offsets remain in `SimS7Backend`).
- [ ] `/status` and `/api/status` reflect the same underlying state in one poll.
- [ ] `dotnet build` (WcsSystem/) passes.
- [ ] Ran through `critical-thinking` skill → verdict recorded.

## Open questions
- crane2/shuttle2 ↔ PLC-connector binding: deferred to PO's gateway-dev conversation (rule 1). Until then
  the sim seeds crane2/shuttle2 from the fixture but does not move them.
- `/api/status` exact key spelling: mirror owner's `SendDataAPI.GetJson()` field names verbatim? (assumed yes.)
