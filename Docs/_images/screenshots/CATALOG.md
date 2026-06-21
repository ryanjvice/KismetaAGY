# Screenshot Catalog

Semantic filenames for all captured game screens in this folder. Each row maps a screenshot to its **code screen** ([`ScreenIds`](../../Assets/_Project/UI/Scripts/Framework/ScreenIds.cs)), the closest **wireframe mockup**, and notes for visual comparison.

Companion docs:

- [Gap capture checklist](gaps/CAPTURE_GAPS.md) — screens not yet captured
- [Priority visual diffs](VISUAL_DIFF.md) — side-by-side analysis vs wireframes
- [Wireframe index](../../wireframes/wireframeImgs/README.md)
- [Flow map](../../wireframes/wireframe_flowMap.md)

Captured **2026-06-21** · **40 files** · **~31 unique UI states**

---

## Shell / meta

| File | Scene | Code screen | Wireframe | Notes |
|------|-------|-------------|-----------|-------|
| [`shell_title.png`](shell_title.png) | Title screen | `Title` | — | Ceremonial shell; no wireframe PNG |
| [`shell_resume.png`](shell_resume.png) | Resume (empty list) | `Resume` | — | Flow map #4 |
| [`shell_join.png`](shell_join.png) | Join a game | `Join` | — | Flow map #4 |
| [`shell_codex_terms.png`](shell_codex_terms.png) | Codex — Terms tab | `Codex` | — | Built; not in wireframeImgs |
| [`shell_codex_cards.png`](shell_codex_cards.png) | Codex — Cards tab | `Codex` | — | Same screen, different tab |
| [`shell_setup_sheet.png`](shell_setup_sheet.png) | New game setup (compact sheet) | `SetupSheet` | — (partial) | Wireframes describe fuller dedicated setup |

---

## Age-open ceremony

| File | Scene | Code screen | Wireframe | Notes |
|------|-------|-------------|-----------|-------|
| [`ageOpen_agekeeper_preroll.png`](ageOpen_agekeeper_preroll.png) | Determine Agekeeper — pre-roll | `AgekeeperContest` | — | Flow map #7 |
| [`ageOpen_agekeeper_result.png`](ageOpen_agekeeper_result.png) | Determine Agekeeper — results | `AgekeeperContest` | — | Post-roll state |
| [`ageOpen_castAgeDie.png`](ageOpen_castAgeDie.png) | Cast the age die | `RoundOpen` | [`newAge_start.png`](../../wireframes/wireframeImgs/newAge_start.png) | Pre-cast; placeholder die art |
| [`ageOpen_reveal_taurus.png`](ageOpen_reveal_taurus.png) | Age reveal — Taurus | `AgeOpening` | [`newAge_start.png`](../../wireframes/wireframeImgs/newAge_start.png) | First age of session |
| [`ageOpen_reveal_libra.png`](ageOpen_reveal_libra.png) | Age reveal — Libra | `AgeOpening` | [`newAge_start.png`](../../wireframes/wireframeImgs/newAge_start.png) | Next age after transit |

---

## Spring

| File | Scene | Code screen | Wireframe | Notes |
|------|-------|-------------|-----------|-------|
| [`spring_intro.png`](spring_intro.png) | Spring intro (five steps) | `SpringIntro` | [`springPhase.png`](../../wireframes/wireframeImgs/springPhase.png) | Step checklist, not in-play hub |
| [`overlay_adeptReceived.png`](overlay_adeptReceived.png) | Adept received modal | overlay | — | Flow map #54 |
| [`spring_commune.png`](spring_commune.png) | Commune (Spring) | `Commune` | [`spring_communeArrange.png`](../../wireframes/wireframeImgs/spring_communeArrange.png) | Strong structural match |

**Not yet captured:** roll zodiac (`SpringHub`), harvest tally, post-harvest hub → see [gaps/CAPTURE_GAPS.md](gaps/CAPTURE_GAPS.md)

---

## Summer

