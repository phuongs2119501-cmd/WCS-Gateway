# REPORT-GW-006

- **Task:** tasks/realmap/TASK-GW-006.md
- **Status:** completed

## What changed
- Updated `WcsSystem/Wcs.PlcService/Plc/SimS7Backend.cs` so `StartJob` still latches the job and sets crane-1 Busy, but does not mark either shuttle Busy at the command-bit rising edge.
- Added lazy shuttle resolution in `AdvanceJob`: `Db500Map.modeCraneShuttle == 2` selects shuttle-2; every other value, including `0`, selects shuttle-1.
- Only the selected shuttle is marked Busy/Free and has X/Z/B stepped and snapped to the target. The non-selected shuttle keeps its seeded Busy/Free state and position. Crane-2 is not moved or toggled.
- Kept command, crane motion, heartbeat, and `plcDone` handshake behavior in the existing simulator flow.

## How verified
- `dotnet build` from `WcsSystem/`: passed with `0 Warning(s)` and `0 Error(s)`.
- Ran a temporary direct `SimS7Backend` verifier, then removed it. Result: `PASS: direct SimS7Backend TC1-TC6 checks passed`.
- TC1: confirmed mock seed values at `Db500Map` addresses and ran the app with `Plc1Settings__UseMock=true` / `Plc2Settings__UseMock=true`; `/status` and `/api/status` both returned populated mock state from the same `SystemState` (`plc1=true`, `crane1.free=true`, `system1.auto=true`, barcode `SIMPLC1000001`, shuttle1 X `1`).
- TC2: command sets crane Busy, crane X steps toward target, completion sets `plcDone`, crane returns Free, and command reset clears `plcDone`.
- TC3: with mode `1`, shuttle-1 goes Busy and moves while shuttle-2 stays Free/stationary; with mode `2`, shuttle-2 goes Busy and moves while shuttle-1 stays Free/stationary; with mode `0`, lazy default selects shuttle-1. Crane-2 Busy stayed false.
- TC4: writing `sysStop` and `sysError` through `Db500Map` addresses reads back correctly.
- TC5: writing `wcsHeartbeat` is accepted and only latches the bit.
- TC6: writing `barcodeOk`/`barcodeNg` reads back correctly, and barcode bytes are read from the configured DBB span starting at `Db500Map.barcodeByteStart`.
- Offset check: simulator state reads/writes use `Db500Map.*`; no literal byte-offset setters/getters remain in `SimS7Backend`. Remaining numeric literals are DB number/DB length/timing/regex/comment values, not DB500 field offsets.
- The `critical-thinking` skill required by the spec was not available in this Codex session's skill list, so I could not run it.

## Risks
- No permanent automated test was added; behavior was verified with a temporary direct harness and a mock-mode HTTP smoke check.
- The lazy selector intentionally defaults stale/zero DBW2 to shuttle-1 per spec. If the router writes DBW2 late enough after the first pump tick, the job will remain on shuttle-1 for that command.

## Follow-ups
- Add a permanent unit/integration test around `SimS7Backend` selected-shuttle motion if the project gets a test project.
- Revisit crane2/shuttle2 PLC binding only after the deferred gateway-dev decision is made.
