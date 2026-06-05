# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WCS (Warehouse Control System) Gateway — controls an automatic warehouse by communicating with Siemens S7-1200 PLCs over Ethernet and exposing a REST API. Written in Vietnamese (comments, docs).

Two independent sub-projects:
- **WcsSystem/** — ASP.NET Core 8 Web API + BackgroundService (main system)
- **TcpSocket/** — Standalone TCP bridge server on port 9000 that proxies status from WcsSystem

## Build & Run

```bash
# Main service (WcsSystem)
cd WcsSystem
dotnet build
dotnet run --project Wcs.PlcService    # Listens on http://0.0.0.0:5000

# TCP bridge (TcpSocket)
cd TcpSocket
dotnet build
dotnet run --project TcpSocket          # Listens on 127.0.0.1:9000
```

Both target .NET 8. No test projects exist.

## Architecture

### WcsSystem/Wcs.PlcService

**Startup (Program.cs):** Registers all services as singletons, enables permissive CORS, binds to port 5000, starts the Worker background service.

**Update loop (Worker.cs):** Runs every 50ms in sequence: SystemService → CraneService → PlcBarcodeReader → LocationRouter → ShuttleService. Each service reads/writes PLC data blocks and updates the shared `SystemState` singleton.

**PLC layer (Plc/):**
- `S7Connector` — base class wrapping S7netplus. Thread-safe (lock), throttled reconnection (5s), deduplicated error logging.
- `Plc1Connector` / `Plc2Connector` — typed singletons for DI.

**Services (Services/):**
- `SystemState` — in-memory state container shared across all services and controllers
- `SystemService` — reads system flags (Auto/Running/Stop/Error) from DB500
- `CraneService` — reads crane position and status from DB500
- `ShuttleService` — reads two shuttles' positions, states, battery from DB500
- `PlcBarcodeReader` — reads 14-char barcode from DB500.DBB32-45, writes OK/NG
- `LocationRouter` — receives job orders, routes to PLC1/PLC2 based on location, selects optimal shuttle by distance
- `TcpClientService` — TCP client connecting to the TcpSocket bridge on port 9000

**Controllers:**
- `GET /status` — full system state JSON (polled by monitor UI)
- `POST /api/plc/write-bool`, `/api/plc/write-int` — direct PLC writes
- `POST /api/location/send` — submit job orders

**PLC memory map:** All data lives in DB500. Offsets 0–14 for commands/locations (written), 16–30 for crane/shuttle state (read), 32–45 for barcode (read), 46–48 for barcode/job signals, 50–68 for system state and positions.

### TcpSocket

Standalone console app on port 9000. Two commands: `PING` → `PONG`, `GET_STATUS` → fetches JSON from WcsSystem's `/status` endpoint.

### TestTools

`wcs_monitor.html` — standalone HTML monitor UI that polls `/status` every 1 second. No build step needed.

## Key Dependencies

- **S7netplus 0.20.0** — Siemens S7 PLC communication
- **Microsoft.Extensions.Hosting 8.0.0** — background service hosting

## Design Patterns

- All services and PLC connectors are **singletons** sharing `SystemState`
- PLC operations are **synchronous** with lock-based thread safety
- The Worker orchestrates a strict read-process-write cycle every 50ms
- `LocationRouter` uses distance-based algorithm to select the nearest free shuttle

---

## Cross-workspace coordination (wcs-mcp)

This workspace is plugged into the shared **wcs-mcp** server (`D:/Sources/WCS-MCP`) — a tiny stdio MCP server backed by SQLite that lets PM agents share rule docs and exchange durable handoff messages without manual copy-paste.

**Your identity on that server:** `gateway-pm`. The primary counterpart is `be-pm` (WCS-Application). The FE counterpart is `fe-pm` (indirect — you usually don't need to message FE directly). The human PO is `po`.

**At every session start, run this first** (before any code work):

```
whoami({})                                               ← confirm server live, scan known_parties for typos
read_messages({ for: "gateway-pm", unread_only: true })
get_doc({ name: "rules.wcs-network" })                   ← project roster; if DOC_NOT_FOUND see bootstrap note below
get_doc({ name: "rules.shared" })                        ← shared rules (use if_none_match to cache)
```

> **Bootstrap:** If `rules.wcs-network` returns `DOC_NOT_FOUND`, create it once:
> `put_doc({ name: "rules.wcs-network", content: "be-pm=D:/Sources/WCS-Application | fe-pm=D:/Sources/WCS-FE | gateway-pm=D:/Sources/WCS-Gateway | po=human | wcs-mcp-pm=D:/Sources/WCS-MCP", author: "gateway-pm", expected_version: 0 })`

For each message you act on: `ack_message({ id })` **immediately** (ack first, reply second — prevents double-processing if session restarts mid-turn).

**When to write to the server** (use `author: "gateway-pm"` / `from: "gateway-pm"` on every call):

- A PLC protocol change that affects what BE sends/receives → `put_doc({ name: "contract.plc.<area>", content: "<spec>", author: "gateway-pm" })` AND `post_message({ from: "gateway-pm", to: "be-pm", topic: "<area> changed", body: "...", refs: ["contract.plc.<area>"] })`.
- A frozen Gateway-side decision → `put_doc({ name: "decision.<id>", content: "..." })`.
- Gateway-specific durable knowledge → `put_doc({ name: "rules.gateway", ... })`.

**Identity is by convention, not auth.** Always pass `gateway-pm`; never claim to be `be-pm`, `fe-pm`, or `po`.

**Doc name conventions:** `rules.<scope>`, `contract.plc.<area>`, `contract.<area>`, `spec.<feature>`, `decision.<id>`.

#### Token-saving & safety tools (use these by default)

- **`whoami({})` once per session** — confirms live server, returns `known_parties`; flag typos, don't mirror them.
- **Re-reading a doc? Cache `content_hash`** then `get_doc({ name, if_none_match: "<hash>" })` — returns `{ not_modified: true }` if unchanged, near-zero tokens.
- **Just need version/size? Use `get_doc_meta({ name })`** — no content, cheap.
- **Writing a shared doc? Pass `expected_version`** — read current version first, then `put_doc({ ..., expected_version })` to avoid clobbering be-pm's edits. `expected_version: 0` = create-only.

Full workflow + tool reference: `D:/Sources/WCS-MCP/docs/usage.md`.

### Talking to be-pm directly (don't ask the PO to ferry)

When you need something from the BE side, **talk to `be-pm` directly**.

- **Heads-up / PLC contract change shipped** → `post_message({ from: "gateway-pm", to: "be-pm", topic: "...", body: "..." })`, then immediately invoke the `notify-peer` skill (`.claude/skills/notify-peer.md`) to wake be-pm headlessly so they read it in ~30s.
- **You need their answer before this turn can finish** (e.g. "what COMMAND shape does BE actually send?", "does BE expect ack before or after PLC executes?") → use the `consult-peer` skill (`.claude/skills/consult-peer.md`). Topic must start with `consult.`.

**One-level-deep wake rule.** If your *own* session was started by a `claude --print` wake from be-pm (you'll see "You were woken by a sender-side wake from be-pm" at the top of your prompt), do **NOT** invoke `notify-peer` or `consult-peer`. Read, reply if useful, ack, stop. This prevents A→B→A→B runaway loops.

If the question is open-ended or expects more than one round-trip, take it to the PO instead.