| File | Scene | Code screen | Wireframe | Notes |
|------|-------|-------------|-----------|-------|
| [`summer_intro.png`](summer_intro.png) | Summer intro | `SummerIntro` | [`seasonTransit_summer.png`](../../wireframes/wireframeImgs/seasonTransit_summer.png) (partial) | Action checklist |
| [`summer_main_activateCrucible.png`](summer_main_activateCrucible.png) | Summer main — ActivateCrucible | `SummerMain` | [`summerPhase_main.png`](../../wireframes/wireframeImgs/summerPhase_main.png) | Substate label visible |
| [`summer_craftBuildSheet.png`](summer_craftBuildSheet.png) | Craft & build sheet | overlay | [`summerPhase_craft.png`](../../wireframes/wireframeImgs/summerPhase_craft.png) | Strong match |
| [`summer_craftReagent.png`](summer_craftReagent.png) | Craft a reagent | overlay | [`craftReagent.png`](../../wireframes/wireframeImgs/craftReagent.png) | See [VISUAL_DIFF.md](VISUAL_DIFF.md) |
| [`summer_lightCauldron.png`](summer_lightCauldron.png) | Light cauldron explainer | overlay | [`lightCauldron.png`](../../wireframes/wireframeImgs/lightCauldron.png) | Very sparse vs wireframe |
| [`summer_buildAstralHouse.png`](summer_buildAstralHouse.png) | Build Astral House | overlay | [`buildAstralHouse.png`](../../wireframes/wireframeImgs/buildAstralHouse.png) | Good match |
| [`summer_placeWards.png`](summer_placeWards.png) | Place wards (empty) | overlay | [`placeWards.png`](../../wireframes/wireframeImgs/placeWards.png) | Empty state |
| [`summer_trade.png`](summer_trade.png) | Trade · rival | overlay | [`table_overview.png`](../../wireframes/wireframeImgs/table_overview.png) (trade) | Thinner than wireframe |
| [`summer_duel_rivalSelect.png`](summer_duel_rivalSelect.png) | Duel — choose rival | overlay | [`duel_target.png`](../../wireframes/wireframeImgs/duel_target.png) | Step 1 of 3 |
| [`summer_duel_ante.png`](summer_duel_ante.png) | Duel — ante | overlay | [`duel_startAnte.png`](../../wireframes/wireframeImgs/duel_startAnte.png) | Strong match |
| [`summer_duel_roll.png`](summer_duel_roll.png) | Duel — roll | overlay | [`duel_roll.png`](../../wireframes/wireframeImgs/duel_roll.png) | Pre-roll |
| [`summer_gambit_rivalSelect.png`](summer_gambit_rivalSelect.png) | Gambit — choose rival | overlay | [`gambit_start.png`](../../wireframes/wireframeImgs/gambit_start.png) | Step 1 of 4 |
| [`summer_activateCrucibleCard.png`](summer_activateCrucibleCard.png) | Activate Crucible card | overlay | [`activateCrucibleCard.png`](../../wireframes/wireframeImgs/activateCrucibleCard.png) | Good match |
| [`summer_endSummer.png`](summer_endSummer.png) | End Summer CTA | overlay | — | Flow map #27 |

**Card table (same overlay, scroll positions):**

| File | Wireframe |
|------|-----------|
| [`overlay_cardTable_top.png`](overlay_cardTable_top.png) | [`fullTable_mockup_1_START_labels.png`](../../wireframes/wireframeImgs/fullTable_mockup_1_START_labels.png) |
| [`overlay_cardTable_scroll1.png`](overlay_cardTable_scroll1.png) | same |
| [`overlay_cardTable_scroll2.png`](overlay_cardTable_scroll2.png) | same |

---

## Autumn

| File | Scene | Code screen | Wireframe | Notes |
|------|-------|-------------|-----------|-------|
| [`autumn_intro.png`](autumn_intro.png) | Autumn intro | `AutumnIntro` | [`autumn_start.png`](../../wireframes/wireframeImgs/autumn_start.png) | Strong match |
| [`autumn_main_surveyCrucible.png`](autumn_main_surveyCrucible.png) | Autumn main — SurveyCrucible | `AutumnMain` | — | Minimal central spiral art |
| [`autumn_manageTableau.png`](autumn_manageTableau.png) | Your tableau (Manage cards) | overlay | — | Flow map #30 |
| [`autumn_endAutumn.png`](autumn_endAutumn.png) | End Autumn CTA | overlay | — | Flow map #36 |

---

## Winter & age transit

| File | Scene | Code screen | Wireframe | Notes |
|------|-------|-------------|-----------|-------|
| [`winter_intro.png`](winter_intro.png) | Winter intro | `WinterIntro` | — (partial) | Step list only |
| [`winter_hub_cardUnlock.png`](winter_hub_cardUnlock.png) | Winter hub — CardUnlock | `WinterHub` | [`winter_cardUnlock.png`](../../wireframes/wireframeImgs/winter_cardUnlock.png) | Hub + action bar |
| [`winter_unlock_commune.png`](winter_unlock_commune.png) | Winter unlock (Commune UX) | `WinterUnlock` | [`spring_communeArrange.png`](../../wireframes/wireframeImgs/spring_communeArrange.png) (layout) | Different subtitle/CTA |
| [`winter_fatefulWager.png`](winter_fatefulWager.png) | Fateful Wager | `FatefulWager` | [`winter_fatefulWager.png`](../../wireframes/wireframeImgs/winter_fatefulWager.png) | See [VISUAL_DIFF.md](VISUAL_DIFF.md) |
| [`winter_transitAge_recap.png`](winter_transitAge_recap.png) | Transit age recap | transit step | [`newAge_result.png`](../../wireframes/wireframeImgs/newAge_result.png) | Pairs with `ageOpen_reveal_libra.png` |

---

## Coverage snapshot

| Region | Wireframe PNGs | Captured | Coverage |
|--------|----------------|----------|----------|
| Shell / meta | 0 | 6 | Good (no wireframe baselines) |
| Age open | 2 | 5 | Good ceremony chain |
| Spring | 7 | 3 | **Low** — missing roll, harvest, hub |
| Summer | 14 | 17 | **Good** — missing consort sheet, results, gambit mid-flow |
| Combat | 9 | 4 | **Partial** |
| Autumn | 8 | 4 | **Low** — missing forge modals, opposition |
| Winter / transit | 8 | 5 | **Medium** — missing limits, age-close |
| Game end | 2 | 1 (in `gaps/`) | **Partial** — `end_chronicle.png` captured; victory pending |

---

## Naming convention

`{region}_{screen}[_{variant}].png`

- **region:** `shell`, `ageOpen`, `spring`, `summer`, `autumn`, `winter`, `overlay`
- **variant:** scroll position, wizard step, age sign, or substate label when needed
