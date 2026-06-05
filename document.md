# WCS Gateway - System Documentation

Complete technical documentation for the WCS (Warehouse Control System) Gateway.

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [API Endpoints](#2-api-endpoints)
3. [Status Response JSON Structure](#3-status-response-json-structure)
4. [Data Models & Response Fields](#4-data-models--response-fields)
5. [PLC Memory Map (DB500)](#5-plc-memory-map-db500)
6. [Connection Methods](#6-connection-methods)
7. [Job Order Processing](#7-job-order-processing---how-to-raise-a-task)
8. [Shuttle Selection Algorithm](#8-shuttle-selection-algorithm)
9. [Barcode Reader Logic](#9-barcode-reader-logic)
10. [Worker Execution Loop](#10-worker-execution-loop)
11. [TCP Socket Bridge Protocol](#11-tcp-socket-bridge-protocol)
12. [Monitor Dashboard (TestTools)](#12-monitor-dashboard-testtools)
13. [Configuration & Constants](#13-configuration--constants)

---

## 1. System Overview

```
┌──────────────────────┐       ┌──────────────────────────────────────┐
│   External System    │       │         WcsSystem (port 5000)        │
│   (WCS Application)  │──────>│  ASP.NET Core 8 Web API             │
│                      │ HTTP  │  + BackgroundService (Worker 50ms)   │
└──────────────────────┘       │                                      │
                               │  Services:                           │
┌──────────────────────┐       │   SystemService, CraneService,       │
│  Monitor Dashboard   │──────>│   ShuttleService, PlcBarcodeReader,  │
│  (wcs_monitor.html)  │ HTTP  │   LocationRouter, TcpClientService   │
└──────────────────────┘       │                                      │
                               │  PLC Connectors:                     │
┌──────────────────────┐       │   Plc1Connector → 192.168.39.10      │
│  TcpSocket Bridge    │<──────│   Plc2Connector → 192.168.39.13      │
│  (port 9000)         │ HTTP  │                                      │
└──────────────────────┘       └─────────┬──────────┬─────────────────┘
                                         │ S7.Net   │ S7.Net
                                   ┌─────▼───┐ ┌───▼─────┐
                                   │  PLC 1  │ │  PLC 2  │
                                   │ S7-1200 │ │ S7-1200 │
                                   └─────────┘ └─────────┘
```

**Two sub-projects:**
- **WcsSystem/** — Main ASP.NET Core 8 Web API + BackgroundService on port 5000
- **TcpSocket/** — Standalone TCP bridge server on port 9000

---

## 2. API Endpoints

Base URL: `http://<host>:5000`

### 2.1 GET /status — Full System State

Returns the complete system state (used by the monitor UI and TcpSocket bridge).

```http
GET /status
```

**Response:** See [Section 3](#3-status-response-json-structure) for full JSON structure.

---

### 2.2 GET /api/status — System State (HomeController)

Same data as `/status`, used by the monitor dashboard.

```http
GET /api/status
```

---

### 2.3 POST /api/location/send — Submit a Job Order

**This is the main endpoint for the WCS Backend to raise a task.**

```http
POST /api/location/send
Content-Type: application/json

{
  "commandType": 1,
  "xin": 5,
  "zin": 10,
  "bin": 1,
  "xout": 20,
  "zout": 15,
  "bout": 2
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `commandType` | int | Yes | `1` = Import Pallet, `2` = Export Pallet, `3` = Transfer Pallet |
| `xin` | int | Yes | Source X coordinate (column 1-26) |
| `zin` | int | Yes | Source Z coordinate (level/row) |
| `bin` | int | Yes | Source B zone (`1` = PLC1 zone, `2` = shared zone, `3` = PLC2 zone) |
| `xout` | int | Yes | Destination X coordinate |
| `zout` | int | Yes | Destination Z coordinate |
| `bout` | int | Yes | Destination B zone |

**Response:** `200 OK`

**How the backend (WCS Application) connects:**
```
POST http://192.168.x.x:5000/api/location/send
```
Send a JSON body with the fields above. The Gateway will:
1. Route the job to the correct PLC (based on `bin` value)
2. Auto-select the nearest shuttle
3. Write coordinates and command to the PLC
4. Wait for PLC completion signal
5. Reset and become ready for the next job

---

### 2.4 POST /api/plc/write-bool — Direct PLC Bool Write

```http
POST /api/plc/write-bool?address=DB500.DBX0.0&value=true
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `address` | string | S7 address (e.g. `DB500.DBX0.0`) |
| `value` | bool | `true` or `false` |

**Response:**
```json
{
  "success": true,
  "address": "DB500.DBX0.0",
  "value": true
}
```

---

### 2.5 POST /api/plc/write-int — Direct PLC Int Write

```http
POST /api/plc/write-int?address=DB500.DBW2&value=1
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `address` | string | S7 address (e.g. `DB500.DBW2`) |
| `value` | int16 | Integer value |

**Response:**
```json
{
  "success": true,
  "address": "DB500.DBW2",
  "value": 1
}
```

---

## 3. Status Response JSON Structure

Full response from `GET /status`:

```json
{
  "plc1": true,
  "plc2": true,

  "barcode1": "ABC1234567890",
  "barcodeOk1": true,
  "barcodeNg1": false,
  "barcode2": "XYZ9876543210",
  "barcodeOk2": false,
  "barcodeNg2": false,

  "crane1": {
    "x": 5,
    "z": 10,
    "busy": false,
    "free": true,
    "error": false,
    "errorCode": 0
  },
  "crane2": {
    "x": 20,
    "z": 15,
    "busy": true,
    "free": false,
    "error": false,
    "errorCode": 0
  },

  "shuttle1": {
    "x": 8,
    "z": 5,
    "b": 1,
    "busy": false,
    "free": true,
    "error": false,
    "errorCode": 0,
    "pin": 85
  },
  "shuttle2": {
    "x": 18,
    "z": 8,
    "b": 2,
    "busy": true,
    "free": false,
    "error": false,
    "errorCode": 0,
    "pin": 72
  },

  "system1": {
    "auto": true,
    "running": true,
    "stop": false,
    "error": false,
    "errorCode": 0
  },
  "system2": {
    "auto": true,
    "running": true,
    "stop": false,
    "error": false,
    "errorCode": 0
  },

  "lastLocation": {
    "commandType": 1,
    "xin": 5,
    "zin": 10,
    "bin": 1,
    "xout": 20,
    "zout": 15,
    "bout": 2
  },

  "modeCraneShuttle": 1
}
```

---

## 4. Data Models & Response Fields

### CraneModel

| Field | Type | Description |
|-------|------|-------------|
| `x` | int | X position (column index) |
| `z` | int | Z position (level/row) |
| `busy` | bool | Crane is executing a command |
| `free` | bool | Crane is idle and available |
| `error` | bool | Crane has an error |
| `errorCode` | int | Numeric error code (0 = no error) |

### ShuttleModel

| Field | Type | Description |
|-------|------|-------------|
| `x` | int | X position (column 1-26) |
| `z` | int | Z position (level/row) |
| `b` | int | B zone/aisle (1, 2, or 3) |
| `busy` | bool | Shuttle is executing a command |
| `free` | bool | Shuttle is idle and available |
| `error` | bool | Shuttle has an error |
| `errorCode` | int | Numeric error code (0 = no error) |
| `pin` | int | Battery percentage (0-100) |

### PlcSystemStateModel

| Field | Type | Description |
|-------|------|-------------|
| `auto` | bool | `true` = Auto mode, `false` = Manual mode |
| `running` | bool | System is running |
| `stop` | bool | System stop signal is active |
| `error` | bool | System has an error |
| `errorCode` | int | System error code |

### LocationModel (lastLocation)

| Field | Type | Description |
|-------|------|-------------|
| `commandType` | int | 1=Import, 2=Export, 3=Transfer |
| `xin` | int? | Source X |
| `zin` | int? | Source Z |
| `bin` | int? | Source B zone |
| `xout` | int? | Destination X |
| `zout` | int? | Destination Z |
| `bout` | int? | Destination B zone |

---

## 5. PLC Memory Map (DB500)

All data resides in **Data Block 500** on both PLCs.

### Command Bits (Written by Gateway)

| Offset | Address | Type | Description |
|--------|---------|------|-------------|
| 0.0 | `DB500.DBX0.0` | Bool | Req_ImportPallet |
| 0.1 | `DB500.DBX0.1` | Bool | Req_ExportPallet |
| 0.2 | `DB500.DBX0.2` | Bool | Req_TransferPallet |

### Mode Selection (Written by Gateway)

| Offset | Address | Type | Description |
|--------|---------|------|-------------|
| 2 | `DB500.DBW2` | Int16 | Mode_CraneShuttle: 0=none, 1=Shuttle1, 2=Shuttle2 |

### Location Coordinates (Written by Gateway)

| Offset | Address | Type | Description |
|--------|---------|------|-------------|
| 4 | `DB500.DBW4` | Int16 | Position_In X |
| 6 | `DB500.DBW6` | Int16 | Position_In Z |
| 8 | `DB500.DBW8` | Int16 | Position_In B |
| 10 | `DB500.DBW10` | Int16 | Position_Out X |
| 12 | `DB500.DBW12` | Int16 | Position_Out Z |
| 14 | `DB500.DBW14` | Int16 | Position_Out B |

### Crane State (Read from PLC)

| Offset | Address | Type | Description |
|--------|---------|------|-------------|
| 16.0 | `DB500.DBX16.0` | Bool | Crane_Free |
| 16.1 | `DB500.DBX16.1` | Bool | Crane_Busy |
| 16.2 | `DB500.DBX16.2` | Bool | Crane_Error |
| 18 | `DB500.DBW18` | Int16 | Crane_ErrorCode |
| 54 | `DB500.DBW54` | Int16 | Crane_X position |
| 56 | `DB500.DBW56` | Int16 | Crane_Z position |

### Shuttle 1 State (Read from PLC)

| Offset | Address | Type | Description |
|--------|---------|------|-------------|
| 20.0 | `DB500.DBX20.0` | Bool | Shuttle1_Free |
| 20.1 | `DB500.DBX20.1` | Bool | Shuttle1_Busy |
| 20.2 | `DB500.DBX20.2` | Bool | Shuttle1_Error |
| 22 | `DB500.DBW22` | Int16 | Shuttle1_ErrorCode |
| 24 | `DB500.DBW24` | Int16 | Shuttle1_Battery (Pin) |
| 58 | `DB500.DBW58` | Int16 | Shuttle1_X |
| 60 | `DB500.DBW60` | Int16 | Shuttle1_Z |
| 62 | `DB500.DBW62` | Int16 | Shuttle1_B |

### Shuttle 2 State (Read from PLC)

| Offset | Address | Type | Description |
|--------|---------|------|-------------|
| 26.0 | `DB500.DBX26.0` | Bool | Shuttle2_Free |
| 26.1 | `DB500.DBX26.1` | Bool | Shuttle2_Busy |
| 26.2 | `DB500.DBX26.2` | Bool | Shuttle2_Error |
| 28 | `DB500.DBW28` | Int16 | Shuttle2_ErrorCode |
| 30 | `DB500.DBW30` | Int16 | Shuttle2_Battery (Pin) |
| 64 | `DB500.DBW64` | Int16 | Shuttle2_X |
| 66 | `DB500.DBW66` | Int16 | Shuttle2_Z |
| 68 | `DB500.DBW68` | Int16 | Shuttle2_B |

### Barcode (Read from PLC)

| Offset | Address | Type | Description |
|--------|---------|------|-------------|
| 32-45 | `DB500.DBB32` - `DB500.DBB45` | Byte[14] | 14-character barcode string |
| 46.0 | `DB500.DBX46.0` | Bool | Barcode_OK |
| 46.1 | `DB500.DBX46.1` | Bool | Barcode_NG |

### Job Completion (Read from PLC)

| Offset | Address | Type | Description |
|--------|---------|------|-------------|
| 48.0 | `DB500.DBX48.0` | Bool | PLC_Done (job completed) |

### System State (Read from PLC)

| Offset | Address | Type | Description |
|--------|---------|------|-------------|
| 50.0 | `DB500.DBX50.0` | Bool | Auto_Mode |
| 50.1 | `DB500.DBX50.1` | Bool | System_Running |
| 50.2 | `DB500.DBX50.2` | Bool | System_Stop |
| 50.3 | `DB500.DBX50.3` | Bool | System_Error |
| 52 | `DB500.DBW52` | Int16 | System_ErrorCode |

---

## 6. Connection Methods

### 6.1 S7 PLC Connection (Siemens S7.Net)

| Parameter | PLC 1 | PLC 2 |
|-----------|-------|-------|
| IP Address | `192.168.39.10` | `192.168.39.13` |
| Port | 102 (S7 default) | 102 (S7 default) |
| Rack | 0 | 0 |
| Slot | 1 | 1 |
| CPU Type | S7-1200 | S7-1200 |

**Features:**
- Thread-safe with `lock` on all read/write operations
- Auto-reconnect with 5-second throttle between attempts
- Deduplicated error logging (logs only on state change)

**Methods available on S7Connector:**

| Method | Description |
|--------|-------------|
| `Connect()` | Initiate connection to PLC |
| `IsConnected` | Check if PLC is online |
| `TryRead<T>(address, out value)` | Read any data type from PLC address |
| `TryReadChar(address, out value)` | Read single byte as char |
| `TryWriteBool(address, value)` | Write boolean to PLC |
| `TryWriteInt16(address, value)` | Write Int16 to PLC |

### 6.2 HTTP REST API

| Setting | Value |
|---------|-------|
| Listen URL | `http://0.0.0.0:5000` |
| CORS | All origins allowed (including `file://`) |
| Content-Type | `application/json` (camelCase) |

### 6.3 TCP Socket Bridge

| Setting | Value |
|---------|-------|
| Listen Address | `0.0.0.0:9000` |
| Protocol | Text-based (UTF-8) |
| Commands | `PING`, `GET_STATUS` |

---

## 7. Job Order Processing — How to Raise a Task

### From WCS Backend Application

To submit a job to the warehouse, send an HTTP POST:

```bash
curl -X POST http://<gateway-ip>:5000/api/location/send \
  -H "Content-Type: application/json" \
  -d '{
    "commandType": 1,
    "xin": 5,
    "zin": 3,
    "bin": 1,
    "xout": 10,
    "zout": 2,
    "bout": 1
  }'
```

### C# Example (from WCS Application)

```csharp
using var client = new HttpClient();
var payload = new {
    commandType = 1,   // 1=Import, 2=Export, 3=Transfer
    xin = 5,
    zin = 3,
    bin = 1,
    xout = 10,
    zout = 2,
    bout = 1
};
var response = await client.PostAsJsonAsync(
    "http://192.168.x.x:5000/api/location/send", payload);
```

### Internal Processing Flow

```
POST /api/location/send
        │
        ▼
  LocationRouter receives job
        │
        ▼
  Route by Bin value:
    Bin=1 ──────────────► PLC1
    Bin=3 ──────────────► PLC2
    Bin=2 + Xin(1-13) ──► PLC1
    Bin=2 + Xin(14-26) ─► PLC2
        │
        ▼
  Select nearest shuttle (CalculateModeCraneShuttle)
        │
        ▼
  Write to PLC:
    1. Location coordinates (DB500.DBW4-14)
    2. Mode_CraneShuttle   (DB500.DBW2)
    3. Command bit         (DB500.DBX0.0/0.1/0.2)
        │
        ▼
  Wait for PLC_Done (DB500.DBX48.0 == true)
        │
        ▼
  Reset command bit → Ready for next job
```

### Command Types

| Value | Name | PLC Bit | Description |
|-------|------|---------|-------------|
| 1 | Import | `DB500.DBX0.0` | Bring pallet INTO the warehouse |
| 2 | Export | `DB500.DBX0.1` | Take pallet OUT of the warehouse |
| 3 | Transfer | `DB500.DBX0.2` | Move pallet between locations |

### Polling for Job Status

After sending a job, poll `GET /status` and check:

```javascript
// Job is active when lastLocation is populated
const status = await fetch("http://<gateway>:5000/status").then(r => r.json());

// Check if equipment is busy
if (status.crane1.busy || status.shuttle1.busy) {
    console.log("Job in progress...");
}

// Check for errors
if (status.crane1.error) {
    console.log("Crane error:", status.crane1.errorCode);
}

// Check system readiness before sending
if (status.system1.auto && status.system1.running && !status.system1.error) {
    console.log("System ready for jobs");
}
```

---

## 8. Shuttle Selection Algorithm

When a job is routed to a PLC, the system automatically selects the best shuttle.

### Logic (`LocationRouter.CalculateModeCraneShuttle`)

```
1. Filter valid shuttles for the target PLC:
   ┌─────────┬──────────────┬─────────────┐
   │ Target  │ Valid B zone │ Valid X range│
   ├─────────┼──────────────┼─────────────┤
   │ PLC1    │ B ∈ {1, 2}   │ X ∈ [1, 13] │
   │ PLC2    │ B ∈ {2, 3}   │ X ∈ [14,26] │
   └─────────┴──────────────┴─────────────┘

2. Calculate distance for each valid shuttle:
   distX = |shuttle.X - crane.X|
   distZ = |shuttle.Z - crane.Z|

3. Select shuttle with:
   - Smallest distX (primary)
   - Smallest distZ (tiebreaker)

4. If no valid shuttle found → fallback to shuttle matching targetPlc number
```

### Example

```
Job sent to PLC1, Crane1 at (X=5, Z=10)

Shuttle1: B=1, X=6,  Z=12 → distX=1, distZ=2  ✓ valid
Shuttle2: B=2, X=4,  Z=9  → distX=1, distZ=1  ✓ valid

Both distX=1, but Shuttle2 has distZ=1 < 2
→ Selected: Shuttle2 (Mode=2)
```

---

## 9. Barcode Reader Logic

### Reading (every 50ms cycle)

1. Read 14 bytes from `DB500.DBB32` through `DB500.DBB45`
2. Convert each byte to char, build string
3. Trim trailing spaces
4. Read validation flags: `DB500.DBX46.0` (OK) and `DB500.DBX46.1` (NG)
5. Log only when barcode value changes

### Writing Validation

| Method | Writes To | Purpose |
|--------|-----------|---------|
| `WriteBarcodeOk(true)` | `DB500.DBX46.0` on both PLCs | Mark barcode as valid |
| `WriteBarcodeNg(true)` | `DB500.DBX46.1` on both PLCs | Mark barcode as invalid |

---

## 10. Worker Execution Loop

Runs every **50 milliseconds** in this exact order:

```
┌─── Loop Start (every 50ms) ───────────────────────┐
│                                                     │
│  1. SystemState.Plc1/Plc2 = IsConnected            │
│  2. SystemService.Update()     → read system flags  │
│  3. CraneService.Update()      → read crane data    │
│  4. PlcBarcodeReader.ReadAll() → read barcodes       │
│  5. LocationRouter.Execute()   → process/send jobs   │
│  6. ShuttleService.Update()    → read shuttle data   │
│  7. SystemState.Plc1/Plc2 = IsConnected (refresh)   │
│                                                     │
└─── Wait 50ms ─────────────────────────────────────┘
```

Each step is wrapped in try-catch — errors are logged but never stop the loop.

---

## 11. TCP Socket Bridge Protocol

For non-HTTP clients (e.g., embedded systems, legacy tools).

**Server:** `0.0.0.0:9000` (UTF-8 text protocol)

### Commands

| Send | Receive | Description |
|------|---------|-------------|
| `PING` | `PONG` | Health check |
| `GET_STATUS` | Full JSON | Same data as `GET /status` |

### Error Response

```json
{
  "error": "WcsSystem unreachable",
  "detail": "connection refused",
  "hint": "Make sure WcsSystem is running on http://127.0.0.1:5000"
}
```

### Timeouts
- TCP I/O: 5 seconds
- HTTP fetch: 3 seconds

---

## 12. Monitor Dashboard (TestTools)

**File:** `TestTools/wcs_monitor.html`
**Open:** directly in browser (`file:///...` or serve via HTTP)
**Polls:** `GET /api/status` every 1 second

### Displayed Sections

| Section | Data Shown |
|---------|------------|
| PLC Connection | Green/Red indicator per PLC |
| Barcode | 14-char barcode + OK/NG badge per PLC |
| Crane 1 & 2 | X, Z position + FREE/BUSY/ERROR status + error code |
| Shuttle 1 & 2 | X, Z, B position + status + battery % + error code |
| Last Location | CommandType, Xin/Zin/Bin, Xout/Zout/Bout, Mode |
| System State 1 & 2 | Auto/Manual, Running, Stop, Error + error code |

### Status Badge Logic

```
ERROR  → Red badge    (error == true)
BUSY   → Yellow badge (busy == true)
FREE   → Green badge  (free == true)
IDLE   → Gray badge   (none of the above)
```

---

## 13. Configuration & Constants

### appsettings.json (PLC connection)

```json
{
  "Plc1Settings": {
    "IpAddress": "192.168.39.10",
    "Rack": 0,
    "Slot": 1,
    "CpuType": "S71200"
  },
  "Plc2Settings": {
    "IpAddress": "192.168.39.13",
    "Rack": 0,
    "Slot": 1,
    "CpuType": "S71200"
  }
}
```

### Key Constants

| Constant | Value | Location |
|----------|-------|----------|
| HTTP Port | 5000 | Program.cs |
| TCP Port | 9000 | TcpSocket/Program.cs |
| Worker Loop | 50ms | Worker.cs |
| Reconnect Throttle | 5s | S7Connector.cs |
| Barcode Length | 14 chars | PlcBarcodeReader.cs |
| Barcode Start Offset | DBB32 | PlcBarcodeReader.cs |
| X Boundary (zone split) | 13/14 | LocationRouter.cs |
| Shuttle B range PLC1 | {1, 2} | LocationRouter.cs |
| Shuttle B range PLC2 | {2, 3} | LocationRouter.cs |
| Shuttle X range PLC1 | 1-13 | LocationRouter.cs |
| Shuttle X range PLC2 | 14-26 | LocationRouter.cs |

### Dependency Injection (all Singletons)

| Service | Purpose |
|---------|---------|
| Plc1Connector | S7 connection to PLC1 |
| Plc2Connector | S7 connection to PLC2 |
| SystemState | Shared in-memory state |
| PlcBarcodeReader | Barcode reading |
| LocationRouter | Job routing |
| CraneService | Crane data collection |
| ShuttleService | Shuttle data collection |
| SystemService | System flags collection |
| TcpClientService | TCP bridge client |
| Worker (HostedService) | 50ms background loop |

### Startup Sequence

1. Worker starts, waits 500ms
2. TCP ping to `127.0.0.1:9000`
3. Connect PLC1
4. Connect PLC2
5. Enter main 50ms loop
