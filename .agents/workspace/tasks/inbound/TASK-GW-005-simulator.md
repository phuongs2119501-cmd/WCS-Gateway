# TASK-GW-005: Fake-PLC simulator mirroring DB500

- **Spec:** specs/SPEC-GW-005-simulator.md
- **Phase:** inbound
- **Dispatched:** 2026-06-05 by gateway-pm
- **Status:** dispatched

## Objective (one line)
Add an in-memory fake-PLC behind `S7Connector` so the whole Gateway runs with no real hardware, swappable by config, replaying a real task lifecycle on the same DB500 keys.

## Files in scope (paths only — Codex reads the repo)
- `WcsSystem/Wcs.PlcService/Plc/S7Connector.cs` (introduce backend seam)
- `WcsSystem/Wcs.PlcService/Plc/IS7Backend.cs` (new)
- `WcsSystem/Wcs.PlcService/Plc/RealS7Backend.cs` (new — wraps today's S7.Net.Plc + connect logic)
- `WcsSystem/Wcs.PlcService/Plc/SimS7Backend.cs` (new — byte[] DB500, address grammar, echo loop)
- `WcsSystem/Wcs.PlcService/Plc/Sim/SimFixture.cs` + `Config/sim-fixture.json` (new — seed state)
- `WcsSystem/Wcs.PlcService/Models/PlcSettings.cs` (add `UseMock` bool)
- `WcsSystem/Wcs.PlcService/Program.cs` (DI: pick Real vs Sim backend per PlcSettings.UseMock)
- `WcsSystem/Wcs.PlcService/appsettings.json` (add `"UseMock": false` to Plc1Settings/Plc2Settings)
- Reference catalog: `WcsSystem/Wcs.PlcService/Config/db500-keys.json` (DB500 offsets — do not change them)

## Acceptance criteria
- [ ] `IS7Backend` exposes the leaf ops S7Connector needs: `Open/Close/IsOpen`, `Read(address)`, `Write(address,value)` (bool/short/byte). S7Connector keeps its lock + reconnect-throttle + dedup error log; only leaf I/O delegates to the backend.
- [ ] `RealS7Backend` = exact current behavior (S7.Net.Plc, same CpuType/IP/Rack/Slot). Real path unchanged.
- [ ] `SimS7Backend` parses `DB500.DBXa.b`, `DB500.DBWn`, `DB500.DBBn` against a `byte[]` (>=76B, big-endian words) and accepts the `DB500.DBX74.0` heartbeat write.
- [ ] Echo loop: on a rising command bit (DBX0.0/0.1/0.2) → set craneBusy=1/craneFree=0, latch target (xout=DBW10, zout=DBW12); over N ticks (config, default 40) step craneX(DBW54)/craneZ(DBW56) toward target; on arrival set plcDone(DBX48.0)=1, craneBusy=0, craneFree=1; when Gateway clears the command bit → clear plcDone → idle. Shuttle motion optional (crane-only is acceptable for v1).
- [ ] `UseMock=true` (per PLC) boots with NO PLC and `/status` returns the seeded fixture; POST /api/location/send drives the full echo to "JOB FINISHED".
- [ ] `dotnet build` (WcsSystem/) passes.

## Do
- Match the existing Vietnamese comment style.
- Reuse the exact address strings the Services already use (from db500-keys.json). Keep PLC1/PLC2 symmetric.
- Keep the sim tick driven off the existing 50ms Worker loop or a self-contained timer — no blocking.

## Don't
- Change any DB500 offset / command bit (they are a frozen contract — contract.gateway-db-keys).
- Add blocking I/O, sleeps, or `.Result`/`.Wait()` to a 50ms `Update()`.
- Touch the `DB500.DBX74.0` heartbeat semantics (sim must accept the write, that's all).
- Regress the real path: with `UseMock=false` behavior must be byte-identical to today.

## Report (mandatory)
When done, write `tasks/inbound/REPORT-GW-005.md`: what changed / how verified / risks / follow-ups.
