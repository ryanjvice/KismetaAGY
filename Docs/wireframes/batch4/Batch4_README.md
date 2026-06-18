# Batch 4 — Summer actions (flow-map #18–25, 27)

The freeform engine season: the two action sheets, the five action screens, the End
Summer CTA, and the Summer main-scene C# back-fill. Introduces the **reagent stepper**
and **transaction strip** patterns reused by Autumn and the contests.

## Screens in this batch

| # | Screen | UXML | C# | USS |
|---|--------|------|----|-----|
| 17 | Summer main scene (back-fill C#) | *(prebuilt)* | `SummerSceneController` | — |
| 18 | Craft & build sheet | `SummerSheets.uxml` | `SummerSheetsController` | `SummerActions.uss` |
| 19 | Craft a reagent | `CraftReagent.uxml` | `CraftReagentController` | `SummerActions.uss` |
| 20 | Forge result | *(inline in `CraftReagent`)* | *(same controller)* | — |
| 21 | Activate crucible card | `ActivateCard.uxml` | `ActivateController` | `SummerActions.uss` |
| 22 | Light cauldron (explainer) | *(inline in `SummerSheets`/`ActivateCard` tips)* | — | — |
| 23 | Build astral house | `BuildHouse.uxml` | `BuildHouseController` | `SummerActions.uss` |
| 24 | Place wards | `PlaceWards.uxml` | `PlaceWardsController` | `SummerActions.uss` |
| 25 | Consort sheet | *(in `SummerSheets`)* | `SummerSheetsController` | `SummerActions.uss` |
| 27 | End Summer CTA | `EndSummer.uxml` | `EndSummerController` | `SummerActions.uss` |

`SummerSheets.uxml` holds **both** menus (Craft & build + Consort) as sibling roots;
`SummerSheetsController.ShowCraftBuild(bool)` toggles which is visible. Forge result (#20)
is an inline hidden panel revealed after crafting. Light cauldron (#22) is an explainer
surfaced via tips (lighting is a consequence of Activate, not a standalone action).

## Reusable patterns introduced

- **`ReagentStepper`** (plain C# helper) — tracks per-reagent spend against a cap and
  per-type availability. Used by Place wards now; Fire / Gambit fee / Opposition fee in
  Batches 5–6 reuse it.
- **Transaction strip** (`.txn`) — "inputs → output" (3 cards → 1 reagent). Reused by
  contest results.
- **Reagent picker** (`.reagent-pick`) — suit-filtered reagent selection with
  locked/dormant states.
- **Formula slots** (`.formula-slot`) — filled chips + dashed empty slots for Activate.

## NATIVE NOTES in this batch

- **Sheets as overlays** — present with a scrim; animate the rise in C#.
- **Craft card filtering** — picking a reagent re-filters the pool by suit (non-matching
  cards dim to ~22%); built from game state.
- **Activate formula** — auto-evaluate the chosen crucible card against the Spread; build
  filled/empty slots in C#.
- **Build cost is mode-aware** — Quickplay halves the cost (1 card); drive cost slots from
  the active `GameMode`.
- **Ward steppers** — supply decrements live; existing vs new wards differ in color.

## Status

#17–25, 27 layout-complete with controllers; `SummerSceneController` back-filled and its
duplicate stub removed from `MainSceneControllers.cs` (which now holds only
`AutumnSceneController`, to be folded into Batch 6). Next: **Batch 5 — contests**
(#26, 33, 47–49): Trade, Duel, Gambit, Opposition + the Stasis result, sharing the
dual-die wizard.
