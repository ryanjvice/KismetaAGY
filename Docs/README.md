# Kismeta: Alchemists of the Great Year — Design Docs

Design and rules documentation for **Great Year**, the Unity mobile game based on *Kismeta: Alchemists of the Great Year* by Goodmagik Games. This folder is the authoritative reference for game rules, card data, lore, and UI mockups used during development.

> **Editing rules/card docs:** Edit `_source/Kismeta_GameGuide.md` or `_source/Kismeta_CardReference.md`, then run `npm run docs:sync` from the repo root. See [MAINTENANCE.md](MAINTENANCE.md) for the full workflow.

---

## Quick Start

| You want to… | Start here |
| ------------ | ---------- |
| Understand the game | [Introduction](Rules/introduction.md) → [Game Overview](Rules/overview.md) |
| Set up a game | [Setup](Rules/setup.md) |
| Follow a round step by step | [Round Overview](Rules/round-overview.md) |
| Look up a card's effect | [Cards →](Cards/README.md) |
| Decode a game term | [Glossary](Glossary.md) |
| Build the mobile UI (uGUI) | [UI.md](UI.md) |
| Understand project structure | [ARCHITECTURE.md](ARCHITECTURE.md) |

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

UI mockups and board art used to guide the Great Year mobile implementation. Primary reference: [`_images/mockups/`](_images/mockups/README.md).

| Location | Contents |
| -------- | -------- |
| [`_images/mockups/`](_images/mockups/README.md) | Screen flows for all phases (Spring–Winter), combat, table layout |
| [`_images/board/`](_images/board/) | Great Year board art asset |

> Images are tracked by Git LFS. See [`_images/README.md`](_images/README.md) for folder conventions.

---

## Developer / Technical Reference

Documentation for building the Great Year Unity mobile game. UI uses **uGUI** (not UI Toolkit).

| Document | Contents |
| -------- | -------- |
| [UI.md](UI.md) | uGUI conventions, main table zone map, mockup screen catalog linked to rules |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Project layout, game/UI layering, mobile defaults, data sources |

---

## Source & tooling

| Path | Role |
| ---- | ---- |
| [`_source/Kismeta_GameGuide.md`](_source/Kismeta_GameGuide.md) | Canonical rules — you edit this |
| [`_source/Kismeta_CardReference.md`](_source/Kismeta_CardReference.md) | Canonical card data — you edit this |
| [`docs-split.manifest.json`](docs-split.manifest.json) | Maps source headings → output files |
| [`MAINTENANCE.md`](MAINTENANCE.md) | Full editing and sync workflow |
| `npm run docs:sync` | Regenerates split rule/card files from source |
