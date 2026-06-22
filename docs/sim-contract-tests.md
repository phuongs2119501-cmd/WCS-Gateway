# Gateway sim contract tests (TC1–TC6) — team runbook

**Purpose:** prove the gateway reports the warehouse truthfully on the **real DB500 map** with no
hardware. Anyone on the gateway side can reproduce these in ~2 minutes.
**Related:** SPEC-GW-006, `Config/db500-keys.json` (v3), docs/reconciliation.md.
**Last verified:** 2026-06-21, all 6 pass, build 0/0.

---

## The one rule these tests guard
> Every field in the `/status` response maps to **exactly one** `Db500Map` key, and the simulator
> writes/moves that **same** offset. If sim-byte ↔ map-key ↔ response-field ever disagree, the
> response is wrong. `SimS7Backend` has **no literal offsets** — it derives everything from
> `Db500Map`, so it cannot drift from the real PLC map.

The 4-link chain each test exercises:
```
sim DB500 byte  →  S7Connector.TryRead(Db500Map.<key>)  →  Service fills SystemState  →  GET /status JSON
```

---

## Prerequisites
- .NET 8 SDK. No PLC, no Python needed (use `curl` + `grep`).
- The gateway listens on **http://localhost:5050**.

## Launch in simulator mode
Sim mode = `UseMock=true` for both PLCs. Override via env (don't edit appsettings):

```bash
cd WcsSystem
Plc1Settings__UseMock=true Plc2Settings__UseMock=true dotnet run --project Wcs.PlcService
```

Startup log should show `[PLC1] UseMock=true -> SimS7Backend DB500` and `Now listening on http://0.0.0.0:5050`.
Seeded values come from `Config/sim-fixture.json`. Job speed = `TicksPerMove` (40 ticks × 50 ms ≈ 2 s/move).

---

## Test cases

### TC1 — Seed / idle
```bash
curl -s http://localhost:5050/status        # our nested contract
curl -s http://localhost:5050/api/status    # owner-compatible flat contract
```
**Expect:** both reflect the same state; `crane1.free=true`, `system1.auto=true`, `barcode1="SIMPLC1000001"`,
`gate1=1`, `shuttle2.z=1` (NOT 257 — see "regression note"). Barcode is read at the real offset `DBB36`.

### TC2 — Crane moves  &  TC3 — Shuttle moves
Fire a job to PLC1 (`bin=1` routes to PLC1), then poll for ~2.5 s:
```bash
curl -s -X POST http://localhost:5050/api/location/send \
  -H "Content-Type: application/json" \
  -d '{"commandType":1,"xin":10,"zin":5,"bin":1,"xout":1,"zout":1,"bout":1}'

for i in $(seq 1 8); do
  sleep 0.3
  resp=$(curl -s http://localhost:5050/status)
  echo "t=$i $(echo "$resp" | grep -oE '"crane1":\{[^}]*\}') | $(echo "$resp" | grep -oE '"shuttle1":\{[^}]*\}')"
done
```
**Expect:** early ticks show `crane1` and `shuttle1` mid-move with `busy=true`; on arrival both reach the
target (`x=1,z=1`, shuttle `b=1`) with `free=true` and the done-handshake clears. Re-run with a different
`xout/zout/bout` to move again.

### TC4 — System status change
```bash
curl -s -X POST "http://localhost:5050/api/plc/write-bool?address=DB500.DBX54.2&value=true"
curl -s http://localhost:5050/status | grep -oE '"system1":\{[^}]*\}'
```
**Expect:** `system1.stop=true` (writing the real system-state offset `DBX54.2`).

### TC5 — Heartbeat
The Worker writes `Db500Map.wcsHeartbeat` (`DBX82.0`) to both PLCs every 50 ms loop.
```bash
# after running a while:
grep -iE "error|exception|out of range" <your run log>
```
**Expect:** no write/read errors; heartbeat accepted by the sim. (Caught a real bug once — see below.)

### TC6 — Barcode result write
```bash
curl -s -X POST "http://localhost:5050/api/plc/write-bool?address=DB500.DBX50.1&value=true"
curl -s http://localhost:5050/status | grep -oE '"barcodeNg1":(true|false)'
```
**Expect:** `barcodeNg1=true` (real barcode OK/NG offset `DBX50.0/.1`).

---

## Notes for whoever runs this
- **`write-bool` / `write-int`** take **query-string** params (`?address=...&value=...`), not a JSON body,
  and target **PLC1 only**.
- **No Python** on the dev box — extract JSON fields with `grep -oE '"field":\{[^}]*\}'`.
- `/status` = our nested shape (`crane1:{x,z,...}`). `/api/status` = owner-compatible flat shape
  (`gateImport1`, `done1`, `fail1`, `directionBlock2_Plc1`, …). Fields our services don't wire yet
  (`gateExport`, `done/fail`, `directionBlock2`) are emitted as defaults — placeholder, not live.
- **Deferred:** crane2 / shuttle2 ↔ PLC-connector binding is not finalized (awaiting gateway-dev
  confirmation). The sim seeds crane2/shuttle2 but does not move crane2.

## Regression note (why TC1 checks shuttle2.z)
The heartbeat was once hardcoded to the **old** offset `DBX74.0`; after the v3 map shift that byte is
inside `shuttle2Z` (`DBW74`), so the heartbeat silently corrupted it (read 257 instead of 1). Fixed by
routing the heartbeat through `Db500Map.wcsHeartbeat`. `shuttle2.z=1` in TC1 is the guard against this
class of literal-offset bug.
