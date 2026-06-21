# Gap Screenshot Capture Checklist

Screens listed here have wireframe mockups but **no baseline screenshot** in the main catalog yet. After capture, move files from `gaps/` to the parent folder and add a row to [CATALOG.md](../CATALOG.md).

## Automated capture (Play Mode)

1. Open **Bootstrap** scene and enter **Play Mode**
2. Start a game (New game → Begin the Great Year) and play until mid-round, or resume an in-progress session
3. Run **Kismeta → UI → Capture Missing Screenshots**
4. Output lands in this folder (`gaps/`)

The menu item navigates via `ScreenRouter` and overlay hosts. Screens that need specific game state (harvest results, duel/gambit mid-wizard, opposition tally) may still need manual capture — see below.

---

## Spring (3)

| Target file | Wireframe | How to reach | Code screen |
|-------------|-----------|--------------|-------------|
| `spring_hub_roll.png` | [`spring_rollZodiac.png`](../../../wireframes/wireframeImgs/spring_rollZodiac.png) | Age open → Spring intro → Begin Spring → roll zodiac step on hub | `SpringHub` |
| `spring_harvest.png` | [`spring_harvest.png`](../../../wireframes/wireframeImgs/spring_harvest.png) | Complete zodiac roll → harvest tally step | `SpringHub` |
| `spring_hub_postHarvest.png` | — (flow map #13) | After harvest, before Commune CTA on hub | `SpringHub` |

---

## Summer (6)

| Target file | Wireframe | How to reach |
|-------------|-----------|--------------|
| `summer_consortSheet.png` | [`summerPhase_consort.png`](../../../wireframes/wireframeImgs/summerPhase_consort.png) | Summer main → Consort button |
| `summer_craftReagent_result.png` | [`craftReagent_result.png`](../../../wireframes/wireframeImgs/craftReagent_result.png) | Craft reagent → forge 3 cards → result state |
| `summer_duel_result.png` | [`duel_result.png`](../../../wireframes/wireframeImgs/duel_result.png) | Duel → rival → ante → roll → resolution |
| `summer_gambit_ante.png` | [`gambit_ante.png`](../../../wireframes/wireframeImgs/gambit_ante.png) | Gambit → rival → ward fee step |
| `summer_gambit_matchWard.png` | [`gambit_matchWard.png`](../../../wireframes/wireframeImgs/gambit_matchWard.png) | Gambit → pay ward fee → stake step |
| `summer_gambit_roll.png` | [`gambit_rollDice.png`](../../../wireframes/wireframeImgs/gambit_rollDice.png) | Gambit → stake card → roll step |
| `summer_gambit_result.png` | [`gambit_result.png`](../../../wireframes/wireframeImgs/gambit_result.png) | Gambit → roll → resolution |

---

## Autumn (7)

| Target file | Wireframe | How to reach |
|-------------|-----------|--------------|
| `autumn_fireStone.png` | — | Autumn main → Fire (stone at Mantle, active crucible slot) |
| `autumn_temperStone.png` | — | Autumn main → Temper (stone Forging) |
| `autumn_opposition_start.png` | [`opposition_start.png`](../../../wireframes/wireframeImgs/opposition_start.png) | Autumn main → Oppose → choose forging rival |
| `autumn_opposition_tally.png` | [`opposition_tally.png`](../../../wireframes/wireframeImgs/opposition_tally.png) | Opposition → roll → tally step |
| `autumn_opposition_result.png` | [`opposition_result.png`](../../../wireframes/wireframeImgs/opposition_result.png) | Opposition → result |
| `autumn_opposition_stasis.png` | [`opposition_sendToStasis.png`](../../../wireframes/wireframeImgs/opposition_sendToStasis.png) | Opposition win → send to stasis |
| `autumn_leaveStasis.png` | [`leaveStasis.png`](../../../wireframes/wireframeImgs/leaveStasis.png) | Autumn main → Leave Stasis (defender in stasis) |
| `autumn_leaveStasis_end.png` | [`leaveStasis_end.png`](../../../wireframes/wireframeImgs/leaveStasis_end.png) | Leave Stasis → resolved |

---

## Winter & game end (5)

| Target file | Wireframe | How to reach | Code screen |
|-------------|-----------|--------------|-------------|
| `winter_cardLimits.png` | [`winter_cardLimits.png`](../../../wireframes/wireframeImgs/winter_cardLimits.png) | Winter hub → over spread/hand limit | `CardLimits` |
| `winter_end.png` | [`winter_end.png`](../../../wireframes/wireframeImgs/winter_end.png) | Complete Winter steps → age-close | `AgeClosing` |
| `ageOpen_wagerResult.png` | [`newAge_fatefulWager_result.png`](../../../wireframes/wireframeImgs/newAge_fatefulWager_result.png) | Age open with pending wager resolving | `AgeOpening` |
| `end_victory.png` | [`winGame.png`](../../../wireframes/wireframeImgs/winGame.png) | Win via final Temper at Gold | `Victory` |
| `end_chronicle.png` | [`chronicleGreatYear.png`](../../../wireframes/wireframeImgs/chronicleGreatYear.png) | Victory → Chronicle | `Chronicle` |

---

## Status

| File | Status |
|------|--------|
| `end_chronicle.png` | Captured (automated menu, Play Mode) |
| All other rows above | **Pending** — run **Kismeta → UI → Capture Missing Screenshots** mid-game, or capture manually per "How to reach" |

After each capture, move completed PNGs to the parent folder, rename if needed, and add a row to [CATALOG.md](../CATALOG.md).
