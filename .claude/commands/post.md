---
argument-hint: <to> | kind:<notify|consult|command> | topic:<...> | context…
description: Power-prompt — startup ritual, then post a durable message to a teammate via wcs-mcp
---

Arguments: $ARGUMENTS

Send a durable message via the `post_message` tool on the **wcs-mcp** server.
`from` is ALWAYS this workspace's own identity string (see your CLAUDE.md) — never another agent's.

Follow these 4 steps in order:

## [1. NGHI THỨC] Startup ritual (always first)
- whoami: confirm this workspace's identity string.
- Read my own inbox (read_messages) and surface anything unacked.
- Load the rules I need for this task.

## [2. KỸ NĂNG] Kind
- Pick `kind` from the arguments. It MUST be one of: notify | consult | command.
- If it is missing or invalid, use AskUserQuestion to make me choose — never guess.

## [3. NGỮ CẢNH] Context
- `to`: a teammate — be-pm, fe-pm, wcs-design, or po (never myself).
  If missing/ambiguous, use AskUserQuestion to pick from the other members.
- `topic`: short subject line.
- body/context: everything else I typed — pass it through as the message body.
- If the body is empty, ask me before sending.

## [4. ĐẦU RA] Output (optional)
- If I said what I want back, include it as the closing ask in the message.
- After sending, confirm to me: who it went to, the kind, the topic.
