# Reconciliation: `feat/handle-connection-wcs` ↔ `main` (owner's real-PLC work)

**Status:** SCAN / REPORT ONLY — no code changed.
**Author:** gateway-pm · **Date:** 2026-06-21
**Fork point (merge-base):** `ad1d2fc (07042026)`

---

## 0. TL;DR

Both branches forked from `ad1d2fc` and independently rewrote the **same files**.
Nothing is lost (both are on `origin/`). The conflict is that the two sides use
**different DB500 memory maps**, so our simulator/codegen branch addresses the
**wrong bytes** relative to the live PLC.

- **`main` (`1720550`, owner via Gemini)** = the **hardware-verified truth**. Real PLC
  connection works. Its `Data Mapping PLC/DataPlc1.cs` / `DataPlc2.cs` is the
  authoritative DB500 layout. Already on `origin/main`.
- **Our branch (`b1227ac`)** = simulator backend + codegen + agent scaffolding, built
  on the **old** `ad1d2fc` map. Good tooling, wrong offsets.

**Resolution principle:** owner's map = single source of truth. Re-point our
`db500-keys.json` to his offsets, regenerate `Db500Map.g.cs`, rebuild sim + services
on top. (Not executed yet — this doc is the plan.)

---

## 1. Branch state

| | commit | where | content |
|---|---|---|---|
| main | `1720550` | `origin/main` (pushed) | real-PLC connection, ConnectingWcs API, DataPlc map |
| ours | `b1227ac` | `origin/feat/handle-connection-wcs` (pushed) | sim backend, codegen, agents |
| base | `ad1d2fc` | shared ancestor | old flat SystemState + old map |

Both maps share one rule (unchanged): **PLC1 and PLC2 expose the SAME DB500 layout**;
identity comes from which connector reads it. The owner's `DataPlc1.cs` and `DataPlc2.cs`
are byte-identical layouts (only `PLC1_DONE`/`PLC2_DONE` naming differs).

---

## 2. DB500 offset delta — sim map (ours) vs real PLC (owner) ⚠️

This table is the heart of the "wrong every point" problem.

| Logical field | Our `db500-keys.json` | Owner `DataPlc*.cs` (REAL) | Shift |
|---|---|---|---|
| reqImport/Export/Transfer | `DBX0.0/.1/.2` | `DBX0.0/.1/.2` | — same |
| reqAutoRun / reqStop | *(absent)* | `DBX0.3 / DBX0.4` | **NEW** |
| modeCraneShuttle | *(was DBW2 "mode")* | `DBW2` | same addr |
| Xin/Zin/Bin | `DBW4/6/8` | `DBW4/6/8` | — same |
| Xout/Zout/Bout | `DBW10/12/14` | `DBW10/12/14` | — same |
| Crane**1** state | `DBX16.x` / code `DBW18` | `DBX16.x` / `DBW18` | — same |
| Crane**2** state | *(absent)* | `DBX20.x` / `DBW22` | **NEW** |
| Shuttle1 state | `DBX20.x` / `DBW22` / batt `DBW24` | `DBX24.x` / `DBW26` / PIN `DBW28` | **+4..+6** |
| Shuttle2 state | `DBX26.x` / `DBW28` / `DBW30` | `DBX30.x` / `DBW32` / `DBW34` | **+4** |
| Barcode (14 char) | `DBB32` | `DBB36` | **+4** |
| Barcode OK / NG | `DBX46.0 / .1` | `DBX50.0 / .1` | **+4** |
| Done / Fail | `DBX48.0` (done only) | `DBX52.0 / DBX52.1` | **+4, +Fail** |
| Sys Auto/Run/Stop/Err | `DBX50.0–.3` / `DBW52` | `DBX54.0–.3` / `DBW56` | **+4** |
| Crane1 pos X/Z | `DBW54/56` | `DBW58/60` | **+4** |
| Crane2 pos X/Z | *(absent)* | `DBW62/64` | **NEW** |
| Shuttle1 pos X/Z/B | `DBW58/60/62` | `DBW66/68/70` | **+8** |
| Shuttle2 pos X/Z/B | `DBW64/66/68` | `DBW72/74/76` | **+8** |
| Gate | single `DBW70` | import `DBW78` / export `DBW80` | **split + shift** |
| WCS heartbeat | `DBX74.0` | `DBX82.0` | **+8** |
| DirectionBlock2 | *(absent)* | `DBW84` | **NEW** |

**Min DB length:** ours `76` bytes → real ≈ **`86`** bytes (last field `DBW84`).

Net: the real map adds a full **second crane per PLC**, **dual import/export gates**,
**reqAutoRun/reqStop command bits**, a **Done/Fail** handshake, and **DirectionBlock2**,
then shifts everything from the shuttle-state region onward by +4 to +8 bytes.

---

## 3. Files: deleted / added / rewritten on `main`

