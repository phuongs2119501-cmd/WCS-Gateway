# TASK-GW-008: Latch last completion (cell + outcome) — survive PLC offset-zeroing on done

- **Spec:** specs/SPEC-GW-007-done-fail-completion.md (Follow-up section — driven by PLC-owner Q5)
- **Phase:** inbound
- **Dispatched:** queued 2026-06-25 by gateway-pm (handle in a later session)
- **Status:** queued

## Why (PLC-owner Q5, 2026-06-25)
On `done`, the PLC zeroes ALL model offsets. So `crane.{X,Z}` reads 0 after completion and the raw `done` bit is transient — `/status` pollers (be-pm, ~1s) will miss it. Need a persistent completion record so be-pm can drop the busy→free heuristic.

## Objective (one line)
In `FinishJob()`, snapshot `{ cell = LastLocation, outcome = done|fail, plc = 1|2, timestamp }` into a persistent `LastCompletion`, kept until the next dispatch, and expose it in status.

## Files in scope (paths only)
- `WcsSystem/Wcs.PlcService/Services/SystemState.cs`        (add `LastCompletion` model + property)
- `WcsSystem/Wcs.PlcService/Services/LocationRouter.cs`     (set it in FinishJob — distinguish done vs fail)
- `WcsSystem/Wcs.PlcService/Controllers/StatusController.cs` + `Controllers/HomeController.cs` (expose `lastCompletion`)
- (new) a small `CompletionModel` if cleaner than reusing LocationModel

## Acceptance criteria
- [ ] `LastCompletion` is set on every FinishJob with the correct outcome (done vs fail) and the dispatched cell.
- [ ] It persists after the PLC zeroes offsets (does NOT read positions back from PLC).
- [ ] Exposed in `/status` + `/api/status`.
- [ ] `dotnet build` (WcsSystem/) passes.

## Do
- Source the cell from `LastLocation` (what WCS dispatched), NOT from `crane.{X,Z}` (zeroed on done).
- Keep PLC1/PLC2 symmetric; FinishJob already knows `_targetPlc` + `_commandType`.

## Don't
- Don't read serviced position back from the PLC after done (it's 0).
- Don't add blocking I/O to the 50ms loop.

## Notes / dependencies
- po will verify the `/status` shape and reuse it later — coordinate field names with be-pm before ratifying contract.gateway-done-fail (→ v4) and notifying.
- Also surface for be-pm/FE: positions = 0 after a job means idle/reset, not "at origin."

## Report (mandatory)
When done, write `tasks/inbound/REPORT-GW-008.md`: what changed / how verified / risks / follow-ups.
