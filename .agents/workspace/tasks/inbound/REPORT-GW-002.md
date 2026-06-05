# REPORT-GW-002

- **Task:** tasks/inbound/TASK-GW-002-db-keys-generator.md
- **Status:** complete

## What changed
- Added `tools/Db500MapGen`, a small dependency-free `net8.0` console generator using `System.Text.Json`.
- Generated `WcsSystem/Wcs.PlcService/Plc/Db500Map.g.cs` from `WcsSystem/Wcs.PlcService/Config/db500-keys.json`.
- `Db500Map.g.cs` emits one `public const string <key>` per catalog field, plus barcode base metadata:
  `barcodeLength`, `barcodeDbNumber`, and `barcodeByteStart`.
- Refactored the five in-scope Services to reference `Db500Map.*` instead of service-local hard-coded DB500 address strings:
  `SystemService.cs`, `CraneService.cs.cs`, `ShuttleService.cs`, `PlcBarcodeReader.cs`, `LocationRouter.cs`.
- Kept PLC1/PLC2 symmetric aliases pointing to the same generated keys where the current address is shared.
- Did not touch `Worker.cs`; heartbeat remains the direct `DB500.DBX74.0` write as required.

## How verified
- `dotnet build WcsSystem/WcsSystem.sln` passed with 0 warnings and 0 errors.
- `dotnet build tools/Db500MapGen/Db500MapGen.csproj` passed with 0 warnings and 0 errors.
- Regenerated with `dotnet run --project tools/Db500MapGen/Db500MapGen.csproj`; output hash stayed unchanged:
  `56799E3DB39F2293075D974608132A5B78ABCBDE84D83D9A0DBD98E46237E9F8`.
- PowerShell verification compared every `fields[].key/address` entry in `db500-keys.json` to `Db500Map.g.cs`;
  all generated constants matched. Spot checks passed for `craneX=DB500.DBW54`, `barcodeOk=DB500.DBX46.0`,
  and `plcDone=DB500.DBX48.0`.
- Searched the five Services for executable DB500 string literals; none remain outside comments.
- `/status` and `LocationModel` code were not changed. No live before/after `/status` baseline was available in this turn;
  byte identity is supported by static verification that the same compile-time address strings feed the unchanged code paths.
- Spec requested a `critical-thinking` skill verdict. That skill was not available in this Codex session, and no deferred
  critical-thinking tool was discoverable. Manual verdict: pass, because the change is pure compile-time constant
  externalization, the generated constants match the catalog, and build/idempotence checks pass.

## Risks
- No known DB500 address string changed form.
- The generator is manual by design; future JSON edits require rerunning `dotnet run --project tools/Db500MapGen/Db500MapGen.csproj`.
- The generated constants intentionally preserve the lower-camel JSON field keys as C# constant names.

## Follow-ups
- Consider adding a lightweight CI check that reruns `Db500MapGen` and fails on a generated-file diff.
- Later, if approved, add typed accessor helpers from the catalog metadata; this task kept the implementation to constants only.
