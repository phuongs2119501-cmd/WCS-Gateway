# TASK-GW-002: Db500Map generator + Service refactor

- **Spec:** specs/SPEC-GW-002-db-keys-catalog.md
- **Phase:** inbound
- **Dispatched:** 2026-06-05 by gateway-pm
- **Status:** dispatched

## Objective (one line)
Generate a typed `Db500Map` from `Config/db500-keys.json` and refactor the 5 Services to reference it instead of duplicated hard-coded address consts — same offsets, zero behavior change.

## Files in scope (paths only — Codex reads the repo)
- `WcsSystem/Wcs.PlcService/Config/db500-keys.json` (SOURCE — read, do not change offsets)
- `tools/Db500MapGen/` (new — small `dotnet run` console that reads the JSON, emits Db500Map.g.cs)
- `WcsSystem/Wcs.PlcService/Plc/Db500Map.g.cs` (new — generated; one `const string <Key> = "DB500..."` per field)
- `WcsSystem/Wcs.PlcService/Services/SystemService.cs`
- `WcsSystem/Wcs.PlcService/Services/CraneService.cs.cs`
- `WcsSystem/Wcs.PlcService/Services/ShuttleService.cs`
- `WcsSystem/Wcs.PlcService/Services/PlcBarcodeReader.cs`
- `WcsSystem/Wcs.PlcService/Services/LocationRouter.cs`

## Acceptance criteria
- [ ] `tools/Db500MapGen` reads `db500-keys.json` and emits `Db500Map.g.cs` with a `const string` per field key (value = the `address`); barcode handled as base address + length. A header comment marks it generated + the source JSON + "do not edit by hand".
- [ ] The 5 Services replace their private `const string ... = "DB500..."` with `Db500Map.<Key>`. Every address must map to the SAME string as today (verify against db500-keys.json — e.g. craneX=DBW54, barcodeOk=DBX46.0, plcDone=DBX48.0, heartbeat stays in Worker.cs).
- [ ] No offset changes; `/status` output and PLC writes are byte-identical to before.
- [ ] `dotnet build` (WcsSystem/) passes; regenerating Db500Map.g.cs from the JSON is idempotent.

## Do
- Match the existing Vietnamese comment style in the Services.
- Keep PLC1/PLC2 symmetric (where two consts share an address today, both point at the same Db500Map key).
- Keep the generator tiny and dependency-free (System.Text.Json).

## Don't
- Change any DB500 offset / command bit / `/status` field / `LocationModel` (contract.gateway-db-keys).
- Add blocking I/O, sleeps, or `.Result`/`.Wait()` to a 50ms `Update()`.
- Touch the `DB500.DBX74.0` heartbeat write in Worker.cs.
- Wire the generator into the build as a hard MSBuild step — keep it a manual `dotnet run` regen for now.

## Report (mandatory)
When done, write `tasks/inbound/REPORT-GW-002.md`: what changed / how verified / risks / follow-ups.
