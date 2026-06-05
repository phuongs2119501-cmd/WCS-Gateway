---
name: brainstorming
description: Divergent ideation mode for WCS-Gateway. Invoke at the start of a feature or architecture decision to generate and pressure-test multiple options before committing — e.g. how to model a new PLC interaction, restructure routing, or add a capability. Diverge first, then converge to a recommendation.
---

# brainstorming (WCS-Gateway)

For the fuzzy front end of a change, before a SPEC exists. Goal: surface the option space so we don't commit to the first idea that compiles.

## Flow

1. **Frame the real goal** — the physical/operational outcome, not the code change. ("Two jobs must never fight over one crane," not "add a lock field.")
2. **Diverge — generate ≥3 distinct options.** Force genuinely different shapes, not variations:
   - Where does the logic live? (PLC program vs Gateway vs BE) — moving it across that boundary is often the real choice.
   - State in `SystemState` vs in the PLC's DB500 vs in a BE record?
   - Poll in the 50ms loop vs event/edge-triggered?
3. **For each option, one line each:** how it works, blast radius (which contracts/peers it touches), and the main risk.
4. **Converge** — score against: safety (can't strand a machine) > fits the 50ms loop > minimal contract churn > simplicity. Pick one, name the runner-up and why it lost.
5. **Hand off** — the winner becomes a `SPEC-<id>.md`; cross-boundary picks get a `decision.<id>` doc + a heads-up to the affected peer.

## WCS-specific tensions to weigh
- **Gateway vs PLC program:** the PLC program is hard to change (vendor, funded change). True mid-task cancel, for example, is a PLC change — prefer Gateway-side designs that work within the existing DB500 contract when possible.
- **Both-PLC symmetry:** any new feature usually needs a PLC1 and PLC2 path — does the option keep them symmetric?
- **Idempotency:** new BE-facing entry points need an idempotency story (`acceptedKey`) from day one, not bolted on.

Keep it fast and cheap. The output is options + a recommendation, not a decision — the PO or a SPEC ratifies.
