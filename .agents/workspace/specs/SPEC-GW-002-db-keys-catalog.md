# SPEC-GW-002: Externalized DB500 key catalog + C# model generator

- **Status:** ratified
- **Author:** gateway-pm
- **Date:** 2026-06-05
- **Related:** contract.gateway-db-keys (v1, published), be-pm thread "Inbound meeting agenda" 3.4, contract.inbound-location-send

## Goal (physical/operational outcome)
Every PLC offset the Gateway reads/writes is described in ONE externalized file so adding or removing a
field is a single JSON edit + regen — no chasing string constants scattered across Services. BE/FE
consume the same catalog (the published `contract.gateway-db-keys`) to interpret PLC results.

## Why now
be-pm 3.4 ask (PO inbound month). BE wants a shared field dictionary as source of truth; today the
offsets are hard-coded `const string`s duplicated across SystemService/CraneService/ShuttleService/
PlcBarcodeReader/LocationRouter — drift-prone, opaque to BE/FE.

## Scope
- **In:** `Config/db500-keys.json` (DONE — authored from live source); a generator emitting a typed
  `Db500Map` partial; refactor Services to reference `Db500Map.*` instead of local consts.
- **Out:** changing any actual offset/behavior; runtime hot-reload of the JSON.

## Behavior
`db500-keys.json` holds `{ meta, regions, fields[], perConnectorUsage }`. Each field = `{ key, address,
type, access, region, meaning, length? }`. Generator (build-time T4 or a `dotnet run` tool) reads the
JSON and emits, per field: a `const string <Key>Addr = "DB500..."` plus a typed accessor; `access`
governs setter emission (read = getter only; write/readwrite = getter+setter). Services swap their
private consts for `Db500Map.*`. Env `WCS_DB500_KEYS` can point at an override JSON.

## Contract impact
- DB500 offsets touched: none (pure externalization — same addresses).
- `/status` / `LocationModel`: unchanged.
- Docs: contract.gateway-db-keys published v1 → be-pm/fe-pm consume. Notify be-pm on publish (done).

## Acceptance criteria (checkable)
- [x] `Config/db500-keys.json` matches every const in the 5 Services (verified field-by-field).
- [x] `contract.gateway-db-keys` published with offset map + generator shape.
- [ ] Generator emits `Db500Map`; the 5 Services compile against it with no offset change.
- [ ] `dotnet build` (WcsSystem/) passes; `/status` output byte-identical before/after.
- [ ] Ran through `critical-thinking` skill → verdict recorded.

## Open questions
- Generator delivery: T4 template vs standalone `tools/` console gen? Lean console gen (simplest, no
  MSBuild coupling). Confirm at dispatch.
