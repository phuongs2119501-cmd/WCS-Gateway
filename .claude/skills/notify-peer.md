---
name: notify-peer
description: After you post_message to be-pm with a non-consult topic, immediately wake them with a brief headless claude --print run so they read it in ~30s instead of waiting for their next session start. SKIP this skill when the topic is consult.* (consult-peer handles that synchronously) or when you yourself are running as a wake child (one level deep only — see Responder section).
---

# notify-peer (Gateway side)

Your peer is **`be-pm`** at `D:/Sources/WCS-Application`. You are `gateway-pm`.

This is the "I posted a heads-up; ping them so they see it now, not at their next session" skill. It runs **after** any `post_message` whose topic is **not** `consult.*` AND when **you are not yourself a wake child**. The wake is bounded to one level deep — the responder must NOT wake back.

## Asker pattern (you just posted)

Right after `post_message` returns OK, invoke a single Bash call (run through the **Bash tool**, bash). The `--permission-mode bypassPermissions` flag is required so the headless run can call the wcs-mcp tools without an interactive permission prompt (which would hang under `--print`):

```
( cd "D:/Sources/WCS-Application" && claude --print --max-turns 3 --permission-mode bypassPermissions "You were woken by a sender-side wake from gateway-pm. Do this and ONLY this: (1) read_messages({ for: 'be-pm', unread_only: true }). (2) For each unread message: read it; if a short reply is useful, post_message({ from: 'be-pm', to: 'gateway-pm', topic: '<keep theirs or extend>', body: '<short reply>' }) — but DO NOT invoke notify-peer or consult-peer, DO NOT spawn another claude --print, DO NOT edit files in this workspace. (3) ack_message({ id }) each. Stop after that." )
```

> The older `-C <dir>` flag was removed from the CLI — `cd` into the workspace instead. A `Reached max turns (3)` / exit 1 is **normal** (the wake still fired); don't treat it as a failure. PowerShell 5.1 has no `&&`, so use `Set-Location "D:/Sources/WCS-Application"; claude ...` on separate statements.

Use a Bash timeout of ~90s. After it returns, **continue your own task** — you do NOT need to read be-pm's reply right now. If they replied, it'll surface at your next session boot via the standard inbox check.

If the wake call fails or times out: don't retry, don't escalate to PO. The message is already durable in be-pm's inbox; they'll see it on next session start. Just note "wake failed, peer will catch up at next session" in your task summary if it matters.

## Responder pattern (your session was woken)

If your *own* session started because someone spawned `claude --print -C D:/Sources/WCS-Gateway` with a wake instruction (you'll see a "You were woken by a sender-side wake from be-pm" sentence at the top of your prompt), **honor the constraints**:

1. `read_messages({ for: "gateway-pm", unread_only: true })`.
2. For each unread: read it; if a short reply is genuinely useful, `post_message` back to be-pm with a brief body.
3. `ack_message({ id })` each.
4. **Do NOT invoke notify-peer.** Do NOT invoke consult-peer. Do NOT spawn another `claude --print`. Do NOT edit files in this workspace.
5. Do NOT pick up unrelated workspace work. Your job in this run is just to acknowledge and reply if useful.
6. Stop.

The one-level-deep rule is the **only** thing preventing A→B→A→B wake-loop runaway. Break it and you'll burn tokens in an infinite ping-pong.

## When NOT to use

- **`consult.*` topics** — use `consult-peer` instead (synchronous, asker parses the reply mid-turn).
- **You're a wake child yourself** — your wake instruction explicitly tells you to skip this.
- **Rapid-fire same-topic posts within seconds** — let the first wake cover the batch.

## Cost / latency notes

- Each wake = one short headless Claude run, ~3 turns max, typically ~10–30s, ends within the 90s timeout.
- Asker pays the Bash blocking time. Cheaper than consult-peer because there's no reply-parsing afterward.
- If be-pm is mid-heavy-task in another session, the wake's `--max-turns 3` will exhaust and exit cleanly.

## Why this exists

Without this, a heads-up `post_message` sits in be-pm's inbox until they next open Claude Code in the BE workspace — which could be hours later. This skill turns "BE will know eventually" into "BE knows in 30 seconds," with no background daemon and no PO involvement.
