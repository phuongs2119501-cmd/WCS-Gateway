# REPORT-GW-005: Fake-PLC simulator mirroring DB500

## What changed
- Added `IS7Backend` plus `RealS7Backend`; `S7Connector` now keeps the existing lock, reconnect throttle, connection state, and deduped error logging while delegating leaf reads/writes to the backend.
- Added `SimS7Backend`, an in-memory DB500 backend with `DB500.DBXn.b`, `DB500.DBWn`, and `DB500.DBBn` parsing, big-endian word storage, heartbeat write acceptance at `DB500.DBX74.0`, and the command echo lifecycle for `DBX0.0/0.1/0.2`.
- Added `Plc/Sim/SimFixture.cs` and `Config/sim-fixture.json` to seed crane/shuttle/system/barcode/gate state for PLC1 and PLC2.
- Added `PlcSettings.UseMock` and wired `Program.cs` to choose `RealS7Backend` or `SimS7Backend` independently per PLC. Defaults remain `UseMock=false` in `appsettings.json`.
- Did not change `WcsSystem/Wcs.PlcService/Config/db500-keys.json` or any DB500 offset.

## How verified
- Ran `dotnet build WcsSystem/WcsSystem.sln`: passed with 0 warnings and 0 errors.
- Started `Wcs.PlcService` with:
  - `Plc1Settings__UseMock=true`
  - `Plc2Settings__UseMock=true`
- Confirmed `/status` returned fixture state:
  - `plc1=true`
  - `barcode1=SIMPLC1000001`
  - `crane1 X=1,Z=1,Free=True,Busy=False`
- Posted `POST /api/location/send` with command type 1, `Xin/Zin/Bin=1/1/1`, `Xout/Zout/Bout=5/3/1`.
- Polled `/status` and confirmed:
  - crane entered `Busy=True`
  - crane reached `X=5,Z=3`
  - crane returned `Free=True,Busy=False`
  - `modeCraneShuttle=0`
- Worker log confirmed full echo: `SEND JOB -> PLC1`, `PLC1 DONE -> RESET CMD`, `JOB FINISHED`.
- Critical-thinking verdict: PASS. The named `critical-thinking` skill was not available in this Codex session's skill list, so this was performed manually against the task acceptance criteria.

## Risks
- Simulator movement is time-tick driven from backend leaf I/O calls, not a dedicated timer. This matches the 50 ms worker loop without blocking, but it only advances while the gateway is actively reading/writing the backend.
- Shuttle movement is intentionally not simulated in v1; the task marks shuttle motion optional.
- Existing `Program.cs` was already dirty before this task with `UseUrls("http://0.0.0.0:5050")` versus HEAD. I preserved the working-tree value and did not change the port.

## Follow-ups
- Add fault/error injection only if v2 fault drills are funded.
- Add simulator coverage tests if the repo gains a test project.
