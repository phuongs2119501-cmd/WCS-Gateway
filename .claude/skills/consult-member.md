---
name: consult-member
description: Synchronously ask a teammate (be-pm, fe-pm, or wcs-design) a question mid-task and get the answer back in the same turn. Use when you need their answer before you can finish. For fire-and-forget heads-ups use a plain post_message; to bring in the WHOLE team use convene-team.
---

# consult-member (Gateway side)

You are **`gateway-pm`** (`D:/Sources/WCS-Gateway`, PLC / hardware gateway). You are
one of a **team** — consult **whichever teammate** owns the answer; there is no fixed
peer:

| Teammate | Workspace dir | Ask them about |
| --- | --- | --- |
| `be-pm` | `D:/Sources/WCS-Application` | what COMMAND shape BE sends, ack timing, how BE consumes PLC results |
| `fe-pm` | `D:/Sources/WCS-FE` | how hardware state is shown in the UI, what the operator sees |
| `wcs-design` | `D:/Sources/WHS` | the warehouse-layout JSON — physical positions/dimensions a layout assumes |

This skill blocks your turn while `<target>` is woken in a headless session, reads its
inbox, posts a reply, and acks. The durable trail in `wcs-mcp` is identical to normal
traffic — this just orchestrates it synchronously.

## When to use

- You need `<target>`'s answer **before this turn can finish**, and one round-trip is
  enough. If you expect a debate or need several teammates, use `convene-team` or
  surface to PO.

## When NOT to use

- You only want to inform them → plain `post_message` (+ `notify-member` if urgent).
- Open-ended design question → take to PO.

## You are the asker

1. **Pick a topic** with the `consult.` prefix: `consult.<slug>`.
2. **Post the question** with enough context to answer cold:
   ```
   post_message({ from: "gateway-pm", to: "<target>", topic: "consult.<slug>",
     body: "<question + relevant PLC message/protocol detail + what you're about to change + what you need>" })
   ```
3. **Spawn `<target>` headlessly and block** (Bash tool). `<target-dir>` from the table:
   ```
   ( cd "<target-dir>" && claude --print --max-turns 5 --permission-mode bypassPermissions "You have unread mail in wcs-mcp with topic prefix 'consult.'. Call read_messages({ for: '<target>', unread_only: true }), answer drawing on this codebase (cite file paths), post_message back to gateway-pm keeping the SAME topic, then ack_message the original. Stop after that — do not start other work, do not wake anyone." )
   ```
   > PowerShell 5.1: `Set-Location "<target-dir>"; claude ...` on separate lines.
   Bash timeout ~120s. Overrun → treat as failure.
4. **Pick up the reply:** `read_messages({ for: "gateway-pm", unread_only: true })`, find
   the matching `consult.<slug>` topic.
5. **Ack:** `ack_message({ id: <reply-id> })`.
6. **Continue** using their answer. If they asked a clarifying question, re-spawn ONCE
   or surface to PO. Never invent the answer.

## You are the responder

If your own session starts and `read_messages` returns a `consult.*` topic from a
teammate — handle it before other work:

1. Read the question; examine this codebase as needed (PLC protocol handlers, message
   contracts, state machines).
2. `post_message({ from: "gateway-pm", to: "<asker>", topic: "<same topic>", body: "<answer with file:line citations>", refs: [<doc names>] })`.
3. `ack_message({ id: <original-id> })`.
4. If spawned via headless `claude --print`, **stop** — the asker is blocked on you.
   Do NOT wake anyone back.

## Failure modes

- **Timeout / no reply on topic** → don't guess. PushNotification the PO with the
  topic + inbox snapshot; pause the task.
- **Vague reply** → one clarification round, then escalate to PO.
- **Topic collision** → suffix a short timestamp (`consult.<slug>-1430`).
