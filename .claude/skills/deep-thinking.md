---
name: deep-thinking
description: Deep-analysis mode for WCS-Gateway. Invoke when a problem needs tracing through the read-process-write cycle, PLC timing, cross-loop state, or the LocationRouter state machine — i.e. when the answer is not local to one function. Produces a reasoned model before any code is touched.
---

# deep-thinking (WCS-Gateway)

Use this when "just read the function" is not enough — when behavior emerges from timing, shared state, or the interaction of the 50ms loop with two asynchronous PLCs.

## Method

1. **Restate the problem** in one sentence, in terms of physical effect (what does a crane/shuttle actually do, or fail to do).
2. **Locate it on the cycle.** Which of the 6 Worker steps owns this? What runs *before* and *after* it in the same 50ms tick? (Ordering bugs hide here — e.g. Router writes Mode *before* Shuttle reads it, deliberately.)
3. **Map the state.** What lives in `SystemState` (shared, cross-service) vs. private service fields (`_jobActive`, `_commandSent`) vs. the PLC's own DB500? Who writes each, who reads each, and on which tick?
4. **Walk the timeline.** Trace 2–3 consecutive ticks by hand. PLC writes are not instant from the PLC program's view — a bit you set this tick may be acted on by the PLC several ticks later, and DONE comes back asynchronously.
5. **Enumerate assumptions** and mark which are verified in code vs. assumed about the PLC program (which you cannot see). Assumptions about the PLC side are the usual root cause.
6. **Consider 2–3 models** that fit the symptom, then design the cheapest experiment/log to distinguish them.

## WCS failure-shapes to keep in mind
- **Stale-read-as-zero**: a failed `TryRead` returns `false`; if the caller ignores the bool, state silently freezes or reads 0.
- **Reconnect throttle**: 5s between connect attempts — a "why didn't it recover instantly" is often this.
- **Address vs connection error**: `IsAddressError` keeps the link up on a bad offset; a real disconnect clears logged-errors and marks down.
- **Routing edge**: `Bin==2` splits on `Xin` ranges; out-of-range falls back to PLC1.

Output a written model (state diagram in words is fine) **before** editing. Hand it to the `critical-thinking` skill to attack.
