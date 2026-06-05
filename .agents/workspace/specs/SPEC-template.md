# SPEC-<id>: <short title>

- **Status:** draft | ratified | superseded
- **Author:** gateway-pm
- **Date:** YYYY-MM-DD
- **Related:** <contract.* / decision.* / TASK-id / peer thread>

## Goal (physical/operational outcome)
<What should the system actually do, in terms of cranes/shuttles/jobs — not code.>

## Why now
<Driver: PO request, BE contract, bug, etc.>

## Scope
- **In:** <one feature slice>
- **Out:** <explicitly deferred>

## Behavior
<How it works: which Worker step, which DB500 offsets, which state. Trace the happy path + the failure paths.>

## Contract impact
- DB500 offsets touched: <none | list>
- `/status` fields / `LocationModel` changed: <none | list>
- Docs to update + peer to notify: <contract.gateway-db-keys → be-pm | none>

## Acceptance criteria (checkable)
- [ ] <criterion>
- [ ] `dotnet build` (WcsSystem/) passes
- [ ] Ran through `critical-thinking` skill → verdict recorded

## Open questions
<For PO / be-pm. If multi-round, escalate rather than guess.>