### Deleted by owner (we must NOT resurrect)
- `TcpSocket/` (entire project — `Program.cs`, `.csproj`)
- `Services/LocationRouter.cs` (369 lines)
- `Services/TcpClientService.cs`
- `Controllers/StatusController.cs`, `Controllers/LocationController.cs`
- `Models/LocationModel.cs`

> Implication: our branch's TCP-bridge edits and `LocationRouter` changes are **dead**.
> The job-routing role moves into the new `ConnectingWcs` API + per-PLC command bits.

### Added by owner (authoritative — preserve)
- `ConnectingWcs/ReceiveDataAPI.cs` — `POST /api/wms/send-command`
- `ConnectingWcs/ProcessingDataReceive.cs` — `WmsCommandModel` payload contract
- `ConnectingWcs/SendDataAPI.cs` — `GET /api/status` JSON + serves monitor HTML
- `ConnectingWcs/ConnectingWcs.md` — owner's API notes
- `Data Mapping PLC/DataPlc1.cs`, `DataPlc2.cs` — the real DB500 map + runtime state

### Rewritten by both (true conflicts on merge)
`Services/SystemState.cs`, `Worker.cs`, `Plc/S7Connector.cs`, `Program.cs`,
`Services/{CraneService,ShuttleService,SystemService,PlcBarcodeReader}.cs`,
`appsettings.json`.

---

## 4. New API contract (owner) — preserve verbatim

### Inbound: WMS → WCS
`POST /api/wms/send-command`  body = `WmsCommandModel`:
- `commandGroup`: `"MoveTask"` | `"SystemControl"` | `"BarcodeResult"`
- MoveTask: `commandType` (1=Import,2=Export,3=Transfer), `targetPlc` (1|2),
  source `xin/zin/bin`, dest `xout/zout/bout`
- SystemControl: `requestAutoRun`, `requestStop`, `resetError`
- BarcodeResult: `barcodeOk`, `barcodeNg`

> Currently `ReceiveCommand` only echoes success — **routing into PLC writes is a TODO**
> the owner left open ("móc dữ liệu này truyền vào Queue/Router"). This is our
> natural gateway-side work item.

### Outbound: monitor/WMS → WCS
`GET /api/status` returns per-PLC JSON (`plc1`, `plc2`, barcodes, gates, crane1/2,
shuttle1/2, system1/2, done/fail, directionBlock2).
`GET /` and `GET /status` serve the HTML monitor (hard-coded path — see §6).

---

## 5. What our branch keeps (re-base, don't discard)

- `Plc/IS7Backend.cs`, `RealS7Backend.cs`, `SimS7Backend.cs` — backend abstraction
- `Plc/Sim/SimFixture.cs`, `Config/sim-fixture.json` — simulator
- `Config/db500-keys.json` + `tools/Db500MapGen` → `Plc/Db500Map.g.cs` — codegen
- `.agents/`, `.claude/skills/`, `.mcp.json`, agent scaffolding

These survive **only after** `db500-keys.json` is re-pointed to the §2 real offsets and
`Db500Map.g.cs` is regenerated.

---

## 6. Risks / cleanup flagged for the owner

1. **Hard-coded HTML path** in `SendDataAPI.cs`:
   `C:\Users\Admin\Desktop\TIN QUANG\...\wcs_monitor.html` — breaks on any other machine.
   Should be `UseStaticFiles` / config path.
2. **`appsettings.json`** still has `"WcsApiUrl": ...:9000/api/status"` — leftover from
   the deleted TcpSocket; dead config.
3. `/status` is served as HTML on `main` but our monitor/tools expect `/status` = JSON
   (now `/api/status`). Any consumer polling `/status` for JSON will break — confirm with
   `fe-pm`.
4. `S7Connector.Read(DataType,...)` raw overload added on main; our backend abstraction
   wraps reads differently — reconcile the interface.

---

## 7. Proposed merge order (when approved)

1. Branch `reconcile/real-plc-map` off our `feat/handle-connection-wcs`.
2. Bring in owner's `ConnectingWcs/` + `Data Mapping PLC/` **as-is** (authoritative).
3. Rewrite `Config/db500-keys.json` offsets → §2 "REAL" column; bump `minLengthBytes` ≈ 86.
4. Regenerate `Plc/Db500Map.g.cs` via `tools/Db500MapGen`.
5. Decide `SystemState` shape: adopt owner's `DataPlc1/DataPlc2` (recommended) and make
   our services + sim read through it.
6. Point `SimS7Backend` / `SimFixture` at the new offsets so sim mirrors real PLC.
7. Delete dead: TcpSocket refs, `WcsApiUrl`, LocationRouter/TcpClientService remnants.
8. Build, run sim, diff `GET /api/status` JSON shape against owner's `main`.
9. Confirm `/status` vs `/api/status` JSON contract with `fe-pm` before opening PR.

---

*This is a plan, not a change. Awaiting go-ahead on a strategy (rebase-map vs full-merge)
before any code is touched.*
