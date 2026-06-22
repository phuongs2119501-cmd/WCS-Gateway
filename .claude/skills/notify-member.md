---
name: notify-member
description: After you post_message to a teammate (be-pm, fe-pm, or wcs-design) with a non-consult topic, immediately wake them with a brief headless claude --print run so they read it in ~30s instead of at their next session start. SKIP when the topic is consult.* (use consult-member) or when you yourself are a wake child. For reaching the WHOLE team at once, use convene-team instead.
---

# notify-member (Gateway side)

You are **`gateway-pm`** (`D:/Sources/WCS-Gateway`, PLC / hardware gateway). You are
one of a **team** on the `wcs-mcp` server — there is **no fixed "peer."** Message
whichever teammate the topic concerns:

| Teammate | Workspace dir | Owns |
| --- | --- | --- |
| `be-pm` | `D:/Sources/WCS-Application` | backend / API |
| `fe-pm` | `D:/Sources/WCS-FE` | frontend / UI |
| `wcs-design` | `D:/Sources/WHS` | 3D layout designer / warehouse-layout JSON |

This is the "I posted a heads-up; ping them so they see it now, not at their next
session" skill. It wakes **whoever you just `post_message`'d**. It runs **after** a
`post_message` whose topic is **not** `consult.*`, and only when **you are not
yourself a wake child** (one level deep — see Responder).

## Asker pattern (you just posted to `<target>`)

`<target>` is the teammate id you sent to; `<target-dir>` is their workspace dir from
the table above. Right after `post_message` returns OK, invoke a single Bash call
(via the **Bash tool**). `--permission-mode bypassPermissions` is required so the
headless run can call wcs-mcp tools without an interactive prompt (which would hang
under `--print`):

```
( cd "<target-dir>" && claude --print --max-turns 3 --permission-mode bypassPermissions "You were woken by a sender-side wake from gateway-pm. Do this and ONLY this: (1) read_messages({ for: '<target>', unread_only: true }). (2) For each unread message: read it; if a short reply is useful, post_message({ from: '<target>', to: 'gateway-pm', topic: '<keep theirs or extend>', body: '<short reply>' }) — but DO NOT invoke notify-member, consult-member, or convene-team, DO NOT spawn another claude --print, DO NOT edit files in this workspace. (3) ack_message({ id }) each. Stop after that." )
```

> The `-C <dir>` flag was removed from the CLI — `cd` into the workspace instead. A
> `Reached max turns (3)` / exit 1 is **normal** (the wake still fired). PowerShell
> 5.1 has no `&&`: use `Set-Location "<target-dir>"; claude ...` on separate lines.

Use a Bash timeout of ~90s. After it returns, **continue your own task** — you do NOT
need to read their reply now; it surfaces at your next session boot.

If the wake fails/times out: don't retry or escalate. The message is durable in
`<target>`'s inbox; they catch up at next session start. Note "wake failed, `<target>`
will catch up" in your summary if it matters.

## Responder pattern (your session was woken)

If your own session started from a `claude --print` wake (you'll see "You were woken
by a sender-side wake from `<someone>`" at the top of your prompt), honor the limits:

1. `read_messages({ for: "gateway-pm", unread_only: true })`.
2. For each unread: read it; short reply if genuinely useful via
   `post_message({ from: "gateway-pm", to: "<someone>", ... })`.
3. `ack_message({ id })` each.
4. **Do NOT invoke notify-member, consult-member, or convene-team. Do NOT spawn
   another `claude --print`. Do NOT edit files.**
5. Stop.

The one-level-deep rule is the **only** thing preventing A→B→A→B wake-loop runaway —
honor it.

## When NOT to use

- **`consult.*` topics** — use `consult-member` (synchronous).
- **You need more than one teammate** — use `convene-team` (broadcast + wake all).
- **You're a wake child** — skip all wakes this run.
- **Rapid-fire same-topic posts** — one wake covers the batch.
