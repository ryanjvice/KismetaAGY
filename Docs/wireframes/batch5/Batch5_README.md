# Batch 5 — Contests (flow-map #26, 33, 47–49)

The four contests plus the Stasis result. The most logic-heavy batch: it introduces the
**dual-die roll-off**, the **step-wizard**, and the **alignment tally**, and reuses the
`ReagentStepper` (Batch 4) for the fee steps.

## Screens in this batch

| # | Screen | UXML | C# | USS |
|---|--------|------|----|-----|
| 26 | Trade | `Trade.uxml` | `TradeController` | `Contests.uss` |
| 33 | Opposition result → Stasis | *(inline in `Opposition`)* | `OppositionController` | `Contests.uss` |
| 47 | Duel | `Duel.uxml` | `DuelController` | `Contests.uss` |
| 48 | Gambit | `Gambit.uxml` | `GambitController` | `Contests.uss` + `SummerActions.uss` |
| 49 | Opposition | `Opposition.uxml` | `OppositionController` | `Contests.uss` + `SummerActions.uss` |

## Shared contest framework

`ContestController` (base) provides the common plumbing the three dice contests share:

- **`ShowStep(panel, allPanels)`** — stacked step panels; show one at a time.
- **`SetWizard(prefix, total, active)`** — drives the step-dot rail (done / active / upcoming).
- **`RollDie(pipLabel, result)`** — a coroutine that tumbles a die through random faces
  and settles on the rolled value. Ties reroll in each contest's resolve logic.

The three dice contests differ exactly as the design intended:

- **Duel** (3 steps) — target → ante → roll. The die *is* the score.
- **Gambit** (4 steps) — target → **ward fee** → stake → roll. Fee step reuses
  `ReagentStepper`; **skipped automatically when the target is unwarded** (`TargetWarded`
  flag jumps straight to stake).
- **Opposition** (3 steps) — fee → roll to **set your sign** → **alignment tally**. The
  die sets the sign; the *higher total* wins, not the die. Win shows the inline Stasis
  result (#33).

**Trade** (#26) is structurally separate — two trays (give/get), public cards only, a
ratio hint, and it *proposes* (needs consent) rather than resolving instantly. So
`TradeController` is a plain `ScreenController`, not a `ContestController`.

## NATIVE NOTES in this batch

- **Dice animation** — `RollDie` swaps glyph text per frame; replace with sprite frames or
  a 3D die for production.
- **Fee steps** reuse `ReagentStepper`; wire the supply from real reagent counts.
- **Gambit unwarded skip** — set `TargetWarded=false` to bypass the fee step.
- **Opposition tally** — the rows are illustrative; compute them from the set sign vs the
  current age (sign +3 / planet +2 / element +1, highest only) in real code.
- **Trade trays** — build tappable give/get pools in C#; only the rival's *public* cards
  appear in the "get" tray (hidden Hand is unreachable). Ratio line is mode-aware (Magnus 2:1).

## Status

#26, 33, 47–49 layout-complete with controllers; dice/wizard/tally framework established
and shared. Next: **Batch 6 — Autumn actions** (#30–32, 34–36): Manage cards, Fire,
Temper, Leave Stasis + result, plus folding in the `AutumnSceneController` C# that's been
waiting in `MainSceneControllers.cs`.
