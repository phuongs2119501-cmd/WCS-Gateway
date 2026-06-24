# QUEUE — WCS-Gateway (gateway-pm)

> Boot file. Single entry point — read this first. Keep ≤50 lines, scannable.
> Flow: SPEC → TASK → dispatch Codex → REPORT → move row to Done.
> Blockers struck through, never deleted (audit trail). Details live in tasks/<phase>/.

## 🔵 Active (dispatched, in flight)
_(none)_

## 🟡 Pending Review (REPORT written, awaiting PM verify)
_(none yet)_

## 🟢 Done
| Date | Task | What shipped | REPORT |
|------|------|--------------|--------|
| 2026-06-05 | bootstrap | .agents workspace + 5 skills scaffolded | — |
| 2026-06-05 | GW-002 research+publish | `contract.gateway-db-keys` v1 + `Config/db500-keys.json` (full DB500 map from live source) | SPEC-GW-002 |
| 2026-06-05 | GW-005 research | `spec.gateway-simulator` v1 + SPEC-GW-005 (fake-PLC echo design) | SPEC-GW-005 |
| 2026-06-05 | GW-005 impl | `IS7Backend`/`RealS7Backend`/`SimS7Backend` + sim-fixture + `UseMock` wiring; sim echo verified, real path unchanged | REPORT-GW-005 |
| 2026-06-05 | GW-002 impl | `tools/Db500MapGen` + `Db500Map.g.cs`; 5 Services refactored to `Db500Map.*`, byte-identical addresses | REPORT-GW-002 |
| 2026-06-22 | GW-006 realmap sim + selected-shuttle | `SimS7Backend` re-pointed to `Db500Map.*` (no literals) at v3 offsets; sim moves crane + ONLY the routed shuttle (modeCraneShuttle DBW2, lazy); `/api/status` flat contract on HomeController. Verified: build 0/0, TC1–TC6, diff reviewed | tasks/realmap/REPORT-GW-006.md |

## 📋 Backlog (specced or known, not dispatched)
- **GW-001 acceptedKey on crane status** — implement `contract.crane-contention-lock v2`: set `acceptedKey` when LocationRouter binds a task (== X-Idempotency-Key), clear on complete/fail; gate late cancel. _Not in code yet._
- **GW-003 port-drift docs** — code now uses 5050; CLAUDE.md + README still say 5000. Reconcile docs.
- **GW-004 reconcile with rules.pm-workflow** — fork be-pm's shared QUEUE/TASK skeleton once published; align structure.
- **GW-007 crane/shuttle read-topology inconsistency** — `Db500Map.g.cs` now matches owner's `main` map byte-for-byte (offsets resolved). BUT our Services diverge from main's "each PLC mirrors all 4 devices" model AND contradict each other: `CraneService` reads Crane2 from PLC2 at **crane-1 offsets** (`craneX` DBW58 / `craneFree` DBX16 — `crane2X`/`crane2Free` DBW62/DBX20 never used), while `ShuttleService` reads Shuttle2 from PLC2 at **shuttle-2 offsets** (`shuttle2X` DBW72 / DBX30). One convention is wrong. Decide correct topology (vs owner `main`, where every device-2 is read at its own distinct offset from BOTH PLCs) before fixing. _Verify-only finding 2026-06-23, no code changed._

_GW-002 + GW-005 → shipped (see Done). Verified by gateway-pm: combined `dotnet build` green, sim echo runs, addresses byte-identical._

## 📝 Activity Log (newest first — every dispatch/verify cycle appends)
- 2026-06-23 — Published `contract.gateway-command-flow` v1 (DRAFT) — Gateway↔PLC flow + dispatch/result API handbook for be-pm (DB500 mailbox+50ms handshake model, WmsCommandModel envelope, `/api/status` polling for done/fail, Transfer CommandType=3 proposal, barcode DBB32→DBB36 + `/status`→`/api/status` corrections to frozen inbound-location-send v3). Posted note to be-pm (msg d36d6c29). Awaiting be-pm+po review (touches frozen v3). — gateway-pm
- 2026-06-23 — Verify-only sweep of Services vs owner `main` (tip-to-tip, merge-base `ad1d2fc`). Confirmed `Db500Map.g.cs` == `main` DataPlc map byte-for-byte (offset delta from reconciliation.md §2 now RESOLVED via GW-006). New finding logged as backlog **GW-007**: crane/shuttle read-topology diverges from main and is self-inconsistent (Crane2→crane1 offsets vs Shuttle2→shuttle2 offsets). Snapshot of owner's confirmed map saved at `d:\tmp\wcs-gateway-main-snapshot-2026-06-23.md`. No code touched. — gateway-pm
- 2026-06-22 — GW-006 VERIFIED + closed: Codex shipped selected-shuttle-only sim (lazy modeCraneShuttle resolve, per-device Busy realistic for be-pm move-history); PM re-ran `dotnet build` 0/0 and reviewed the diff (no literal offsets, crane2 untouched, plcDone handshake intact). Row → Done. — gateway-pm
- 2026-06-22 — Confirmed crane+shuttle bit offsets to be-pm (their LOCKED ask); published `contract.plc.device-bits` v1 from db500-keys v3. Finalized SPEC-GW-006 (selected-shuttle-only realism + corrected /api/status note) and dispatched TASK-GW-006 to Codex (background). — gateway-pm
- 2026-06-05 — GW-005 + GW-002 dispatched to Codex (sequential, background) + verified: combined `dotnet build` 0/0, all 5 Services on `Db500Map.*`, SimS7Backend echo (Busy→move→plcDone→FINISHED) confirmed by Codex, real path byte-identical. QUEUE rows → Done. — gateway-pm
- 2026-06-05 — Caught heartbeat `DBX74.0` missing from catalog; bumped `contract.gateway-db-keys` v1→v2 + JSON minLength 72→76 before dispatch. — gateway-pm
- 2026-06-05 — 3.4 + 3.3 research shipped: read all 5 PLC Services, authored `Config/db500-keys.json` (canonical DB500 map, verified field-by-field), published `contract.gateway-db-keys` v1 + `spec.gateway-simulator` v1, wrote SPEC-GW-002/005. Messaged be-pm. — gateway-pm
- 2026-06-05 — Scaffolded `.agents/workspace/` (QUEUE, codex-dispatch, SPEC/TASK/REPORT templates) + 5 `.claude/skills`. Seeded backlog GW-001..004 from WcsSystem research + be-pm tips. — gateway-pm
