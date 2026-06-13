#!/usr/bin/env node
/**
 * export-cards.mjs
 *
 * Parses Docs/_source/Kismeta_CardReference.md and emits cards.json to
 * Assets/_Project/Scripts/Data/Generated/cards.json.
 *
 * The output schema matches CardDefinition in Kismeta.Core.
 * Expected total: 156 cards (22 Major Arcana + 112 Minor Arcana + 22 Crucible).
 *
 * Run via: npm run data:sync
 */

import { readFileSync, writeFileSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT  = join(__dirname, '../..');
const SOURCE     = join(REPO_ROOT, 'Docs/_source/Kismeta_CardReference.md');
const OUT_DIR    = join(REPO_ROOT, 'Assets/_Project/Scripts/Data/Generated');

mkdirSync(OUT_DIR, { recursive: true });

const text = readFileSync(SOURCE, 'utf8');
const cards = [];

// ─── Fate Cards (10) ──────────────────────────────────────────────────────────
const fateData = [
  { name: 'The Tower',          arcana: 16, effect: 'Turn all Adept cards face down. Players will have to pay 1 salt to refresh.' },
  { name: 'Death',              arcana: 13, effect: 'All Hands are returned to the deck. The Agekeeper shuffles the deck.' },
  { name: 'The Sun',            arcana: 19, effect: 'All players receive one of each Reagent.' },
  { name: 'Judgement',          arcana: 20, effect: 'Draw 1 card for each Cauldron you have lit.' },
  { name: 'Justice',            arcana: 11, effect: 'All Duels and Gambits are resolved via a best-of-3 series until the end of the round.' },
  { name: 'The Moon',           arcana: 18, effect: 'Draw four cards from the top of the Deck. Keep any two. Return the rest to the bottom of the deck.' },
  { name: 'The Fool',           arcana:  0, effect: 'Draw 2 cards. Your Opponents receive 1 Reagent of their choice.' },
  { name: 'Wheel of Fortune',   arcana: 10, effect: 'All players roll their Zodiac Dice. Highest roll gains 2 Salt. Lowest roll discards 1 card.' },
  { name: 'The Hanged Man',     arcana: 12, effect: 'All players pass their Hand cards to the Player to their Left.' },
  { name: 'The Lovers',         arcana:  6, effect: 'Pick 1 other player. They choose both for you, either: Draw 2 cards or Gain 1 Reagent of their choice.' },
];

for (const fd of fateData) {
  cards.push({
    id: `major.fate.${fd.arcana}`,
    deck: 'Kismeta',
    suit: null,
    rank: null,
    cardVariant: null,
    majorArcanaType: 'Fate',
    arcanaNumber: fd.arcana,
    name: fd.name,
    sign: null,
    planet: null,
    effectType: 'Fate',
    effectText: fd.effect,
    wildcardArcanaNumber: -1,
    crucibleGroup: null,
    activationFormula: null,
    alchemicalFormula: null,
    alchemicalCost: null,
  });
}

// ─── Adept Cards (12) ─────────────────────────────────────────────────────────
const adeptData = [
  { name: 'The Magician',       arcana:  1, sign: 'Gemini',      planet: 'Mercury',
    base: 'Ignore Card Lock. Swap cards freely between Hand and Spread.',
    resonant: 'Nullify "Reversed" effects in your Spread.' },
  { name: 'The High Priestess', arcana:  2, sign: 'Pisces',      planet: 'Moon',
    base: 'Draw your Harvest directly from the Deck and +2 cards. Return any two after reviewing all.',
    resonant: 'Expand your maximum Hand limit to 7 cards.' },
  { name: 'The Empress',        arcana:  3, sign: 'Taurus',      planet: 'Venus',
    base: 'Craft One Reagent type 2-for-1. Mark the matching Cauldron with Salt.',
    resonant: 'Apply 2-for-1 to any two Reagent types.' },
  { name: 'The Emperor',        arcana:  4, sign: 'Aries',       planet: 'Mars',
    base: 'Protect any 2 Spread cards from being lost in Duels or Gambits.',
    resonant: 'Protect any 2 cards from any type of attack.' },
  { name: 'The Hierophant',     arcana:  5, sign: 'Scorpio',     planet: 'Mars',
    base: 'Shift your Zodiac Dice roll ±1 Sign on the Zodiac Wheel for Harvest or Opposition.',
    resonant: 'Shift your Zodiac Dice roll ±2. Apply this effect during any Opposition.' },
  { name: 'The Devil',          arcana: 15, sign: 'Capricorn',   planet: 'Saturn',
    base: 'Sacrifice any card to steal any one card from an opponent\'s Spread.',
    resonant: 'Sacrifice this card to Banish an opponent\'s Adept card. Both cards are returned to the deck.' },
  { name: 'The Chariot',        arcana:  7, sign: 'Leo',         planet: 'Sun/Moon',
    base: 'Initiate Duels without an Ante card.',
    resonant: 'Gain a reroll in any duel.' },
  { name: 'Strength',           arcana:  8, sign: 'Sagittarius', planet: 'Jupiter',
    base: '+1 to Duel dice rolls.',
    resonant: '+2 to Duel & Gambit dice rolls.' },
  { name: 'The Hermit',         arcana:  9, sign: 'Virgo',       planet: 'Mercury',
    base: 'Hold up to 3 Adept cards.',
    resonant: 'All elemental Aspect alignments are doubled. (Fire, Air, Water, Earth)' },
  { name: 'Temperance',         arcana: 14, sign: 'Libra',       planet: 'Venus',
    base: 'Craft Salt for any Two Cards.',
    resonant: 'Salt is a Wild Reagent for one other type. Mark the matching Cauldron with Salt.' },
  { name: 'The Star',           arcana: 17, sign: 'Aquarius',    planet: 'Saturn',
    base: 'Anytime you lose a Duel, Gambit or Opposition, draw two cards.',
    resonant: 'Nullify an Adept or Fate card effect in play. Pay 1 Salt to refresh.' },
  { name: 'The World',          arcana: 21, sign: 'Cancer',      planet: 'Sun/Moon',
    base: 'Your Ward Reagents remain with you after successful Transmutations.',
    resonant: 'This card is a Wildcard for any Crucible card. Flip to activate. Refresh for 1 Salt.' },
];

for (const ad of adeptData) {
  cards.push({
    id: `major.adept.${ad.arcana}`,
    deck: 'Kismeta',
    suit: null,
    rank: null,
    cardVariant: null,
    majorArcanaType: 'Adept',
    arcanaNumber: ad.arcana,
    name: ad.name,
    sign: ad.sign,
    planet: ad.planet,
    effectType: 'Adept',
    effectText: ad.base,
    effectTextResonant: ad.resonant,
    wildcardArcanaNumber: -1,
    crucibleGroup: null,
    activationFormula: null,
    alchemicalFormula: null,
    alchemicalCost: null,
  });
}

// ─── Crucible Cards (22) ──────────────────────────────────────────────────────
// Columns: sulphur (SP), aquaRegia (AR), vitriol (V), quicksilver (Q), salt (S)
const crucibleData = [
  // Group A
  { group:'A', name:'The Fool',          arcana: 0,  sp:1, ar:0, v:0, q:0, s:1, formula:'Pair of Swords with the same Rank' },
  { group:'A', name:'The Magician',      arcana: 1,  sp:0, ar:0, v:0, q:1, s:1, formula:'Pair of Mercury planets (5s + Princess)' },
  { group:'A', name:'The High Priestess',arcana: 2,  sp:0, ar:1, v:0, q:0, s:1, formula:'Pair of Moon planets (2s + Queens)' },
  { group:'A', name:'The Empress',       arcana: 3,  sp:0, ar:0, v:1, q:0, s:1, formula:'Pair of Venus planets (4s + 9s)' },
  // Group B
  { group:'B', name:'The Emperor',       arcana: 4,  sp:2, ar:0, v:1, q:0, s:0, formula:'3-card Wands Straight (Any 3 consecutive ranks)' },
  { group:'B', name:'The Hierophant',    arcana: 5,  sp:0, ar:0, v:2, q:0, s:1, formula:'One Venus + Pair of Pentacles (Same Rank)' },
  { group:'B', name:'The Lovers',        arcana: 6,  sp:0, ar:0, v:0, q:2, s:1, formula:'3-card Swords Straight (Any 3 consecutive ranks)' },
  { group:'B', name:'The Chariot',       arcana: 7,  sp:0, ar:2, v:0, q:0, s:1, formula:'3 Moons (2s & Queens)' },
  { group:'B', name:'Strength',          arcana: 8,  sp:2, ar:0, v:0, q:0, s:1, formula:'One Sun + Pair of Wands (Same Rank)' },
  { group:'B', name:'The Hermit',        arcana: 9,  sp:0, ar:0, v:1, q:2, s:0, formula:'3-card Pentacles Straight (Any 3 consecutive ranks)' },
  { group:'B', name:'Wheel of Fortune',  arcana:10,  sp:0, ar:2, v:0, q:0, s:1, formula:'3-card Cups Straight (Any 3 consecutive ranks)' },
  // Group C
  { group:'C', name:'Justice',           arcana:11,  sp:0, ar:0, v:1, q:2, s:0, formula:'Two Pairs — Pentacles and Swords Suits only' },
  { group:'C', name:'The Hanged Man',    arcana:12,  sp:0, ar:3, v:0, q:0, s:0, formula:'Three Cups (any ranks) + one Sun card (Ace)' },
  { group:'C', name:'Death',             arcana:13,  sp:2, ar:1, v:0, q:0, s:0, formula:'Three Mars planets (7s + Knights) + Two Cups' },
  { group:'C', name:'Temperance',        arcana:14,  sp:2, ar:0, v:1, q:0, s:0, formula:'Three Jupiter planets (3s + 8s) + Two Wands' },
  { group:'C', name:'The Devil',         arcana:15,  sp:1, ar:0, v:1, q:0, s:1, formula:'Three Saturn planets (6s, 10s, or Kings) + Two Pentacles' },
  { group:'C', name:'The Tower',         arcana:16,  sp:0, ar:1, v:2, q:0, s:0, formula:'Two Mars planet (7 or Knight) + 3-card Wands straight' },
  { group:'C', name:'The Star',          arcana:17,  sp:0, ar:0, v:0, q:3, s:0, formula:'3-card Swords straight + Two Venus cards (3 or 8)' },
  // Group D
  { group:'D', name:'The Moon',          arcana:18,  sp:0, ar:3, v:0, q:1, s:0, formula:'Four Mercury planets (5s + Princesses) + Two Cups' },
  { group:'D', name:'The Sun',           arcana:19,  sp:3, ar:0, v:1, q:0, s:0, formula:'Four Wands + Two Sun cards (Aces)' },
  { group:'D', name:'Judgement',         arcana:20,  sp:0, ar:0, v:2, q:2, s:0, formula:'Four Swords + Two Venus planets (4s or 9s)' },
  { group:'D', name:'The World',         arcana:21,  sp:1, ar:1, v:1, q:1, s:0, formula:'Four Saturn planets (6s/10s/Kings) + Two Pentacles' },
];

for (const cd of crucibleData) {
  cards.push({
    id: `crucible.${cd.group.toLowerCase()}.${cd.arcana}`,
    deck: 'Crucible',
    suit: null,
    rank: null,
    cardVariant: null,
    majorArcanaType: null,
    arcanaNumber: cd.arcana,
    name: cd.name,
    sign: null,
    planet: null,
    effectType: 'Crucible',
    effectText: null,
    wildcardArcanaNumber: -1,
    crucibleGroup: cd.group,
    activationFormula: cd.formula,
    alchemicalFormula: cd.formula,
    alchemicalCost: { sulphur: cd.sp, aquaRegia: cd.ar, vitriol: cd.v, quicksilver: cd.q, salt: cd.s },
  });
}

// ─── Minor Arcana (112) ───────────────────────────────────────────────────────
// Four suits × 14 ranks × 2 variants = 112 cards.
// Data sourced from Docs/_source/Kismeta_CardReference.md tables.

const minorArcanaData = {
  Cups: {
    element: 'Water',
    planet: 'Moon', // default cauldron planet; individual ranks override below
    card1: [
      { rank:'Ace',      cv:1,  planet:'Sun',     type:'Entry Fee',  effect:'Discard to build an Astral House when your Zodiac die is on a Water sign.' },
      { rank:'Two',      cv:2,  planet:'Moon',    type:'Harvest',    effect:'Astral Houses on Water signs earn double at Harvest.' },
      { rank:'Three',    cv:3,  planet:'Jupiter', type:'Build',      effect:'Craft Salt for any two Cups cards.' },
      { rank:'Four',     cv:4,  planet:'Venus',   type:'Reversed',   effect:'Your Hand limit is reduced by 1 at round end.', nullifies:'5 of Wands', isCurse:true },
      { rank:'Five',     cv:5,  planet:'Mercury', type:'Reversed',   effect:'Your opponent gains a Reroll in Duels against you.', nullifies:'6 of Wands', isCurse:true },
      { rank:'Six',      cv:6,  planet:'Saturn',  type:'Reversed',   effect:'Your Opponent Chooses Your Ante Card.', nullifies:'4 of Wands', isCurse:true },
      { rank:'Seven',    cv:7,  planet:'Mars',    type:'Forge',      effect:'When you Fire the Stone, gain 1 Aqua Regia.' },
      { rank:'Eight',    cv:8,  planet:'Jupiter', type:'Craft',      effect:'Reduce the cost to craft Aqua Regia by -1 card (minimum 1).' },
      { rank:'Nine',     cv:9,  planet:'Venus',   type:'Social',     effect:'Draw 1 Kismeta Card for each successful Trade this round.' },
      { rank:'Ten',      cv:10, planet:'Saturn',  type:'Opposition', effect:'This card counts as a Wild Suit in Oppositions.' },
      { rank:'Princess', cv:11, planet:'Mercury', type:'Gambit',     effect:'+1 to Dice Rolls when Defending in Gambits.' },
      { rank:'Knight',   cv:12, planet:'Mars',    type:'Duel',       effect:'+1 to Dice Rolls when Defending in Duels.' },
      { rank:'Queen',    cv:13, planet:'Moon',    type:'Forge',      effect:'Aqua Regia is Wild for Alchemical Formulas.' },
      { rank:'King',     cv:14, planet:'Saturn',  type:'Craft',      effect:'Discard to craft 1 Aqua Regia.' },
    ],
    card2: [
      { rank:'Ace',      cv:1,  planet:'Sun',     wcName:null,               wcNum:-1, effect:'+2 Bonus Harvest Cards when the Cosmic Age\'s Element is Water.' },
      { rank:'Two',      cv:2,  planet:'Moon',    wcName:'The High Priestess',wcNum:2,  effect:'Wildcard for The High Priestess.' },
      { rank:'Three',    cv:3,  planet:'Jupiter', wcName:'Wheel of Fortune',  wcNum:10, effect:'Wildcard for Wheel of Fortune.' },
      { rank:'Four',     cv:4,  planet:'Venus',   wcName:'The Chariot',       wcNum:7,  effect:'Wildcard for The Chariot.' },
      { rank:'Five',     cv:5,  planet:'Mercury', wcName:'The Sun',           wcNum:19, effect:'Wildcard for The Sun.' },
      { rank:'Six',      cv:6,  planet:'Saturn',  wcName:'The Lovers',        wcNum:6,  effect:'Wildcard for The Lovers.' },
      { rank:'Seven',    cv:7,  planet:'Mars',    wcName:'The Fool',          wcNum:0,  effect:'Wildcard for The Fool.' },
      { rank:'Eight',    cv:8,  planet:'Jupiter', wcName:'Wheel of Fortune',  wcNum:10, effect:'Wildcard for Wheel of Fortune.' },
      { rank:'Nine',     cv:9,  planet:'Venus',   wcName:'The Chariot',       wcNum:7,  effect:'Wildcard for The Chariot.' },
      { rank:'Ten',      cv:10, planet:'Saturn',  wcName:'The Hanged Man',    wcNum:12, effect:'Wildcard for The Hanged Man.' },
      { rank:'Princess', cv:11, planet:'Mercury', wcName:'The Moon',          wcNum:18, effect:'Wildcard for The Moon.' },
      { rank:'Knight',   cv:12, planet:'Mars',    wcName:'The Star',          wcNum:17, effect:'Wildcard for The Star.' },
      { rank:'Queen',    cv:13, planet:'Moon',    wcName:null,               wcNum:-1, effect:'Increase your Hand Limit by +1 at Round end.' },
      { rank:'King',     cv:14, planet:'Saturn',  wcName:null,               wcNum:-1, effect:'Cups are protected from Duels. This card remains vulnerable.' },
    ],
  },
  Pentacles: {
    element: 'Earth',
    card1: [
      { rank:'Ace',      cv:1,  planet:'Sun',     type:'Entry Fee',  effect:'Discard to build an Astral House when your Zodiac die is on an Earth sign.' },
      { rank:'Two',      cv:2,  planet:'Moon',    type:'Harvest',    effect:'Astral Houses on Earth signs earn double at Harvest.' },
      { rank:'Three',    cv:3,  planet:'Jupiter', type:'Build',      effect:'Craft Salt for any two Pentacles cards.' },
      { rank:'Four',     cv:4,  planet:'Venus',   type:'Reversed',   effect:'You must offer +2 Resources for every +1 Received in Trades.', nullifies:'5 of Swords', isCurse:true },
      { rank:'Five',     cv:5,  planet:'Mercury', type:'Reversed',   effect:'All Reagents cost an additional +1 Card to Craft.', nullifies:'6 of Swords', isCurse:true },
      { rank:'Six',      cv:6,  planet:'Saturn',  type:'Reversed',   effect:'Adept cards cost an additional +1 card to purchase.', nullifies:'4 of Swords', isCurse:true },
      { rank:'Seven',    cv:7,  planet:'Mars',    type:'Forge',      effect:'When you Fire the Stone, gain 1 Vitriol.' },
      { rank:'Eight',    cv:8,  planet:'Jupiter', type:'Craft',      effect:'Reduce the cost to craft Vitriol by -1 card (minimum 1).' },
      { rank:'Nine',     cv:9,  planet:'Venus',   type:'Social',     effect:'Draw +2 Cards for each successful Gambit this round.' },
      { rank:'Ten',      cv:10, planet:'Saturn',  type:'Opposition', effect:'This card counts as a Wild Suit in Oppositions.' },
      { rank:'Princess', cv:11, planet:'Mercury', type:'Gambit',     effect:'+1 to Dice Rolls when Defending in Gambits.' },
      { rank:'Knight',   cv:12, planet:'Mars',    type:'Duel',       effect:'+1 to Dice Rolls when Defending in Duels.' },
      { rank:'Queen',    cv:13, planet:'Moon',    type:'Forge',      effect:'Vitriol is Wild for Alchemical Formulas.' },
      { rank:'King',     cv:14, planet:'Saturn',  type:'Craft',      effect:'Discard to craft 1 Vitriol.' },
    ],
    card2: [
      { rank:'Ace',      cv:1,  planet:'Sun',     wcName:null,            wcNum:-1, effect:'+2 Bonus Harvest Cards when the Cosmic Age\'s Element is Earth.' },
      { rank:'Two',      cv:2,  planet:'Moon',    wcName:'The Hierophant',wcNum:5,  effect:'Wildcard for The Hierophant.' },
      { rank:'Three',    cv:3,  planet:'Jupiter', wcName:'The Empress',   wcNum:3,  effect:'Wildcard for The Empress.' },
      { rank:'Four',     cv:4,  planet:'Venus',   wcName:'The Hermit',    wcNum:9,  effect:'Wildcard for The Hermit.' },
      { rank:'Five',     cv:5,  planet:'Mercury', wcName:'The Devil',     wcNum:15, effect:'Wildcard for The Devil.' },
      { rank:'Six',      cv:6,  planet:'Saturn',  wcName:'The Hierophant',wcNum:5,  effect:'Wildcard for The Hierophant.' },
      { rank:'Seven',    cv:7,  planet:'Mars',    wcName:'Death',         wcNum:13, effect:'Wildcard for Death.' },
      { rank:'Eight',    cv:8,  planet:'Jupiter', wcName:'The Empress',   wcNum:3,  effect:'Wildcard for The Empress.' },
      { rank:'Nine',     cv:9,  planet:'Venus',   wcName:'The Devil',     wcNum:15, effect:'Wildcard for The Devil.' },
      { rank:'Ten',      cv:10, planet:'Saturn',  wcName:'The Hermit',    wcNum:9,  effect:'Wildcard for The Hermit.' },
      { rank:'Princess', cv:11, planet:'Mercury', wcName:'Judgement',     wcNum:20, effect:'Wildcard for Judgement.' },
      { rank:'Knight',   cv:12, planet:'Mars',    wcName:'The Moon',      wcNum:18, effect:'Wildcard for The Moon.' },
      { rank:'Queen',    cv:13, planet:'Moon',    wcName:null,            wcNum:-1, effect:'Increase your Hand Limit by +1 at Round end.' },
      { rank:'King',     cv:14, planet:'Saturn',  wcName:null,            wcNum:-1, effect:'Pentacles are protected from Duels. This card remains vulnerable.' },
    ],
  },
  Swords: {
    element: 'Air',
    card1: [
      { rank:'Ace',      cv:1,  planet:'Sun',     type:'Entry Fee',  effect:'Discard to build an Astral House when your Zodiac die is on an Air sign.' },
      { rank:'Two',      cv:2,  planet:'Moon',    type:'Harvest',    effect:'Astral Houses on Air signs earn double at Harvest.' },
      { rank:'Three',    cv:3,  planet:'Jupiter', type:'Build',      effect:'Craft Salt for any two Swords cards.' },
      { rank:'Four',     cv:4,  planet:'Venus',   type:'Reversed',   effect:'You must offer 2 Ante Cards (instead of 1) to begin a Duel.', nullifies:'5 of Cups', isCurse:true },
      { rank:'Five',     cv:5,  planet:'Mercury', type:'Reversed',   effect:'Pay an extra +1 Salt to start any Opposition or Gambit.', nullifies:'6 of Cups', isCurse:true },
      { rank:'Six',      cv:6,  planet:'Saturn',  type:'Reversed',   effect:'Duels you start are a best-of-3 dice roll.', nullifies:'4 of Cups', isCurse:true },
      { rank:'Seven',    cv:7,  planet:'Mars',    type:'Forge',      effect:'When you Fire the Stone, gain 1 Quicksilver.' },
      { rank:'Eight',    cv:8,  planet:'Jupiter', type:'Craft',      effect:'Reduce the cost to craft Quicksilver by -1 card (minimum 1).' },
      { rank:'Nine',     cv:9,  planet:'Venus',   type:'Social',     effect:'Draw +2 Cards for each successful Duel this round.' },
      { rank:'Ten',      cv:10, planet:'Saturn',  type:'Opposition', effect:'This card counts as a Wild Suit in Oppositions.' },
      { rank:'Princess', cv:11, planet:'Mercury', type:'Gambit',     effect:'+1 to Dice Rolls when Attacking in Gambits.' },
      { rank:'Knight',   cv:12, planet:'Mars',    type:'Duel',       effect:'+1 to Dice Rolls when Attacking in Duels.' },
      { rank:'Queen',    cv:13, planet:'Moon',    type:'Forge',      effect:'Quicksilver is Wild for Alchemical Formulas.' },
      { rank:'King',     cv:14, planet:'Saturn',  type:'Craft',      effect:'Discard to craft 1 Quicksilver.' },
    ],
    card2: [
      { rank:'Ace',      cv:1,  planet:'Sun',     wcName:null,          wcNum:-1, effect:'+2 Bonus Harvest Cards when the Cosmic Age\'s Element is Air.' },
      { rank:'Two',      cv:2,  planet:'Moon',    wcName:'The High Priestess',wcNum:2,  effect:'Wildcard for The High Priestess.' },
      { rank:'Three',    cv:3,  planet:'Jupiter', wcName:'Justice',     wcNum:11, effect:'Wildcard for Justice.' },
      { rank:'Four',     cv:4,  planet:'Venus',   wcName:'Justice',     wcNum:11, effect:'Wildcard for Justice.' },
      { rank:'Five',     cv:5,  planet:'Mercury', wcName:'The World',   wcNum:21, effect:'Wildcard for The World.' },
      { rank:'Six',      cv:6,  planet:'Saturn',  wcName:'Strength',    wcNum:8,  effect:'Wildcard for Strength.' },
      { rank:'Seven',    cv:7,  planet:'Mars',    wcName:'Strength',    wcNum:8,  effect:'Wildcard for Strength.' },
      { rank:'Eight',    cv:8,  planet:'Jupiter', wcName:'Death',       wcNum:13, effect:'Wildcard for Death.' },
      { rank:'Nine',     cv:9,  planet:'Venus',   wcName:'The Tower',   wcNum:16, effect:'Wildcard for The Tower.' },
      { rank:'Ten',      cv:10, planet:'Saturn',  wcName:'The Tower',   wcNum:16, effect:'Wildcard for The Tower.' },
      { rank:'Princess', cv:11, planet:'Mercury', wcName:'The Sun',     wcNum:19, effect:'Wildcard for The Sun.' },
      { rank:'Knight',   cv:12, planet:'Mars',    wcName:'The Star',    wcNum:17, effect:'Wildcard for The Star.' },
      { rank:'Queen',    cv:13, planet:'Moon',    wcName:null,          wcNum:-1, effect:'Increase your Spread Limit by +1 at Round end.' },
      { rank:'King',     cv:14, planet:'Saturn',  wcName:null,          wcNum:-1, effect:'Swords are protected from Duels. This card remains vulnerable.' },
    ],
  },
  Wands: {
    element: 'Fire',
    card1: [
      { rank:'Ace',      cv:1,  planet:'Sun',     type:'Entry Fee',  effect:'Discard to build an Astral House when your Zodiac die is on a Fire sign.' },
      { rank:'Two',      cv:2,  planet:'Moon',    type:'Harvest',    effect:'Astral Houses on Fire signs earn double at Harvest.' },
      { rank:'Three',    cv:3,  planet:'Jupiter', type:'Build',      effect:'Craft Salt for any two Wands cards.' },
      { rank:'Four',     cv:4,  planet:'Venus',   type:'Reversed',   effect:'Your opponent draws 2 Kismeta Cards if they win a Duel against you.', nullifies:'5 of Pentacles', isCurse:true },
      { rank:'Five',     cv:5,  planet:'Mercury', type:'Reversed',   effect:'-1 to Dice rolls when you defend in Duels.', nullifies:'6 of Pentacles', isCurse:true },
      { rank:'Six',      cv:6,  planet:'Saturn',  type:'Reversed',   effect:'-1 to dice rolls when you attack in Duels.', nullifies:'4 of Pentacles', isCurse:true },
      { rank:'Seven',    cv:7,  planet:'Mars',    type:'Forge',      effect:'When you Fire the Stone, gain 1 Sulphur.' },
      { rank:'Eight',    cv:8,  planet:'Jupiter', type:'Craft',      effect:'Reduce the cost to craft Sulphur by -1 card (minimum 1).' },
      { rank:'Nine',     cv:9,  planet:'Venus',   type:'Social',     effect:'Draw 1 Kismeta Card for each successful Opposition this round.' },
      { rank:'Ten',      cv:10, planet:'Saturn',  type:'Opposition', effect:'This card counts as a Wild Suit in Oppositions.' },
      { rank:'Princess', cv:11, planet:'Mercury', type:'Gambit',     effect:'+1 to Dice Rolls when Attacking in a Gambit.' },
      { rank:'Knight',   cv:12, planet:'Mars',    type:'Duel',       effect:'+1 to Dice Rolls when Attacking in a Duel.' },
      { rank:'Queen',    cv:13, planet:'Moon',    type:'Forge',      effect:'Sulphur is Wild for Alchemical Formulas.' },
      { rank:'King',     cv:14, planet:'Saturn',  type:'Craft',      effect:'Discard to craft 1 Sulphur.' },
    ],
    card2: [
      { rank:'Ace',      cv:1,  planet:'Sun',     wcName:null,          wcNum:-1, effect:'+2 Bonus Harvest Cards when the Cosmic Age\'s Element is Fire.' },
      { rank:'Two',      cv:2,  planet:'Moon',    wcName:'The Lovers',  wcNum:6,  effect:'Wildcard for The Lovers.' },
      { rank:'Three',    cv:3,  planet:'Jupiter', wcName:'The Magician',wcNum:1,  effect:'Wildcard for The Magician.' },
      { rank:'Four',     cv:4,  planet:'Venus',   wcName:'The Hanged Man',wcNum:12,effect:'Wildcard for The Hanged Man.' },
      { rank:'Five',     cv:5,  planet:'Mercury', wcName:'Temperance',  wcNum:14, effect:'Wildcard for Temperance.' },
      { rank:'Six',      cv:6,  planet:'Saturn',  wcName:'The Emperor', wcNum:4,  effect:'Wildcard for The Emperor.' },
      { rank:'Seven',    cv:7,  planet:'Mars',    wcName:'The Fool',    wcNum:0,  effect:'Wildcard for The Fool.' },
      { rank:'Eight',    cv:8,  planet:'Jupiter', wcName:'The Magician',wcNum:1,  effect:'Wildcard for The Magician.' },
      { rank:'Nine',     cv:9,  planet:'Venus',   wcName:'Temperance',  wcNum:14, effect:'Wildcard for Temperance.' },
      { rank:'Ten',      cv:10, planet:'Saturn',  wcName:'The Emperor', wcNum:4,  effect:'Wildcard for The Emperor.' },
      { rank:'Princess', cv:11, planet:'Mercury', wcName:'Judgement',   wcNum:20, effect:'Wildcard for Judgement.' },
      { rank:'Knight',   cv:12, planet:'Mars',    wcName:'The World',   wcNum:21, effect:'Wildcard for The World.' },
      { rank:'Queen',    cv:13, planet:'Moon',    wcName:null,          wcNum:-1, effect:'Increase your Spread Limit by +1 at Round end.' },
      { rank:'King',     cv:14, planet:'Saturn',  wcName:null,          wcNum:-1, effect:'Wands are protected from Duels. This card remains vulnerable.' },
    ],
  },
};

const rankSlug = r => r.toLowerCase().replace(/ /g, '-');

for (const [suit, suitData] of Object.entries(minorArcanaData)) {
  for (let i = 0; i < suitData.card1.length; i++) {
    const c1 = suitData.card1[i];
    const c2 = suitData.card2[i];
    const slug = `${suit.toLowerCase()}.${rankSlug(c1.rank)}`;

    cards.push({
      id: `minor.${slug}.1`,
      deck: 'Kismeta',
      suit,
      rank: c1.rank,
      cardVariant: 1,
      majorArcanaType: null,
      arcanaNumber: -1,
      name: `${c1.rank} of ${suit}`,
      sign: null,
      planet: c1.planet,
      effectType: c1.type ?? null,
      effectText: c1.effect,
      isCurse: c1.isCurse ?? false,
      nullifiesCard: c1.nullifies ?? null,
      wildcardArcanaNumber: -1,
      crucibleGroup: null,
      activationFormula: null,
      alchemicalFormula: null,
      alchemicalCost: null,
    });

    cards.push({
      id: `minor.${slug}.2`,
      deck: 'Kismeta',
      suit,
      rank: c2.rank,
      cardVariant: 2,
      majorArcanaType: null,
      arcanaNumber: -1,
      name: `${c2.rank} of ${suit} (II)`,
      sign: null,
      planet: c2.planet,
      effectType: c2.wcName ? 'WildcardLink' : 'Passive',
      effectText: c2.effect,
      isCurse: false,
      nullifiesCard: null,
      wildcardArcanaNumber: c2.wcNum,
      wildcardArcanaMajorName: c2.wcName ?? null,
      crucibleGroup: null,
      activationFormula: null,
      alchemicalFormula: null,
      alchemicalCost: null,
    });
  }
}

// ─── Output & validation ─────────────────────────────────────────────────────
const EXPECTED = 156;
const outPath = join(OUT_DIR, 'cards.json');
writeFileSync(outPath, JSON.stringify(cards, null, 2));

const majorCount    = cards.filter(c => c.deck === 'Kismeta' && !c.suit).length;
const crucibleCount = cards.filter(c => c.deck === 'Crucible').length;
const minorCount    = cards.filter(c => c.suit).length;
const total         = cards.length;

console.log(`[cards] wrote cards.json`);
console.log(`  Major Arcana : ${majorCount}  (expected 22)`);
console.log(`  Crucible     : ${crucibleCount}  (expected 22)`);
console.log(`  Minor Arcana : ${minorCount} (expected 112)`);
console.log(`  TOTAL        : ${total} (expected ${EXPECTED})`);

if (total !== EXPECTED) {
  console.warn(`[cards] WARNING: expected ${EXPECTED} cards, got ${total}`);
  process.exitCode = 1;
} else {
  console.log('[cards] Card count OK.');
}

// Spread limit note
console.log('[cards] NOTE: Authoritative Spread limit is 5 (card-zones.md / Winter). Glossary "7" is a known doc inconsistency.');
