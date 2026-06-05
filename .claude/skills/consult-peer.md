---
name: consult-peer
description: Synchronously ask be-pm (the BE workspace) a question mid-task and get the answer back in the same turn. Use when you need their answer before you can finish what you're doing. For fire-and-forget heads-ups, just call post_message and move on — don't use this skill.
---

# consult-peer (Gateway side)

Your peer is **`be-pm`** at `D:/Sources/WCS-Application`. You are `gateway-pm`.

This skill blocks your turn while the BE PM is woken in a separate headless Claude session, reads its inbox, posts a reply, and acks. It's the synchronous flavor of normal `post_message` traffic — the durable trail in the `wcs-mcp` server is identical.

## When to use

- You need be-pm's answer (API contract shape, command/result message format, what the BE actually expects from Gateway) **before this turn can finish**.
- One round-trip is enough. If you expect a debate, surface to PO instead.

## When NOT to use

- You just want to inform them ("PLC heartbeat contract updated") — that's a normal `post_message`, no spawn needed.
- PO is actively in the room and switching workspaces is faster than a peer spawn.
- The question is open-ended. Take that to PO.

## You are the asker

1. **Pick a topic** with the `consult.` prefix and a short slug:
   - `consult.task-command-schema`
   - `consult.task-result-ack-timing`

2. **Post the question** with enough context that be-pm can answer cold:
   ```
   post_message({
     from: "gateway-pm",
     to:   "be-pm",
     topic: "consult.<slug>",
     body:  "<question + relevant PLC command/result shape, what you're about to change, what you need to know>"
   })
   ```

3. **Spawn be-pm headlessly** and block on it (run through the **Bash tool**, bash). `--permission-mode bypassPermissions` is required so the headless run can call the wcs-mcp tools without an interactive prompt:
   ```
   ( cd "D:/Sources/WCS-Application" && claude --print --max-turns 5 --permission-mode bypassPermissions "You have unread mail in the wcs-mcp server with topic prefix 'consult.'. Call read_messages({ for: 'be-pm', unread_only: true }), answer the question(s) drawing on this BE codebase (cite file paths), call post_message back to gateway-pm keeping the SAME topic, then ack_message the original. Stop after that — do not start other work." )
   ```
   > PowerShell 5.1: `Set-Location "D:/Sources/WCS-Application"; claude ...` on separate statements.
   Set a Bash timeout (~120s). If it overruns, treat as failure.

4. **Pick up the reply** — filter your inbox by topic:
   ```
   read_messages({ for: "gateway-pm", unread_only: true })
   ```
   Find the message with matching `consult.<slug>` topic.

5. **Ack the reply:** `ack_message({ id: <reply-id> })`.

6. **Continue the original task** using be-pm's answer. If they asked a clarifying question, clarify and re-spawn ONCE, or surface to PO. Never invent the answer.

## You are the responder

If your *own* session starts and `read_messages` returns a `consult.*` topic from be-pm — meaning **be-pm has consulted you** — handle it before resuming any other work:

1. Read the question. Examine this codebase as needed (PLC connectors, Services, TCP protocol, REST controllers).
2. `post_message({ from: "gateway-pm", to: "be-pm", topic: "<same topic>", body: "<answer with file:line citations>", refs: [<any contract/spec doc names>] })`.
3. `ack_message({ id: <original-id> })`.
4. If you were spawned via headless `claude --print`, **stop**.

## Failure modes

- **Headless spawn times out / returns no reply** → don't guess. PushNotification the PO with the topic and a `read_messages` snapshot; pause the original task.
- **Reply is vague** → one clarification round (re-spawn once). Beyond that, escalate to PO.
- **Topic collision in same session** → suffix with a short timestamp (`consult.<slug>-1430`).
