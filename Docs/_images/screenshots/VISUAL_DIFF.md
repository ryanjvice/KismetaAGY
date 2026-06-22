# Priority Visual Diffs

Side-by-side comparison of **shipped screenshots** vs **wireframe mockups** for the four highest-traffic screens that exist in both sets. Use these notes to guide the next UI polish pass.

Composite images (wireframe left, screenshot right): [`composites/`](composites/)

Reference: [UI_styleGuide.md](../../wireframes/UI_styleGuide.md)

---

## 1. Commune (Spring)

| | Wireframe | Screenshot |
|---|-----------|------------|
| **Files** | [`spring_communeArrange.png`](../../wireframes/wireframeImgs/spring_communeArrange.png) | [`spring_commune.png`](spring_commune.png) |
| **Composite** | [`composites/diff_commune.png`](composites/diff_commune.png) | |

### Layout — match

- Header with season icon, title, subtitle
- Three-column stats bar (spread alignment / safe in hand / verdict)
- Spread zone (green) and Hand zone (purple) with card counts
- Instruction tip box
- Primary CTA at bottom

### Gaps to close

| Area | Wireframe | Screenshot | Fix |
|------|-----------|------------|-----|
| **Card chips** | Full rank + suit glyph + alignment bubble + planet chip | Text label ("Nine", "King") + tiny placeholder icon | Wire `CardChipFactory` / `SymbolGlyphs`; apply suit colors from style guide §2.2 |
| **Sigil** | Green flower/zodiac season icon | Square-in-circle placeholder | Replace with `ti-flower` / season sigil from Spring palette |
| **Typography** | Distinct display vs body sizing | Generic sans-serif | Apply `.font-display` / `.font-heading` / `--font-*` tokens |
| **Verdict chip** | Color-coded verdict badge | Plain text "balanced" | Style `.verdict--balanced` semantic token |
| **CTA copy** | "Lock the tableau · to Summer" | Same — OK | — |
| **Viewport fill** | Cards and zones use ~70% of frame | Bottom ~30% empty | Expand spread/hand min-heights; scale card row |

---

## 2. Craft a reagent

| | Wireframe | Screenshot |
|---|-----------|------------|
| **Files** | [`craftReagent.png`](../../wireframes/wireframeImgs/craftReagent.png) | [`summer_craftReagent.png`](summer_craftReagent.png) |
| **Composite** | [`composites/diff_craftReagent.png`](composites/diff_craftReagent.png) | |

### Layout — partial match

- Back arrow + title + subtitle
- Reagent picker (Salt + four elementals)
- Card selection row with progress counter
- Summary bar + forge button

### Gaps to close

| Area | Wireframe | Screenshot | Fix |
|------|-----------|------------|-----|
| **Reagent dots** | Colored elemental dots per style guide §2.3 | Colored circles — close | Verify hex values match tokens |
| **Card row** | 6+ full card faces, suit-colored fills | 6 mini cards with rank text only | Use standard card chip component at selection size |
| **Selection highlight** | Gold border + check overlay | Yellow border — OK | — |
| **Cauldron hint** | Inline cauldron color badge (Red for Wands) | Text only "lit Red cauldron" | Add cauldron icon + `--suit-wands` color swatch |
| **Forge button** | Enabled/disabled states with clear copy | Disabled purple "needs 1 more card" | Match wireframe `--semantic-caution` disabled styling |
| **Viewport fill** | Card row + reagent list fill frame | Top-heavy; large empty bottom | Anchor card row mid-screen |

---

## 3. Age opening / reveal

| | Wireframe | Screenshot |
|---|-----------|------------|
| **Files** | [`newAge_start.png`](../../wireframes/wireframeImgs/newAge_start.png) | [`ageOpen_reveal_taurus.png`](ageOpen_reveal_taurus.png) |
| **Composite** | [`composites/diff_ageOpening.png`](composites/diff_ageOpening.png) | |

Also compare pre-cast die screen: [`ageOpen_castAgeDie.png`](ageOpen_castAgeDie.png)

### Layout — structural match

- Ceremonial header ("the heavens turn")
- Large zodiac emblem
- Age name + planet/element tags
- Cosmic effect card
- "This age favors" list
- Agekeeper banner + CTA

### Gaps to close

| Area | Wireframe | Screenshot | Fix |
|------|-----------|------------|-----|
| **Zodiac art** | Illustrated sign glyph in circle | White square placeholder in maroon circle | `SymbolGlyphs` zodiac emoji or sign art asset |
| **Planet/element tags** | Styled pills with icons | Text-only pills — close | Add planet/element mini-icons |
| **Cosmic effect icon** | Themed icon per effect type | Purple square placeholder | Effect-specific icon from board art set |
| **Favors list icons** | Small themed icons per bullet | Square placeholders | Harvest/alignment/reagent icons |
| **Typography** | Display sizing on age name, section heads | Sans-serif throughout | Apply `.font-display` on age name, `.font-heading` on sections |
| **Frame break** | Full-bleed ceremonial tableau, no season chrome | Correct — ceremonial screen | — |
| **Cast die step** | Rich die animation area | `?` placeholder + sparse bottom half | Match wireframe die art; fill viewport |

---

## 4. Fateful Wager

| | Wireframe | Screenshot |
|---|-----------|------------|
| **Files** | [`winter_fatefulWager.png`](../../wireframes/wireframeImgs/winter_fatefulWager.png) | [`winter_fatefulWager.png`](winter_fatefulWager.png) |
| **Composite** | [`composites/diff_fatefulWager.png`](composites/diff_fatefulWager.png) | |

### Layout — strong match

- Header + optional subtitle
- 3×4 zodiac sign grid
- Stake-from-hand card area
- Instruction box
- Skip / Place wager buttons

### Gaps to close

| Area | Wireframe | Screenshot | Fix |
|------|-----------|------------|-----|
| **Zodiac grid** | Gold Unicode/emoji glyphs (♈♉…) | Gold symbol font in buttons — close | Verify `SymbolGlyphs` at grid size |
| **Selected sign** | Bright glow border on active sign | Purple border on Pisces — OK | Align glow color to `--gold-bright` |
| **Stake cards** | Tap-to-select spread + hand cards | Single spread card shown | Show both zones; multi-select like wireframe select screen |
| **Card chip** | Full card face in stake row | Red "Seven" mini card | Standard card chip |
| **Place wager button** | Disabled until sign + stake chosen | Greyed "Place wager" — OK | — |
| **Winter palette** | Blue accent `#6fa8d4` on borders | Purple accent dominates | Rebalance toward Winter tokens §2.4 |

---

## Cross-cutting fixes (all four screens)

1. **Placeholder iconography** — replace square-in-circle sigils with season icons, zodiac glyphs, and Tabler/suit icons
2. **Card chip component** — unify all card surfaces through `CardChipFactory` with suit fill colors and rank typography
3. **Typography tokens** — use `--font-*` size tokens and semantic USS classes (`.font-display`, `.font-heading`, `.font-emoji`)
4. **Viewport density** — reduce empty lower-half dead space; wireframes target 380×844 full bleed
5. **Semantic color tokens** — wire `--gold`, `--muted`, season accents, and success/caution boxes from `Kismeta.uss`

---

## Recommended polish order

1. **Commune** — highest visibility; card chips unlock Trade, Card table, and Winter unlock reuse
2. **Age opening** — first impression; ceremonial typography + zodiac art
3. **Craft reagent** — template for all Summer action modals
4. **Fateful Wager** — Winter palette + zodiac grid validates emoji/font pipeline
