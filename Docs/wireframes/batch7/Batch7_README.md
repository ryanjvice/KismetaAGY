# Batch 7 — Game-end &amp; overlays (flow-map #50–55)

The final batch: the victory coronation, the chronicle, the persistent card-table
overlay, and the three card modals. With this, all 55 screens are converted.

## Screens in this batch

| # | Screen | UXML | C# | USS |
|---|--------|------|----|-----|
| 50 | Victory coronation | `Victory.uxml` | `VictoryController` | `EndAndOverlays.uss` |
| 51 | Chronicle | `Chronicle.uxml` | `ChronicleController` | `EndAndOverlays.uss` |
| 52 | Card table | `CardTable.uxml` | `CardTableController` | `EndAndOverlays.uss` |
| 53 | Minor Arcana inspect | `CardModals.uxml` | `CardModalsController` | `EndAndOverlays.uss` |
| 54 | Adept received | `CardModals.uxml` | `CardModalsController` | `EndAndOverlays.uss` |
| 55 | Fate received | `CardModals.uxml` | `CardModalsController` | `EndAndOverlays.uss` |

The three card modals share one UXML (`CardModals.uxml`) as sibling roots;
`CardModalsController.Show(Modal)` reveals exactly one. They are structurally distinct as
designed: **inspect** = read + Done; **Adept** = place/hold choice; **Fate** = fired
effect + single Accept.

## Reached-from

- **Victory** is reached from the winning Temper at Gold (Batch 6, `TemperStoneController`
  with `IsWinningMove`). `chronicle-btn` → Chronicle; `newgame-btn` → front of app.
- **Card table** opens from any in-game header menu (the `menu-btn` in the season scenes,
  already wired to `OnOpenCardTable`).
- **Card modals** open on card tap (inspect) or on drawing a Major Arcana (Adept/Fate).

## NATIVE NOTES in this batch

- **Chronicle race chart** — the one genuinely un-USS-able piece. Render `#chart-host`
  with `generateVisualContent` + `Painter2D`, a charting package, or
  `com.unity.vectorgraphics`. Y-axis = stage names (Lead..Altar); X = ages; **use distinct
  point shapes per series** (circle/square/triangle) for colorblind safety.
- **Victory halo** — gold glow/sprite on `.victory-hero`; serif font for the title.
- **Card table** — build player blocks from game state in C#; show **your** hand as chips; rivals show **hidden placeholders** (`?`) matching hand count only (never rival card faces). Stasis rows show "cannot be opposed"; per-rival action buttons are named `{action}-{rival}`.
- **Tarot art** — a sprite per card on `.tarot`; inspect alignment values computed vs the
  current age.

## CONVERSION COMPLETE

All 55 screens (flow-map #1–55) are now converted to UXML + C# controllers, sharing the
`Kismeta.uss` base plus per-area stylesheets. See `MANIFEST.md` for the full status table
and `README.md` for the project-level setup and the standing caveats (validated as
well-formed XML + balanced C#, not yet compiled against Unity's UXML schema).

### Stylesheet inventory
`Kismeta.uss` (base tokens + components) · `TitleScreen.uss` · `SeasonIntro.uss` ·
`AgeCeremony.uss` · `SpringSteps.uss` · `SummerActions.uss` · `Contests.uss` ·
`AutumnActions.uss` · `EndAndOverlays.uss`

### Controller inventory (by area)
`ScreenController` (base) · `TitleScreenController` · `ShellControllers` (setup sheet,
join, codex) · `SetupController` · `CeremonyControllers` (round-open, age open/close,
season intros) · `SpringStepControllers` (roll/harvest, tap-swap, spring hub) ·
`WinterStepControllers` (wager, limits, winter hub) · `SummerActionControllers` (sheets,
craft, activate, build, wards, end-summer, summer scene) · `ContestControllers` (duel,
gambit, opposition, trade) · `AutumnActionControllers` (fire, temper, manage, leave-stasis,
autumn scene) · `EndAndOverlayControllers` (victory, chronicle, card table, card modals).
