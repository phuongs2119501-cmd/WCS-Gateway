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

You are `gateway-pm` (PLC / hardware gateway) on the shared **wcs-mcp** team (stdio MCP server at `D:/Sources/WCS-MCP`), alongside `be-pm` (`D:/Sources/WCS-Application`), `fe-pm` (`D:/Sources/WCS-FE`), `wcs-design` (`D:/Sources/WHS`, the 3D layout designer; owns `contract.warehouse-layout`), and the human `po`. Message **any** member directly — including `fe-pm` when a hardware fact affects the UI, or `wcs-design` if a layout assumption conflicts with hardware reality — no fixed pairs, no asking the PO to ferry. Identity is convention, not auth: always pass `gateway-pm`; never claim to be another member.

**Session start:** `read_messages({ for: "gateway-pm", unread_only: true })`, then `whoami({})`; `ack_message({ id })` **immediately** on anything you act on (ack first, reply second — survives a mid-turn restart).

**Reach a teammate** (full mechanics live in the skill bodies, not here):
- `/post` — guided send (recipient + kind menus).
- `notify-member` skill — FYI + wake one teammate.
- `consult-member` skill — ask one teammate and block for their reply.
- `convene-team` skill — broadcast to all + collect everyone's take.

**Share durable output** another member depends on (a `contract.plc.*`/decision change) via `put_doc` + `post_message`. Doc-naming, versioning, and token-saving (`content_hash` caching) conventions: `get_doc({ name: "rules.shared" })` and `D:/Sources/WCS-MCP/docs/usage.md`.

**One-level-deep wake rule.** If your own session was started by a wake/convene (noted at the top of your prompt), do **NOT** invoke notify-member/consult-member/convene-team after replying — read, reply, ack, stop. This prevents A→B→A→B runaway loops; honor it. Open-ended multi-round debates go to the PO instead.
