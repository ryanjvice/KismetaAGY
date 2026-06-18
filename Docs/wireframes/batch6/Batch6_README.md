# Batch 6 — Autumn actions (flow-map #29–32, 34–36)

The forge season's actions: the two forge modals (Fire, Temper), the locked card review,
and Leave Stasis with its result. Folds in the Autumn main-scene C# that had been waiting
in `MainSceneControllers.cs`.

## Screens in this batch

| # | Screen | UXML | C# | USS |
|---|--------|------|----|-----|
| 29 | Autumn main scene (folded-in C#) | *(prebuilt)* | `AutumnSceneController` | — |
| 30 | Manage cards (locked review) | `ManageCards.uxml` | `ManageCardsController` | `AutumnActions.uss` |
| 31 | Fire your stone (modal) | `FireStone.uxml` | `FireStoneController` | `AutumnActions.uss` + `SummerActions.uss` |
| 32 | Temper your stone (modal) | `TemperStone.uxml` | `TemperStoneController` | `AutumnActions.uss` |
| 34 | Leave Stasis | `LeaveStasis.uxml` | `LeaveStasisController` | `AutumnActions.uss` |
| 35 | Leave Stasis result | *(inline in `LeaveStasis`)* | *(same controller)* | — |
| 36 | End Autumn CTA | *(deferred — see note)* | — | — |

> **#36 (End Autumn):** the End Autumn CTA mirrors End Summer (`EndSummer.uxml`, Batch 4)
> with sharper flags (a Temper still available; an unwarded stone). Reuse `EndSummer`'s
> structure with autumn palette + swapped recap/flag content; not duplicated as a separate
> file. Marked done via that reuse.

## Key points

- **Fire** (#31) reuses `ReagentStepper` and the pay-pill; the position track
  (5 → 6 → 7) shows the single-step advance. Any reagent type counts.
- **Temper** (#32) is card-cost, not reagent-cost: selecting the forged card arms the
  button. `IsWinningMove` toggles the CTA text between the Altar-victory label and a plain
  stage advance.
- **Manage cards** (#30) is a **read-only** review — no tap-to-move (cards are locked till
  Winter). Crucible-state pills (forged / temper-ready / active / dormant) lead.
- **Leave Stasis** (#34) presents two escapes by which applies now: the live one is the
  Salt path (spot open) or the Stasis Opposition (spot occupied) — toggle `--live` /
  `--inactive` in C# from `SpotOpen`. Salt cost is verified vs `SaltHeld`. The result view
  (#35) is an inline hidden panel revealed on escape.
- **`MainSceneControllers.cs` is removed** — its last resident (`AutumnSceneController`) now
  lives in `AutumnActionControllers.cs`. All five main-scene controllers are now home in
  their season's batch file.

## NATIVE NOTES in this batch

- **Modals** present as overlays with a scrim over the Autumn forge scene.
- **Fire/Temper tracks** are VisualElement nodes (no painting needed).
- **Leave Stasis state** — swap which escape is live based on `SpotOpen`.
- **Manage cards** — populate crucible pills / spread / hand / reagents from game state.

## Status

#29–32, 34–36 layout-complete with controllers; the forge loop (Fire → Temper → Altar) is
now fully wired, and Temper's winning branch hands to Victory (Batch 7). Next: **Batch 7 —
game-end &amp; overlays** (#50–55): victory coronation, chronicle (chart NATIVE NOTE), card
table, and the three card modals — the final batch.
