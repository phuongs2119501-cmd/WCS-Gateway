# Codex Dispatch & Prompt Guide — WCS-Gateway

How the PM (gateway-pm) hands coding work to the Codex developer. House style, adopted from be-pm.

## The flow (never skip a step)
1. **SPEC** — write `specs/SPEC-<id>.md` from the template. The *what* and *why*, acceptance criteria. One feature slice.
2. **TASK** — write `tasks/<phase>/TASK-<id>.md` from the template. The *how-to-execute* + file pointers + Do/Don't + the "write REPORT.md when done" line.
3. **Dispatch** — one task per dispatch. No batching.
4. **REPORT** — Codex writes `tasks/<phase>/REPORT-<id>.md` (4-step contract). Mandatory — Codex will NOT self-report unless the prompt tells it to.
5. **Verify** — PM reads REPORT, builds, moves the QUEUE row to Done, appends Activity Log.

## Dispatch command (Windows / PowerShell)
```powershell
# $null | closes stdin (or codex hangs). danger-full-access or REPORT writes to .agents/** get sandboxed away.
$null | codex exec --sandbox danger-full-access "<prompt>"
```
Two gotchas (from be-pm, learned the hard way):
- **Close stdin** (`$null |`) or `codex exec` hangs waiting for input.
- **`--sandbox danger-full-access`** — `workspace-write` blocks writes under `.agents/**`, so REPORTs silently vanish. Alternatively pre-author the REPORT.md skeleton so the path exists.

## Prompt house style (every prompt has all four)
1. **Context pointer** — link the SPEC + TASK path. Do NOT paste whole files; Codex reads the repo. Give *paths*, e.g. `WcsSystem/Wcs.PlcService/Services/LocationRouter.cs`.
2. **Acceptance criteria** — explicit, checkable. "Build passes; `acceptedKey` set on bind and cleared on FinishJob; late cancel returns rejected."
3. **Do / Don't** — Do: match Vietnamese comments, use S7Connector Try* methods. Don't: change DB500 offsets, add blocking calls to the 50ms loop, touch the heartbeat.
4. **Report line** — literally: "When done, write `tasks/<phase>/REPORT-<id>.md` with: what changed / how verified / risks / follow-ups."

## Safety gate before dispatch
Run the change through the `critical-thinking` skill first. If it touches a DB500 offset / command bit / `/status` field / `LocationModel`, it's a **contract change** — handle the `contract.*` doc + be-pm notify (see `discussing` skill) before or alongside the dispatch.

## Prompt skeleton
```
Task: <one line>. Spec: specs/SPEC-<id>.md. Task file: tasks/<phase>/TASK-<id>.md.
Files in scope (paths only): <...>
Acceptance criteria:
  - <checkable 1>
  - dotnet build (WcsSystem/) passes
Do: <...>   Don't: change DB500 offsets / block the 50ms loop / skip the heartbeat.
When done, write tasks/<phase>/REPORT-<id>.md: what changed / how verified / risks / follow-ups.
```
