# TASK-<id>: <short title>

- **Spec:** specs/SPEC-<id>.md
- **Phase:** <phase>
- **Dispatched:** YYYY-MM-DD by gateway-pm
- **Status:** queued | dispatched | reported | verified

## Objective (one line)
<...>

## Files in scope (paths only — Codex reads the repo)
- `WcsSystem/Wcs.PlcService/...`

## Acceptance criteria
- [ ] <checkable behavior>
- [ ] `dotnet build` (WcsSystem/) passes

## Do
- Match the existing Vietnamese comment style.
- Use `S7Connector` Try* methods; check their bool returns.
- Keep PLC1/PLC2 paths symmetric.

## Don't
- Change any DB500 offset / command bit without a contract update.
- Add blocking I/O, sleeps, or `.Result`/`.Wait()` to a 50ms `Update()`.
- Touch the `DB500.DBX74.0` heartbeat write.

## Report (mandatory)
When done, write `tasks/<phase>/REPORT-<id>.md` containing:
**what changed / how verified / risks / follow-ups.**
