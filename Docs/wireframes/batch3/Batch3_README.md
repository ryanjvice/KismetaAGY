# Batch 3 — Spring &amp; Winter steps (flow-map #11–15, 38–44)

The step screens of the two sequenced seasons, plus the back-filled C# controllers for
the Spring and Winter hubs built earlier. This batch introduces the **tap-to-swap**
pattern (shared by Commune and Unlock) and the **wager grid**.

## Screens in this batch

| # | Screen | UXML | C# | USS |
|---|--------|------|----|-----|
| 11–12 | Roll your sign + harvest tally | `SpringRollHarvest.uxml` | `SpringRollHarvestController` | `SpringSteps.uss` |
| 13 | Post-harvest hub (back-fill C#) | *(prebuilt)* | `SpringHubController` | — |
| 14 | Commune (tap-to-swap) | `Commune.uxml` | `TapSwapController` | `SpringSteps.uss` |
| 15 | Card Lock → Summer | *(reuse `TransitAge` w/ swapped copy)* | — | — |
| 38 | Winter hub — post-unlock | *(prebuilt `WinterHub`)* | `WinterHubController` | — |
| 39 | Unlock (tap-to-swap) | `WinterUnlock.uxml` | `TapSwapController` | `SpringSteps.uss` |
| 40 | Fateful Wager | `FatefulWager.uxml` | `FatefulWagerController` | `SpringSteps.uss` |
| 41 | Winter hub — post-wager (back-fill C#) | *(prebuilt `WinterHub`)* | `WinterHubController` | — |
| 42 | Card limits | `CardLimits.uxml` | `CardLimitsController` | `SpringSteps.uss` |
| 43 | Winter hub — post-limits | *(prebuilt `WinterHub`)* | `WinterHubController` | — |
| 44 | Transit the age | `TransitAge.uxml` | *(simple; wire `transit-btn`)* | — |

## Key patterns introduced

**Tap-to-swap (`TapSwapController`).** One controller serves both Commune (#14) and
Unlock (#39) — they're the same interaction in different seasons. It holds two
`List<CardVM>` (Spread, Hand), builds chips in C#, and on each tap moves the card's data
between lists and rebuilds both zones + the live readout. `CardVM` is a placeholder card
model — map it to your real card type.

**`WinterUnlock.uxml`** is generated from `Commune.uxml` (same structure, winter palette,
swapped copy/button name) — illustrating that screens sharing a pattern can share UXML
structure too.

**Hub states (#38, 41, 43)** all use the single prebuilt `WinterHub.uxml`; the difference
between post-unlock / post-wager / post-limits is which step is active and which CTA shows
— driven in C# by `WinterHubController` (swap step-dot state classes, set the active CTA).
The same applies to the Spring hub.

**Card limits gate (#42 → #44).** `CardLimitsController.Refresh(handCount)` toggles the
transit button between `btn--disabled` ("discard N more") and `btn--primary`
("transit the age") based on whether the hand is legal.

## NATIVE NOTES in this batch

- **Zodiac wheel** (`SpringRollHarvest`) — animate `#wheel-host`; reveal the tally on settle.
- **Tap-to-swap chips** — built from game state in C#; the static chips in UXML are layout
  samples. Clear `#spread-cards`/`#hand-cards` and repopulate at runtime.
- **Wager sign grid** — 12 `.sign-cell` buttons (authored or instantiated); selection via
  `sign-cell--selected`.
- **Over-limit flag** — `card-chip--overlimit` (amber edge) on the cards beyond the limit.

## Status

#11–15, 38–44 layout-complete with controllers; Spring/Winter hub C# back-filled and the
duplicate hub stubs removed from `MainSceneControllers.cs`. Next: **Batch 4 — Summer
actions** (#18–25, 27): the craft/build/consort sheets and the action screens, plus the
Summer main-scene C#.
