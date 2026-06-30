# KISMETA
### *Alchemists of the Great Year*
## Narrative Slot Framework

*Digital adaptation — keeping story, intent & lore continuous across every screen*

---

## Contents

1. [Why this framework exists](#1--why-this-framework-exists)
2. [The four narrative slots](#2--the-four-narrative-slots)
3. [Two tiers of scene](#3--two-tiers-of-scene)
4. [Authoring rules](#4--authoring-rules)
5. [The populated content — every step](#5--the-populated-content--every-step)
6. [Data schema (for engineering)](#6--data-schema-for-engineering)
7. [The persistent thread (recommended)](#7--the-persistent-thread-recommended)
8. [Worked example — applying it to an existing screen](#8--worked-example--applying-it-to-an-existing-screen)

---

## 1 · Why this framework exists

A physical Kismeta player sees the whole table at once — the board, every rival's cards, the dice, the lore in the rulebook. A phone player sees one screen at a time. Each screen is a narrow window, and atmosphere, intent, and rules all have to fit through it without crowding each other out or vanishing.

The risk in any board-game-to-screen port is that the game alternates between two failure modes: atmospheric transition screens that say nothing actionable, and mechanical action screens that strip away every trace of the world. Players feel the seams. The story stops being continuous.

This framework prevents that by defining a small, fixed set of narrative slots that appear in the same place on every scene. Each slot answers exactly one question. Because the slots never move and never change voice, the player learns to read them at a glance — the gold line is always the cosmos speaking, the instruction line is always "what I do now." The thread never drops, even on a dense forge screen.

> **Design principle:** Author narrative as *data, not layout*. Every step's story text lives in one structured place (Section 5), separate from the UI that renders it. This keeps the voice consistent, lets you tune verbosity globally, and means a writer and an engineer can work from the same source.

---

## 2 · The four narrative slots

Every scene fills the same four slots. Each has a fixed voice, a fixed screen position, and a fixed question it answers. Treat the voice column as a hard contract — it is what makes the slots legible across 26 different screens.

| Slot | Question it answers | Voice & tone | Screen position |
|---|---|---|---|
| **Cosmic beat** | Where are we in the cosmic story? | The Fates speaking. Mythic, italic, second-person. One line on action steps; a full paragraph on transitions. | Top — above the action |
| **Your charge** | What am I trying to do right now? | Instructional, present-tense, second-person. Plain and concrete. Never flavorful. | Just under the beat |
| **The stakes** | What's the risk or tradeoff? | Cautionary aside. Warns of exposure, permanence, or a one-way door. Omitted on purely safe/admin steps. | A callout box, mid-scene |
| **The verb** | What do I literally tap? | UI label. One or two words. Imperative. Matches the button exactly. | The action button(s) |

> **Note on the beat.** Your existing season-intro screens already nail this slot with a full paragraph. On action screens, compress it to **one line** — enough to keep the world present, not so much that it competes with the charge. This is the "light" verbosity setting you chose.

---

## 3 · Two tiers of scene

Not every scene carries the same narrative weight. Sort each into one of two tiers; the tier decides how much of each slot you render.

| | Transition scenes | Action scenes |
|---|---|---|
| **What they are** | Season & age intros, age-passing screens. The story breathes here. | In-step interactions: harvest, forge, craft, duel, wager. |
| **Cosmic beat** | Full paragraph. Maximum atmosphere. | One line only. |
| **Your charge** | A short list of the steps to come. | A single concrete instruction. |
| **The stakes** | A "why it matters" callout. | One line, only if real risk exists. |
| **The verb** | One forward button ("Begin Spring"). | The specific action(s) available. |
| **Frequency** | Once per season / age boundary. | Every interactive step. |

---

## 4 · Authoring rules

Follow these when writing or revising any step's text, so 26 screens read as one authored voice.

- **One question per slot.** If a line answers two questions, it belongs in two slots. Never let the charge drift into flavor or the beat into instruction.
- **The beat is the Fates; the charge is the game; the stakes is a warning.** Keep these three voices distinct — a player should feel who is speaking without being told.
- **Keep action-tier beats to one line**, ideally under twelve words. Atmosphere is seasoning, not the meal.
- **Stakes appear only when a step carries real risk**, permanence, or a one-way commitment (firing, building, locking, wagering). Pure admin steps (survey, unlock, enforce limits) omit it — a false warning erodes trust in the slot.
- **The verb must match the button label exactly.** If the screen has two buttons, the verb slot lists both, separated by a middot.
- **Use the game's own capitalized terms** — Spread, Forge, Stasis, Ward, Adept — consistently. They are proper nouns of the world.
- **Prefer the second person** throughout. "Drive your stone toward gold," not "the player advances the stone."

> **A built-in path to a verbosity toggle.** Because the beat and stakes slots are authored separately from the charge and verb, you can later expose a player setting — full lore, light (default), or terse — simply by choosing which slots to render. Returning players who know the rules can drop to charge + verb; first-timers keep all four. You don't rewrite anything; you just hide slots.

---

## 5 · The populated content — every step

The complete slot text for all 26 steps, grouped by season. Transition steps are marked. This is the authoring source: edit prose here, and treat Section 6 as the machine-readable mirror of this table.

### Spring

#### Season Intro · *transition*

| Slot | Text |
|---|---|
| **Cosmic beat** | *A new age dawns. Set the sign, claim your place on the wheel, and gather your harvest.* |
| **Your charge** | Open the round: set the cosmic age, find your sign, and gather your harvest. |
| **The stakes** | This age's sign shapes everything — which cards harvest well, which alignments score, and your own cosmic effect for the round. |
| **The verb** | Begin Spring |

#### Set the Cosmic Age · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Roll the Cosmic Age die, and let the heavens set this round's Sign for all.* |
| **Your charge** | Read the age's three Aspects — Sign, Planet, Element — and its cosmic effect aloud. |
| **The stakes** | Every harvest bonus and alignment this round is measured against these three Aspects. |
| **The verb** | Cast the age |

#### Determine Your Sign · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Roll your Zodiac die and take your sign. The cosmos marks you for this age.* |
| **Your charge** | Roll your Zodiac die and move your meeple to your sign — your identity for this round. |
| **The stakes** | Your sign is an alignment source AND grants a personal cosmic effect that lasts until Winter. |
| **The verb** | Roll your zodiac die |

#### Harvest Cards · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Tally your alignment bonuses and take your harvest from the Agekeeper.* |
| **Your charge** | Tally your bonus from every source, then take your full harvest from the Agekeeper. |
| **The stakes** | Each source scores only its single highest Aspect — Sign +3, Planet +2, Element +1. Nothing stacks within a source. |
| **The verb** | Deal & commune |

#### Commune · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Sort your fortune. Place cards in Spread, Hand, and Arcanum.* |
| **Your charge** | Sort your cards into Spread (visible engine) and Hand (hidden reserve). Major Arcana go to your Arcanum. |
| **The stakes** | Spread cards score alignment and activate Crucibles but can be stolen in a Duel. Hand cards are safe but idle. |
| **The verb** | Lock the tableau · to Summer |

#### Card Lock · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Lock your tableau. Spread and Hand freeze until Winter releases them.* |
| **Your charge** | Confirm your placement — cards freeze between Spread and Hand until Winter. |
| **The stakes** | Over-load your Spread and you expose value to theft; under-load it and you starve your engine. This is binding. |
| **The verb** | Lock 🔒 |

### Summer

#### Season Intro · *transition*

| Slot | Text |
|---|---|
| **Cosmic beat** | *The sun rides high. Craft, activate, trade, duel, gambit, and oppose as you will.* |
| **Your charge** | Take any actions, in any order, as many as you can fuel — no fixed order, no turn limit. |
| **The stakes** | This is where the round is won or lost. Repeat actions as long as you have cards and reagents to spend. |
| **The verb** | Begin Summer |

#### Activate Crucible Card · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Activate a Crucible. Complete its Codex formula and light the cauldron.* |
| **Your charge** | Collect a Codex card set in your Spread, discard it, light the matching cauldron, and flip the Crucible card face-up. |
| **The stakes** | The coal can never be moved once placed — light the cauldron your higher-tier cards will demand. |
| **The verb** | Activate |

#### Craft a Reagent · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Craft a reagent. Feed three suit-matched cards into a lit cauldron.* |
| **Your charge** | Discard 3 suit-matching cards into a lit cauldron to craft 1 elemental reagent. Salt needs no cauldron — any 3 cards. |
| **The stakes** | Reagents only transfer by Trade. Each elemental type needs its own cauldron lit first. |
| **The verb** | Forge |

#### Build an Astral House · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Raise a House on your sign. Pay two planet-matching cards and stake the sky.* |
| **Your charge** | Pay 2 cards matching your sign's planet to raise a House — a permanent harvest source, opposition boost, and cosmic effect. |
| **The stakes** | Houses are permanent — the cards are spent for good and cannot be reclaimed. Build deliberately. |
| **The verb** | Raise the House |

#### Place Wards · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Set your wards. Place reagents on Active cards before rivals come.* |
| **Your charge** | Place reagents on your Active Crucible or Adept cards to set the fee challengers must pay to Gambit them. |
| **The stakes** | Crucible wards are permanent; Adept wards return if the Adept leaves play. An unwarded active card can be gambited for free. |
| **The verb** | Seal the wards |

#### Trade · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Strike a bargain. Exchange cards, reagents, or Active Crucibles with a rival.* |
| **Your charge** | Exchange cards, reagents, or Active Crucible cards freely with a rival — the only way to move reagents. |
| **The stakes** | Hidden Hand cards cannot be requested. In Magnus mode, misaligned players trade 2:1. |
| **The verb** | Complete trade |

#### Duel · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Duel for a Spread card. Ante your own and roll against a rival.* |
| **Your charge** | Name a card in a rival's Spread, ante a card of your own, and roll — higher roll takes the prize. |
| **The stakes** | Lose, and your ante returns to the deck. Only Spread cards can be targeted; the Hand is safe. |
| **The verb** | Ante & roll |

#### Gambit · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Gambit a rival's Active card. Pay their ward fee and roll for the prize.* |
| **Your charge** | Stake one of your Active cards to seize a rival's Crucible or Adept card. Pay their ward fee, then roll. |
| **The stakes** | Lose, and your offered card is Arrested — pay 1 Salt to free it, and you can't re-gambit that rival this round. |
| **The verb** | Challenge this rival |

### Autumn

#### Season Intro · *transition*

| Slot | Text |
|---|---|
| **Cosmic beat** | *The crucible runs hot. Craft, activate, fire your stone, and drive it toward gold.* |
| **Your charge** | Advance your stone through the forge — and oppose any rival that dares to forge. |
| **The stakes** | The moment your stone enters the forge it can be Opposed. Weigh advancing now against waiting for a safer age. |
| **The verb** | Begin Autumn |

#### Survey the Crucible · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Read the forge before you move.* |
| **Your charge** | Note every stone's position — Mantle (safe), Forge (vulnerable), Stasis (frozen), or the Altar. |
| **The stakes** | — *(no stakes shown on this step)* |
| **The verb** | Survey |

#### Opposition · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Align against a rival and break their forging.* |
| **Your charge** | Pay the defender's ward fee, roll, and total your Aspect alignment. Win, and their stone falls to Stasis. |
| **The stakes** | Each defense the rival survives grants them a stacking Besieged Bonus — a dogpile on the leader can backfire. |
| **The verb** | Oppose |

#### Fire the Stone · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Commit your stone to the flames.* |
| **Your charge** | Satisfy an Active Crucible card's formula — alignment + reagents — and advance your stone into the forge. |
| **The stakes** | A stone Fired this Autumn is safe this round, but Forging next round leaves it open to Opposition. Ward as you advance. |
| **The verb** | Fire |

#### Temper the Stone · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Temper your stone. Advance it to the next Mantle and seal this stage.* |
| **Your charge** | Advance a stone that has forged a full round to the next Mantle space and discard its Crucible card. |
| **The stakes** | A stone that returned from Stasis this round cannot Temper yet — it must forge a full round first. |
| **The verb** | Temper |

#### Leave Stasis · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Leave Stasis. Pay Salt and return your stone to the forge.* |
| **Your charge** | Pay 2 Salt to return your stone to its old forge position and resume forging. |
| **The stakes** | If your old position is taken, you must wait a round or win a Stasis Opposition to swap into it. |
| **The verb** | Leave Stasis |

### Winter

#### Season Intro · *transition*

| Slot | Text |
|---|---|
| **Cosmic beat** | *The long night settles in. Unlock, wager, trim your hand, and turn the wheel.* |
| **Your charge** | Close the age: unlock your cards, place an optional wager, enforce limits, and transit to the next age. |
| **The stakes** | Only your cards reset. Lit cauldrons, astral houses, active Crucible cards, and your stone's forge position all carry forward. |
| **The verb** | Begin Winter |

#### Card Unlock · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Unlock your cards. Move freely between Hand and Spread one last time.* |
| **Your charge** | Move cards freely between Hand and Spread one last time before the age closes. |
| **The stakes** | — *(no stakes shown on this step)* |
| **The verb** | Unlock cards |

#### Fateful Wager · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Bet on the sign the coming age will wear.* |
| **Your charge** | Predict the next cosmic sign and stake any cards from your Spread or Hand. Guess right and your wager doubles. |
| **The stakes** | Guess wrong and the cards are lost to the Fates. Major Arcana in your Arcanum cannot be wagered. |
| **The verb** | Place wager · Skip wager |

#### Card Limits · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Enforce your limits. Discard down to five Spread and five Hand.* |
| **Your charge** | Discard down to 5 Spread and 5 Hand. Return all Fate cards to the deck; Adepts remain. |
| **The stakes** | — *(no stakes shown on this step)* |
| **The verb** | Enforce limits |

#### Transit the Age · *action*

| Slot | Text |
|---|---|
| **Cosmic beat** | *Turn the wheel. Pass the key and begin the next Cosmic Age.* |
| **Your charge** | The Agekeeper shuffles the deck and passes the key clockwise — a new age begins, unless the Great Work is done. |
| **The stakes** | — *(no stakes shown on this step)* |
| **The verb** | Turn the wheel · cast the next age |

---

## 6 · Data schema (for engineering)

Author every step as a record in one content file, keyed by a stable step id. The renderer reads slots by tier and verbosity; it never hard-codes prose. This is the contract between the writer's table above and the UI.

```json
{
  "id": "autumn.fire",            // stable, never reused
  "season": "Autumn",
  "tier": "action",              // "transition" | "action"
  "order": 3,                     // sequence within the season
  "beat":   "Commit your stone to the flames.",
  "charge": "Satisfy an Active Crucible card's formula — alignment + reagents — and advance into the forge.",
  "stakes": "A stone Fired this Autumn is safe this round, but Forging next round opens it to Opposition. Ward as you advance.",
  "verb":   ["Fire"],             // array; one entry per button, in display order
  "breadcrumb": "Forging"         // optional: stone/sub-state for the persistent thread (Sec. 7)
}
```

- **`stakes` is nullable.** A null value means the renderer shows no stakes callout — that is the intended state for safe/admin steps, not missing content.
- **`verb` is an array** so a screen with two buttons (e.g. "Place wager" · "Skip wager") needs no special case.
- **A verbosity setting maps to a slot mask:** full = `[beat, charge, stakes, verb]`; light = same but beat trimmed to one line (default); terse = `[charge, verb]`.
- **Because ids are stable,** you can attach analytics, localization keys, and audio/voice-over lines to the same record later without restructuring.

---

## 7 · The persistent thread (recommended)

The four slots handle the current step. One more element handles the macro story: an always-visible breadcrumb that situates the moment in the larger arc, so a phone player never loses the thread a tabletop player gets for free from seeing the whole board.

Your header already shows **Leo · Spring · Round 1**. Extend it with the stone's current state, which ties lore to mechanics in a single word:

| Breadcrumb reads | Stone state | What it silently tells the player |
|---|---|---|
| *Leo · Autumn · Tempering* | Mantle Ring | Safe — focus on activating & firing |
| *Leo · Autumn · Forging* | Forge | Exposed — you can be Opposed now |
| *Leo · Autumn · In Stasis* | Stasis | Frozen — your charge is to get back |

Updating one breadcrumb word as the stone moves does more narrative work than a paragraph of text, because it is always on screen and always true. It is the smallest possible version of "keep the story present."

---

## 8 · Worked example — applying it to an existing screen

Take your Autumn forge screen (the stained-glass crucible with Fire / Temper / Oppose / Cards / Pass). Today it carries the charge and the verbs well, but the beat and stakes have dropped away. Here is the same screen with all four slots restored at "light" verbosity:

| Slot | Rendered text | Where it sits |
|---|---|---|
| **Breadcrumb** | Leo · Autumn · Round 1 · Forging | Header, persistent |
| **Cosmic beat** | *Commit your stone to the flames.* | One line, top of card |
| **Your charge** | Satisfy a Crucible formula and advance into the forge. | Under the beat |
| **The stakes** | Forging next round opens you to Opposition — ward as you advance. | Amber callout, mid-card |
| **The verb** | Fire · Temper · Oppose · Cards · Pass | The action buttons |

Nothing about the mechanics changed. Two short lines and a one-word breadcrumb carry the entire world back onto a screen that had become a control panel — and because they come from the same slots every other screen uses, the player reads them without effort.

---

*The Crucible of Kismeta awaits.*
