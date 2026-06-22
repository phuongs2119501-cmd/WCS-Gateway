---
name: convene-team
description: Bring the WHOLE team into one discussion. Broadcast a message to all other members (be-pm, fe-pm, wcs-design) and wake each once so they weigh in. Use when a change has broad impact and the PO says something like "this affects the app, get everyone's take" / "call a meeting". One bounded round.
---

# convene-team (Gateway side)

You are **`gateway-pm`** (`D:/Sources/WCS-Gateway`). Use this when a change or decision
needs input from **more than one** teammate — not a single heads-up. Typical trigger:
PO says *"small change but it affects the current app, get everyone in to discuss"* —
e.g. a PLC protocol change that ripples into both the API and the UI.

Team roster (message all EXCEPT yourself):

| Teammate | Workspace dir | Owns |
| --- | --- | --- |
| `be-pm` | `D:/Sources/WCS-Application` | backend / API |
| `fe-pm` | `D:/Sources/WCS-FE` | frontend / UI |
| `wcs-design` | `D:/Sources/WHS` | 3D layout designer / warehouse-layout JSON |

## Pattern

1. **Anchor the context in a doc** so everyone reads the same source:
   `put_doc({ name: "spec.<slug>" or "decision.<slug>", content: "<the change, why, and the specific question each teammate should answer>", author: "gateway-pm" })`.
2. **Broadcast** to all other members at once:
   ```
   broadcast({ from: "gateway-pm", to: ["be-pm", "fe-pm", "wcs-design"],
     topic: "consult.<slug>",
     body: "<one-paragraph change + impact + the exact question you need each to answer>",
     refs: ["spec.<slug>"] })
   ```
   (Use a `consult.<slug>` topic so each member knows a reply is expected.)
3. **Wake each recipient once** — loop, one headless run per teammate (`<m>` = id,
   `<m-dir>` = their workspace dir):
   ```
   ( cd "<m-dir>" && claude --print --max-turns 5 --permission-mode bypassPermissions "You were convened by gateway-pm for a team discussion. Do ONLY this: (1) read_messages({ for: '<m>', unread_only: true }) and get_doc the referenced spec/decision. (2) Post your view: post_message({ from: '<m>', to: 'gateway-pm', topic: '<same consult topic>', body: '<your take + impact on your area, cite file paths>' }). (3) ack the original. Do NOT convene, notify, consult, or wake anyone; do NOT edit files. Stop." )
   ```
   Bash timeout ~120s per member.
4. **Collect & synthesize:** `read_messages({ for: "gateway-pm", unread_only: true })`,
   gather everyone's responses on the topic, summarize the points of agreement /
   conflict, and report to the PO. Ack each response.

## Loop safety (critical)

A convened member is a **wake child**: it weighs in with a durable message and does
**NOT** convene, notify, consult, or wake anyone. This keeps a team meeting to **one
round**. If the discussion needs another round, the **PO** drives it (or you convene
again deliberately). Never let a woken member re-broadcast — that is the only thing
preventing a wake storm across the whole team.

## When NOT to use

- Only one teammate is involved → `notify-member` (FYI) or `consult-member` (need an
  answer).
- A frozen decision to announce, no discussion needed → `put_doc decision.*` +
  `broadcast` with a plain (non-`consult.`) topic; no wakes required if not urgent.
