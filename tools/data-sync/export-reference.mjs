#!/usr/bin/env node
/**
 * export-reference.mjs
 *
 * Emits three static reference JSON files from the canonical docs sources:
 *   - cosmic-ages.json   (12 Zodiac signs with planet, element, cosmic effect)
 *   - correspondence.json (Element ↔ Suit ↔ Reagent ↔ Cauldron colour)
 *   - game-modes.json    (Quickplay / Standard / MagnusAlchemist descriptor stubs)
 *
 * These are hand-transcribed from the docs tables and can be regenerated any time.
 * Edit the data arrays below when docs change, then run: npm run data:sync
 */

import { writeFileSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = join(__dirname, '../..');
const OUT_DIR   = join(REPO_ROOT, 'Assets/_Project/Scripts/Data/Generated');

mkdirSync(OUT_DIR, { recursive: true });

// ── Cosmic Ages ───────────────────────────────────────────────────────────────
// Source: Docs/Reference/cosmic-ages.md
const cosmicAges = [
  { sign: 'Aries',       planet: 'Mars',        element: 'Fire',  effect: '+1 Base Harvest Card each round.' },
  { sign: 'Taurus',      planet: 'Venus',       element: 'Earth', effect: 'Court Cards of Pentacles are a Wild Suit.' },
  { sign: 'Gemini',      planet: 'Mercury',     element: 'Air',   effect: 'Craft Quicksilver with 2 Swords cards (Cauldron must be lit).' },
  { sign: 'Cancer',      planet: 'Sun/Moon',    element: 'Water', effect: 'Craft Salt for any 2 cards.' },
  { sign: 'Leo',         planet: 'Sun/Moon',    element: 'Fire',  effect: 'Court Cards of Wands are a Wild Suit.' },
  { sign: 'Virgo',       planet: 'Mercury',     element: 'Earth', effect: 'Craft Vitriol with 2 Pentacles cards (Cauldron must be lit).' },
  { sign: 'Libra',       planet: 'Venus',       element: 'Air',   effect: '+1 Base Harvest Card each round.' },
  { sign: 'Scorpio',     planet: 'Mars',        element: 'Water', effect: 'Court Cards of Cups are a Wild Suit.' },
  { sign: 'Sagittarius', planet: 'Jupiter',     element: 'Fire',  effect: 'Craft Sulphur with 2 Wands cards (Cauldron must be lit).' },
  { sign: 'Capricorn',   planet: 'Saturn',      element: 'Earth', effect: 'Craft Salt for any 2 cards.' },
  { sign: 'Aquarius',    planet: 'Saturn',      element: 'Air',   effect: 'Court Cards of Swords are a Wild Suit.' },
  { sign: 'Pisces',      planet: 'Jupiter',     element: 'Water', effect: 'Craft Aqua Regia with 2 Cups cards (Cauldron must be lit).' },
];

writeFileSync(join(OUT_DIR, 'cosmic-ages.json'), JSON.stringify(cosmicAges, null, 2));
console.log(`[reference] wrote cosmic-ages.json (${cosmicAges.length} entries)`);

// ── Correspondence ────────────────────────────────────────────────────────────
// Source: Docs/Reference/correspondence.md
const correspondence = [
  { color: 'Red',    element: 'Fire',  suit: 'Wands',     reagent: 'Sulphur',     cauldron: 'Red' },
  { color: 'Blue',   element: 'Water', suit: 'Cups',      reagent: 'AquaRegia',   cauldron: 'Blue' },
  { color: 'Green',  element: 'Earth', suit: 'Pentacles', reagent: 'Vitriol',     cauldron: 'Green' },
  { color: 'Yellow', element: 'Air',   suit: 'Swords',    reagent: 'Quicksilver', cauldron: 'Yellow' },
];

writeFileSync(join(OUT_DIR, 'correspondence.json'), JSON.stringify(correspondence, null, 2));
console.log(`[reference] wrote correspondence.json (${correspondence.length} entries)`);

// ── Game Modes ────────────────────────────────────────────────────────────────
// Source: Docs/Rules/setup.md
const gameModes = [
  {
    id: 'Quickplay',
    displayName: 'Quickplay / First Play',
    description: 'Recommended for first playthroughs.',
    astralHouseCost: 'Pay 1 Kismeta Card whose Planet matches your current Zodiac Sign.',
    craftingRule: 'Discard 3 matching-Suit cards into a lit Cauldron to craft 1 Reagent. No Cauldron modifier.',
    crucibleBuild: 'Quickplay build (see Crucible deck table in docs).',
    magnusModifiers: false,
  },
  {
    id: 'Standard',
    displayName: 'Standard Game',
    description: 'Recommended once you are familiar with the rules.',
    astralHouseCost: 'As written in the rules.',
    craftingRule: 'Standard crafting with Cauldron modifiers.',
    crucibleBuild: 'Standard build.',
    magnusModifiers: false,
  },
  {
    id: 'MagnusAlchemist',
    displayName: 'Magnus Alchemist (Mastery)',
    description: 'For those returning from previous Great Years.',
    astralHouseCost: 'As written in the rules.',
    craftingRule: 'Standard crafting with Cauldron modifiers.',
    crucibleBuild: 'Magnus Alchemist build.',
    magnusModifiers: true,
    magnusAlignedRule: 'Aligned players trade freely.',
    magnusMisalignedTradeRule: 'Misaligned players trade 2:1 in favour of the non-initiating player.',
    magnusMisalignedCombatBonus: '+1 dice roll or +1 Alignment Points when confronting a Misaligned opponent.',
  },
];

writeFileSync(join(OUT_DIR, 'game-modes.json'), JSON.stringify(gameModes, null, 2));
console.log(`[reference] wrote game-modes.json (${gameModes.length} entries)`);
