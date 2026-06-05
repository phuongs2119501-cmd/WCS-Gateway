---
name: discussing
description: Structured cross-workspace discussion for WCS-Gateway. Invoke when a decision or question involves be-pm (BE), fe-pm (FE), or the PO — picks the right channel (consult vs post vs escalate), keeps identity correct (gateway-pm), and routes contract changes to the right wcs-mcp doc.
---

# discussing (WCS-Gateway)

You are `gateway-pm`. Counterparts: `be-pm` (WCS-Application), `fe-pm` (WCS-FE, usually indirect), `po` (human architect). The server is convention-only — always send `from:"gateway-pm"`, never impersonate.

## Pick the channel

| Situation | Tool |
|---|---|
| Heads-up / contract shipped, no answer needed this turn | `post_message` → then **one** `notify-peer` wake (skip if you're a wake child) |
| You need their answer **before this turn ends**, one round-trip | `consult-peer` skill, topic `consult.<slug>` |
| Open-ended, multi-round, or a real decision | escalate to **PO** — do not loop the peer |
| Frozen decision / durable knowledge | `put_doc` (`decision.<id>`, `contract.*`, `rules.gateway`) |

## Discipline (from rules.shared)
- `read_messages` always `unread_only: true`; **ack first, reply second**.
- Batch: **one** `notify-peer` per turn covering all that turn's posts.
- Default to durable `post_message` *without* a wake — wake only if they need it within minutes.
- One-level-deep wake: if you were woken, never wake back.
- Cache `content_hash`; re-check docs with `if_none_match` / `get_doc_meta`, not blind re-fetch.

## When a change crosses the Gateway boundary
A PLC protocol change (what BE sends/receives, or a DB500 offset) → `put_doc({ name:"contract.plc.<area>" | "contract.gateway-db-keys", author:"gateway-pm", expected_version })` **and** `post_message` be-pm with `refs:[<doc>]`. Read current version first; pass `expected_version` to avoid clobbering be-pm's edits (`0` = create-only).

## Framing a question so it can be answered cold
State: what you're about to change, the exact PLC command/result shape involved, and the single thing you need decided. Cite `file:line`. Cold-answerable questions get answered in one round; vague ones bounce.
