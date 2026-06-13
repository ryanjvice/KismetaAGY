# Kismeta: Alchemists of the Great Year — Design Docs

Design and rules documentation for the Unity mobile port of **Kismeta: Alchemists of the Great Year**, a strategic card-and-dice game by Goodmagik. This folder is the authoritative reference for all game rules, card data, and lore used during development.

> **Editing these docs:** Edit `_source/Kismeta_GameGuide.md` or `_source/Kismeta_CardReference.md`, then run `npm run docs:sync` from the repo root. See [MAINTENANCE.md](MAINTENANCE.md) for the full workflow.

---

## Quick Start

| You want to… | Start here |
|--------------|-----------|
| Understand the game | [Introduction](Rules/introduction.md) → [Game Overview](Rules/overview.md) |
| Set up a game | [Setup](Rules/setup.md) |
| Follow a round step by step | [Round Overview](Rules/round-overview.md) |
| Look up a card's effect | [Cards →](Cards/README.md) |
| Decode a game term | [Glossary](Glossary.md) |

---

## Rules

- [Introduction](Rules/introduction.md) — What Kismeta is, players, play time
- [Components](Rules/components.md) — Everything in the box
- [Game Overview](Rules/overview.md) — The Great Year, The Great Work, The Altar
- [Setup](Rules/setup.md) — Steps I–VI; all three game modes
- [Round Overview](Rules/round-overview.md) — Round at a Glance reference table

### Phases (each round)

- [Phase 1: Spring](Rules/phases/spring.md) — Set the Cosmic Age, roll Zodiac dice, Harvest, Commune, Card Lock
- [Phase 2: Summer](Rules/phases/summer.md) — Activate Crucible Cards, build Astral Houses, craft Reagents, Trade, Duel, Gambit
- [Phase 3: Autumn](Rules/phases/autumn.md) — Survey the Crucible, Opposition, Fire the Stone, Temper, Stasis
- [Phase 4: Winter](Rules/phases/winter.md) — Card Unlock, Fateful Wager, enforce limits, Transit the Age

### End-game & Strategy

- [Winning the Game](Rules/winning.md)
- [Quick Tips](Rules/quick-tips.md)
- [Strategy Guide](Rules/strategy-guide.md)

---

## Cards

Full card reference for all 156 cards (134 Kismeta + 22 Crucible).

- [Card Reference Index](Cards/README.md)
- **Major Arcana:** [Overview](Cards/major-arcana/overview.md) · [Fate Cards](Cards/major-arcana/fates.md) · [Adept Cards](Cards/major-arcana/adepts.md)
- **Crucible Deck:** [Overview](Cards/crucible/overview.md) · [Group A (Beginner)](Cards/crucible/group-a.md) · [Group B (Standard)](Cards/crucible/group-b.md) · [Group C (Advanced)](Cards/crucible/group-c.md) · [Group D (Mastery)](Cards/crucible/group-d.md)
- **Minor Arcana:** [Overview](Cards/minor-arcana/overview.md) · [Cups](Cards/minor-arcana/cups.md) · [Pentacles](Cards/minor-arcana/pentacles.md) · [Swords](Cards/minor-arcana/swords.md) · [Wands](Cards/minor-arcana/wands.md)

---

## Reference

Quick-lookup tables for use during development and play.

- [Cosmic Ages & Effects](Reference/cosmic-ages.md) — All 12 Zodiac signs, planets, elements, and Cosmic Effects
- [Correspondence](Reference/correspondence.md) — Element ↔ Suit ↔ Reagent ↔ Cauldron ↔ Color
- [Kismeta Cards Reference](Reference/major-arcana-rules.md) — Minor & Major Arcana card type rules
- [Card Zones: Spread, Hand & Arcanum](Reference/card-zones.md) — Zone rules, limits, and permitted uses

---

## Glossary

- [Glossary of Terms](Glossary.md) — ~55 terms, alphabetical, with definitions

---

## Lore

- [Prologue: Alchemists of the Great Year](Lore/prologue.md)
- [Epilogue: The Veil Stirs…](Lore/epilogue.md)

---

## Reference Images

Photography and visual reference of the physical game — 4-player table shots across all phases, component close-ups, board overheads, and card scans. Used to guide mobile UI implementation.

- [Image index & naming conventions](_images/README.md)

| Subfolder | Contents |
|-----------|---------|
| [`_images/gameplay/spring/`](_images/gameplay/spring/) | Phase 1 — dice rolls, Harvest, card layout |
| [`_images/gameplay/summer/`](_images/gameplay/summer/) | Phase 2 — Crafting, Duels, Gambits, Trades |
| [`_images/gameplay/autumn/`](_images/gameplay/autumn/) | Phase 3 — Opposition, Forge, Stasis |
| [`_images/gameplay/winter/`](_images/gameplay/winter/) | Phase 4 — Card Unlock, Wager, reset |
| [`_images/setup/`](_images/setup/) | Pre-game table setup |
| [`_images/components/`](_images/components/) | Dice, tokens, Stones, Meeples |
| [`_images/board/`](_images/board/) | Great Year Board; Zodiac Wheel |
| [`_images/cards/`](_images/cards/) | Kismeta deck and Crucible deck card photography |

> Images are tracked by Git LFS. See [`_images/README.md`](_images/README.md) for the naming convention and how to add new images.

---

## Known discrepancies in source material

These inconsistencies exist in the canonical source files. They are preserved as-is in all generated output and should be reconciled in a dedicated rules pass before implementing game logic.

| Issue | Affected files |
|-------|---------------|
| Base Harvest is **3** in phase rules but **2** in Glossary | [Spring](Rules/phases/spring.md), [Glossary](Glossary.md) |
| Major Arcana count: **12 Adept / 10 Fate** in Components vs **11 / 11** in Glossary | [Components](Rules/components.md), [Glossary](Glossary.md) |
| Court rank naming: **Princess** in Card Reference vs **Page** in Game Guide | [Minor Arcana](Cards/minor-arcana/overview.md), [Spring phase](Rules/phases/spring.md) |

---

## Source & tooling

| Path | Role |
|------|------|
| [`_source/Kismeta_GameGuide.md`](_source/Kismeta_GameGuide.md) | Canonical rules — you edit this |
| [`_source/Kismeta_CardReference.md`](_source/Kismeta_CardReference.md) | Canonical card data — you edit this |
| [`docs-split.manifest.json`](docs-split.manifest.json) | Maps source headings → output files |
| [`MAINTENANCE.md`](MAINTENANCE.md) | Full editing and sync workflow |
| `npm run docs:sync` | Regenerates all split files from source |
