# .agents/workspace — WCS-Gateway PM workspace

The gateway-pm operating surface for driving the Codex developer and tracking what changed.

## Layout
```
.agents/workspace/
  QUEUE.md            ← BOOT FILE. Read first. Active / Pending Review / Done / Backlog / Activity Log. ≤50 lines.
  codex-dispatch.md   ← how to hand work to Codex (dispatch cmd + prompt house style)
  specs/              ← SPEC-<id>.md  (what + why, acceptance criteria)
  tasks/<phase>/      ← TASK-<id>.md (how to execute) + REPORT-<id>.md (Codex's 4-step record)
  *-template.md       ← fork these
```

## Cycle
SPEC → TASK → `$null | codex exec --sandbox danger-full-access "<prompt>"` → REPORT → PM verify → QUEUE Done + Activity Log.

## Where history lives
- **Per-cycle, queryable:** each `REPORT-<id>.md` (what changed / how verified / risks / follow-ups) + the QUEUE Activity Log.
- **Cross-workspace, contract-affecting:** wcs-mcp `contract.*` docs + `post_message` to be-pm. (See the `discussing` skill.)

## Companion skills (`.claude/skills/`)
`coding` · `deep-thinking` · `critical-thinking` · `discussing` · `brainstorming` — invoke per task; `critical-thinking` is the gate before every Codex dispatch.

> Structure forked from be-pm's proven layout; reconcile against the shared `rules.pm-workflow` doc on wcs-mcp once published (backlog GW-004).
