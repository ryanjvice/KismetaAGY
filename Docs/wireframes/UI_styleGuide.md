# Kismeta: Alchemists of the Great Year
## Mobile App — UI & Visual Style Guide

*Production design system for the Unity UI Toolkit port. Documents color and type
tokens, responsive layout rules, shared components, and per-scene specifications.
Wireframe mockups used a **380×844 design baseline** for proportions; the shipped
game is **full-bleed and responsive** on phones and tablets.*

---

## 1. Design Principles

These five ideas govern every screen. When a new screen is designed, it should
be checked against them.

1. **One persistent frame.** Spring, Summer, and Autumn share a single layout —
   status bar, two rival strips, a central object, a context action bar, and a
   tableau dock. The player learns the frame once; only the center and the
   actions change per season. This is what makes a dense board game legible on any
   portrait phone or tablet — the layout fills the viewport and scales with the device.

2. **The center is either a *map* or *state*.** The central object shows a *map*
   when a season is about positions you read (Spring's zodiac wheel, Autumn's
   crucible spiral) and *state* when it's about status you track (Summer's
   cauldrons). It is never decorative. Sequenced seasons (Spring, Winter) replace
   the map with a *step hub* when there is no live board object to show.

3. **Surface the rule at the decision point.** Every commitment screen shows its
   cost, consequence, and the relevant rule exactly where the player acts — Build's
   permanence, Activate's cauldron consequence, Commune's exposure tradeoff,
   Winter's "only cards reset." The app does the fiddly math (harvest scoring,
   alignment tallies, ward fees, card limits, wager odds) so the player decides,
   not calculates.

4. **Honor the public / hidden line absolutely.** Spreads, face-up Fates, and
   Active Adepts are public and shown in full. Hands and face-down cards are
   private and shown only as counts. No screen ever leaks hidden information —
   not the card table, not a rival strip, not a trade request.

5. **Know when to hold the frame and when to break it.** In-game screens keep the
   frame. *Ceremonial* screens deliberately break it: the title, the age
   open/close, and the victory coronation abandon the working chrome to become
   tableaux. Breaking the frame signals "this is a threshold, not play."

---

## 2. Color Tokens

### 2.1 Global / brand

| Token | Hex | Use |
|---|---|---|
| `--ink` | `#f3e9d2` | Primary text (warm parchment) |
| `--gold` | `#e8b94a` | Brand/primary accent, age, CTAs |
| `--gold-deep` | `#c9962f` | Gold borders, dividers |
| `--gold-bright` | `#f4d35e` | Highlights, your-stone, alignment glow |
| `--muted` | `#b89a6e` | Secondary/label text |
| `--muted-dim` | `#8a6a7a` | Tertiary text, uppercase eyebrows |

### 2.2 Suit colors (consistent everywhere a card appears)

| Suit | Element | Fill | Icon |
|---|---|---|---|
| Wands | Fire | `#7a1f1a` | `ti-wand` |
| Cups | Water | `#1f3a6e` | `ti-droplet` |
| Pentacles | Earth | `#1f6e4a` | `ti-coin` |
| Swords | Air | `#8a6a1f` | `ti-sword` |

### 2.3 Reagent colors

| Reagent | Dot fill | Dot border |
|---|---|---|
| Salt | `#cfcfcf` | `#ffffff` |
| Sulphur | `#c0392b` | `#e57373` |
| Vitriol | `#1f6e4a` | `#5dcaa5` |
| Aqua Regia | `#2e6da4` | `#6fa8d4` |
| Quicksilver | `#b8941f` | `#e0c060` |

### 2.4 Seasonal palettes

Each season owns a hue. The seasonal arc (green → amber → red → blue) reads as a
turning year; a player can tell the season from color alone.

| Season | Register | Bg base | Accent | Accent text | Sigil icon |
|---|---|---|---|---|---|
| **Spring** | awakening, growth | `#0f1410` | `#5dca8a` / `#7fc49a` | `#7fc49a` | `ti-flower` / ❀ |
| **Summer** | high sun, industry | `#180f0a` | `#f0a040` / `#f0b060` | `#f0b060` | `ti-sun` |
| **Autumn** | the forge, clash | `#160a0a` | `#d4783a` / `#e08a4a` | `#e08a4a` | `ti-flame` |
| **Winter** | the long night | `#0e1420` | `#6fa8d4` / `#9ec9e0` | `#9ec9e0` | ❄ / `ti-mountain` |

### 2.5 Semantic / system colors

| Meaning | Fill bg | Border | Text | Used for |
|---|---|---|---|---|
| Success / safe / win | `#16261f` | `#2b6a4a` | `#7fc4a8` | positive results, "ok" states |
| Danger / attack / loss | `#261016` | `#6a3030` | `#f0908a` | losses, exposure warnings, duels |
| Caution / over-limit | `#2a1f0e` / `#2a1810` | `#6a5a3a` / `#6a4a2a` | `#e0c878` / `#e0a060` | tips, over-limit, "still available" |
| Arcanum / magic | `#241028` | `#6a4a8a` | `#cdb0e0` | adepts, wagers, wards, commune |

### 2.6 Arcanum sub-palettes

| Card type | Bg | Border | Text | Icon | Numeral |
|---|---|---|---|---|---|
| **Adept** (active tool) | `#241038` purple | `#9a7ac0` | `#e0c8f0` | `ti-eye` | Roman (e.g. V) |
| **Fate** (event) | `#0e2440` blue | `#5a8ac0` | `#bcd8f0` | `ti-star` | Roman (e.g. X) |

---

## 3. Typography

- **Sans (UI):** system sans via `--font-sans`. All labels, body, buttons, data.
- **Serif (display):** `--font-serif`. Reserved for *ceremonial* names and titles
  only — KISMETA, age names (SCORPIO), "Magnus Alchemista", card proper names.
  Using serif marks a moment as a threshold.

### Type scale (baseline at 380×844)

Values below are the **design baseline**. Production uses scaled USS tokens
(`--font-body`, `--font-label`, `--font-title`, `--font-display`) set at runtime
by `ViewportLayout` from `--ui-scale`.

| Role | Baseline size | Weight | Notes |
|---|---|---|---|
| Ceremony title | 26–36 | 600 | serif, letter-spacing .06–.14em; uses `--font-display` |
| Screen/modal title | 14 | 500 | with icon; `--font-title` |
| Card proper name | 19 | 600 | serif |
| Section value / stat | 15–18 | bold | numbers, counts |
| Body | 11–13 | 400–500 | `--font-body` |
| Label / eyebrow | 9–10 | 400–500 | uppercase, letter-spacing .06–.10em, `--muted-dim`; `--font-label` |
| Micro / caption | 7–9 | 400 | sub-labels under chips, fine print; `--font-micro` |

---

## 4. Spacing, Shape & Elevation

- **Viewport:** screens fill `width: 100%` / `height: 100%` with safe-area padding
  on the root (notch, home indicator). No mockup phone-card frame or fixed width cap.
- **Design baseline:** 380×844 px artboard — drives proportions and PanelSettings
  reference resolution, not production layout width.
- **Cards/panels inside:** radius `9–14px`. Inset panels use a darker fill than
  their container with a 1px border one step lighter.
- **Buttons:** `min-height` ≥ 44px (`--touch-min`); radius `10–11px`; padding scales
  with `--ui-scale`. Primary uses the season/semantic accent at ~1.15–1.2 brightness;
  secondary is muted bg + muted border.
- **Header bars:** padding via `--space-sm` / `--space-md`, 1px bottom border.
- **Left-accent rows** (`border-left: 3px solid <player/season>`): used for
  per-item lists (ages in the chronicle, players in the card table).
- **Icon set:** Tabler Icons (`ti-*`) throughout, sized relative to `--font-title`.
- **Decorative backdrops:** faint (opacity .25–.55) sacred-geometry, constellations,
  snowflakes, or rising-heat motes behind ceremonial and intro headers only.

### 4.1 Responsive layout

Production layout is implemented in Unity UI Toolkit via `ViewportLayout`
(`Assets/_Project/UI/Scripts/Framework/ViewportLayout.cs`) and `Kismeta.uss`.

| Concern | Rule |
|---|---|
| **Full bleed** | `.screen`, `.kismeta-root` are 100% viewport; flex column layout |
| **Safe area** | `Screen.safeArea` applied as root padding on resize |
| **Scale tokens** | `--ui-scale` = `min(viewportW/380, viewportH/844)`, clamped 0.85–1.35 |
| **Spacing** | `--space-xs` … `--space-xl` derived from scale |
| **Touch targets** | `--touch-min` ≥ 44px on buttons, action bar, setup pips |
| **Flex frame** | status bar + rivals + **stage** (`flex-grow: 1`) + action bar + dock |
| **Modals** | full-screen scrim; centered card at 92% width, max 520px, max-height 88%, scrollable body |
| **Bottom sheets** | full width, anchored to bottom safe area, max-height 90% |
| **Viewport classes** | `.viewport--compact` (w &lt; 360), `.viewport--regular`, `.viewport--tablet` (shortest side ≥ 600dp) |
| **PanelSettings** | Scale With Screen Size; reference 380×844; match width/height ≈ 0.5 |

**Test resolutions** in Unity Game view before shipping a screen: 390×844, 428×926,
768×1024 (portrait phone and tablet).

---

## 5. Shared Components

These recur across many screens. Build once, reuse.

### 5.1 Status bar (top of every in-game frame)
Left: season sigil + season name + a one-line state subtitle. Right: a contextual
read-out (current Cosmic Age, or round number) + hamburger menu (`ti-menu-2`).
Recolored per season. The hamburger is the entry point to the **card table** and settings.

### 5.2 Rival strips
Two compact rows directly under the status bar showing other players. Each: color
dot + name + a **phase-aware** sub-stat (Autumn: stone position/ward; Summer: spread
size/houses; Spring: rolled sign/harvest status; Winter: closing progress). Tappable
to expand a rival's public detail. They are *season-aware* — always showing the stat
that matters now.

### 5.3 Central object
- **Map mode** — zodiac wheel (Spring), crucible spiral (Autumn). Tokens (meeples,
  stones) sit on positions. Your token always carries a persistent label.
- **State mode** — four cauldrons + codex counter (Summer). Lit = colored + solid
  gold border; dormant = dashed border, dimmed.
- **Step-hub mode** — for sequenced seasons with no live board (Winter; Spring at
  rest). A progress rail + the relevant state panel.

### 5.4 Context action bar
The 3-button row defining the season's verbs:
- **Autumn:** Fire · Temper · Oppose
- **Summer:** Craft & build · Consort · Activate (the first two open sheets)
- **Spring / Winter:** replaced by a single "continue to next step" CTA (sequenced).

Pattern: keep **three buttons** rhythmically even when a season has more actions —
group sub-actions into sheets rather than adding buttons.

### 5.5 Tableau dock (bottom)
Your private zone summary: a label + count badges, a horizontally-scrollable Spread
strip (aligned cards glow gold), Arcanum chips, and a Hand count (never the Hand
cards — hidden). Recolored per season. Spread strip scrolls horizontally on narrow
viewports; card chips use `--card-chip-w` / `--card-chip-h` tokens.

### 5.6 Step wizard
Multi-stage actions (Duel, Gambit, Opposition, Craft, Activate, the season step
flows) are split into numbered steps with a "step N of M" counter in the header and
a back button. Each step asks one question; the wizard *is* the rules enforcement
(you can't skip the ante, the fee, the target).

### 5.7 Dual-die contest component
Shared by Duel, Gambit, Opposition. Two pip dice that tumble (~12 frames @ 70ms)
then settle; values called out below; ties auto-reroll. The fork:
- **Duel** — roll *is* the score.
- **Gambit** — ward-fee gate first; roll is the score; outcome swaps/arrests.
- **Opposition** — roll *sets your sign*, then feeds an alignment **tally**.

### 5.8 Alignment / harvest tally
Itemized "where your points come from" list. Each row: source icon + name + matched
aspect + points, color-coded by aspect (**sign = gold +3**, **planet = coral +2**,
**element = teal +1**). Used in Opposition, Spring harvest, and the card-inspect
"alignment this age." Teaches the "highest single aspect only, not stacked" rule by
showing one number per source.

### 5.9 Reagent fee stepper
+/− steppers per reagent type with a running "paid / required" counter; + locks at
the required total; reagents you lack are dimmed. Used for Gambit fee, Opposition
fee, Fire cost, ward placement, Stasis escape.

### 5.10 Resolution screen
Every action resolves on a consistent template: **(1)** confirm the verdict, **(2)**
show the board change as *movement* (token slides), **(3)** itemize consequences in a
"what changed" block, **(4)** offer two forward exits (never a dead-end OK). Outcome
tints the screen and the final button (green win / red loss).

### 5.11 Transaction strip ("inputs → output")
A small "X → Y" row showing what was spent and what was gained (cards → reagent,
set → lit cauldron, wager → result). Reused across craft, activate, and contest results.

### 5.12 Tap-to-swap
Card-zone reorganization (Commune, Winter Unlock) uses tap-to-move between two
stacked zones, not drag (better mobile targets). Only legal cards are tappable;
Major Arcana are fixed.

---

## 6. Scene Catalog

Grouped by tier. Each entry notes the screen's job and its distinguishing spec.

### 6.1 Front-of-app & setup

**Title screen** — *ceremonial, frame broken.* Gold "8" altar sigil with halo over
faint sacred geometry; KISMETA in serif; tagline; menu (New game primary; Resume/Join;
How to play/Codex). Gold-on-dark.

**Setup (compact sheet)** — rises over title. Player count, mode, deck build, first
Agekeeper, with a live summary line. For quick start.

**Setup (dedicated screen)** — fuller version. Player count 2–6 (each with a
descriptive one-liner) + difficulty as **three concrete rule-sets**, with a live
"[mode] changes" panel:
- *Quickplay* (~30m): houses cost 1, light-then-craft gateway, gentle economy.
- *Standard* (~60m): all contests, balanced costs, free trading.
- *Magnus* (~90m): misaligned trades 2:1, misalignment grants foes +1, tight economy.

### 6.2 Age ceremonies — *frame broken, two tiers above seasons*

**Age opening** — cosmic curtain-up. Starfield + drawn constellation; sigil glowing;
"THE AGE OF SCORPIO" in serif; planet/element chips; the **Cosmic Effect** (the
round-long rule); a "this age favors" forecast; the Agekeeper named. Bright purples.
Hands into the Spring intro.

**Age closing** — cosmic curtain-down; mirrors the open inverted (constellation
*setting*, sigil faded to bronze, "has passed"). "How the age moved the board"
standings shifts; a mini stat row; the key passing to the next Agekeeper with the
next sign unknown. Valedictory. Hands to the next age open.

### 6.3 Season intros — *frame broken, shared template*

Same template, season-tuned. Each: sigil in a glowing ring, season name (serif),
uppercase tagline, an italic serif couplet, the shared Cosmic-Age strip, a preview
of the phase's **steps** (sequenced seasons) or **action groups** (free-choice
Summer), and one season-specific orienting tip.

| Intro | Palette | Preview | Key tip |
|---|---|---|---|
| Spring | green | 5 steps (set age → sign → harvest → commune → lock) | the age's sign shapes everything |
| Summer | amber | 3 action groups (Activate / Craft & build / Consort) | no order, no limit; where the round is won |
| Autumn | red-orange | 3 forge actions (Fire / Temper / Oppose) | a forging stone is exposed — ward & time it |
| Winter | blue | 4 steps (unlock → wager → limits → transit) | only cards reset; engine carries forward |

### 6.4 Main scenes & hubs

**Spring post-harvest hub** — *map hub.* Zodiac wheel (your meeple landed) + sign/harvest
result panel + horizontal step rail (age set ✓ · sign ✓ · harvested ✓ · **commune** ·
lock) + harvest in dock as "unsorted." The pause between gathering and committing.

**Summer main scene** — *state center.* Four cauldrons + codex counter; grouped
3-button bar (Craft & build / Consort open sheets; Activate is atomic gold).

**Autumn main scene** — *map center.* Crucible spiral with stones; Fire/Temper/Oppose
bar; "Manage cards" affordance. (Established early as the original pattern reference.)

**Winter hub** — *step-hub center* (no live board). Closing-rites checklist as the
central object; progress bar; steps shown done/active/locked. Frame intact, blue.
Progresses across states:
- *post-unlock:* unlock ✓, **wager** active (two CTAs: skip / place); tableau within limits.
- *post-wager:* wager ✓, **limits** active (amber); over-limit tension surfaced ("Hand 6/5").
- *post-limits:* limits ✓, **transit** active (gold); zones green-checked; resolved.

### 6.5 Summer actions

**Craft & build sheet** — 2×2 self-actions: Craft reagent / Light cauldron / Build
house / Place wards. Green.

**Consort sheet** — vertical list targeting rivals: Trade / Duel / Gambit, each with a
cost subtitle. Red.

**Craft a reagent** — *interactive.* Pick reagent (filters pool by suit; dormant
cauldrons locked + dimmed; Salt always available, no cauldron). Tap 3 matching cards
(non-matching dim to ~22%). Forge bar reads back the suit→reagent→cauldron contract.

**Forge result** — transaction strip (3 cards → 1 reagent) + full reagent stockpile
dashboard + a contextual strategy tip + two forward exits.

**Activate a Crucible card** — pick a dormant card (shown by *cauldron color*, not
hidden content). Formula auto-evaluates against your Spread (set vs. rank-sum types);
filled chips + dashed missing slots + live progress. Inline cauldron-consequence
strip. Button = full contract.

**Light cauldron (explainer)** — rules-honest: lighting is a *consequence* of
Activate, not a free action. Shows coal economy (coals on cards → spent into
cauldrons) and routes to Activate. Interactive only in Quickplay.

**Build an Astral House** — sign-derived cost ("2 Mars"), cost cards tagged with
*why* they qualify, the **three permanent benefits** (harvest source / opposition
boost / permanent cosmic effect), permanence warning under the button.

**Place ward reagents** — supply tray decrements live; existing vs. new wards shown
in different colors; +/− per card; "no ward · free to gambit" teaching line; seals a
batch. Targets both Crucible (permanent) and Adept (spent/returned) cards.

### 6.6 Consort / contests

**Trade** — two stacked trays (you give / they give); request only from a rival's
*public* cards (hidden Hand unreachable); alignment-driven ratio hint (Magnus 2:1);
"cannot trade" list (houses/wards/dormant/coals); *proposes* (needs consent).

**Duel** — 3-step wizard: target a rival's Spread card → ante your own → roll-off.
High roll steals; ante safe on win, returns to deck on loss. Pure dice.

**Gambit** — 4-step: target active card (by cauldron/type) → **pay ward fee** → stake
matching-type card → roll-off. Win swaps; loss arrests your stake (1 Salt to free).
Unwarded targets skip the fee. Arrested cards are valid targets.

**Opposition** — fee → roll to **set your sign** → alignment **tally** (vs the age)
→ resolve. Higher *total* wins (not the die). Win sends stone to Stasis; loss grants
defender +1 Besieged; tie rerolls. The tally is the strategic centerpiece.

### 6.7 Autumn forge actions

**Fire your stone** — *modal.* Position track (5→6→7, Gold beyond); reagent-fee
stepper (any type); advances **one position**; warns the stone stays exposed.

**Temper your stone** — *modal.* Stage track (Lead→Bronze→Silver→Gold→Altar);
cost is *discarding a forged Crucible card*, not reagents; advances a **whole stage**.
The final Temper at Gold is the winning move (trophy callout, hands to victory).

**Manage cards (Autumn)** — *locked review*, not rearrange (Card Lock holds till
Winter). Crucible-card states (forged/temper-ready/active/dormant) lead; full Spread
& Hand (yours to see); reagents + Arcanum. Plans Fire/Temper/Oppose.

**Leave Stasis** — two escapes, presented by which applies now: pay 2 Salt (live if
your forge spot is open) or Stasis Opposition (if occupied). Salt cost verified;
tip: progress was never lost, only position.

### 6.8 Spring / Winter step screens

**Spring Commune** — *interactive tap-to-swap.* Minor Arcana move freely between
Spread (visible, duel-exposed) and Hand (safe); aligned cards glow with +value tags;
live readout (spread alignment / safe-in-hand / verdict); Major Arcana fixed in
Arcanum; Card Lock warning.

**Spring harvest tally** — roll the wheel (meeple lands) → itemized harvest (base +
bonus + Agekeeper's Boon), highest-aspect-only rule shown.

**Winter Unlock** — tap-to-swap, final rearrange before limits.

**Winter Fateful Wager** — predict next age's sign (12-grid) + stake any cards
(Major Arcana excluded); doubles if right, lost if wrong; resolved next Spring;
genuinely optional (equal-weight Skip).

**Winter Card limits** — discard down to Spread 5 / Hand 5; over-limit cards
red-edged; wagered cards set aside (don't count); Fates return to deck, Adepts stay.

**Winter Transit** — key passes clockwise; deck reshuffles; states what *survives*
(cauldrons, houses, active crucibles, stone positions); pending wager flagged.

### 6.9 Round / game flow

**Round-open (next age)** — Agekeeper rolls the **Cosmic Age die** (distinct from
your Spring roll); age reveal with effect; **pending wager resolves** here
(double / lost). Hands into Spring.

**Victory** — *ceremonial, frame fully broken.* Gold; stone crowned on the Altar
under a halo; lead→gold progress bar; "All Hail… Magnus Alchemista" (serif); the
three win conditions itemized (4 cards, 8 stages, survived final Opposition); final
standings for all players; Chronicle / New Great Year exits.

**Chronicle** — post-game ledger. Stat row; a **race-to-the-Altar line chart**
(y-axis in Lead→…→Altar labels); age-by-age recap; key-moments list; contest
win-loss record per player.

### 6.10 Persistent overlays

**Card table** — toggle (from the hamburger) to survey *all* players' public cards
at once. Per-player rows: color-accent + name + position + alignment-threat count +
hidden-hand *count*; full Spread (aligned cards dotted) + Arcanum chips. Inline
Duel/Gambit/Trade launchers per rival (state-aware: Stasis = "cannot be opposed").
Sort by threat / turn order / arcanum-only. **Never leaks hidden info.**

### 6.11 End-of-phase CTAs (free-choice seasons)

**End Summer** — recap of the season's work + "still available" flags (e.g. unwarded
Crucible card) + "Keep working" / "End Summer" (near-equal weight).

**End Autumn** — sharper stakes: flags a **Temper still available** (gold opportunity)
and an **unwarded stone** (red risk); "this is your last chance to Fire/Temper/ward
this age"; weighting favors "Temper first" over "End anyway."

*Sequenced seasons (Spring/Winter) need no end-CTA — they flow to completion.*

---

## 7. Card System

### 7.1 Card chip (compact — used everywhere)
Token-sized (`--card-chip-w` × `--card-chip-h`, baseline ~34×48px), suit-colored fill,
gold border (or gold-bright 2px when selected/aligned), rank top + suit icon below.
An alignment dot (top-right) or +value tag when it matches the current age. The
atomic unit of every tableau, hand, spread, and tray.

### 7.2 Minor Arcana inspect modal
Tap any card to open. Full-size tarot-style card (mirrored rank corners, centered
suit) → **aspects** (element / planet / suit, three tiles) → **alignment this age**
(live computed: matched aspect + points, with the alternatives noted) → **effect**
(most pips have none — stated honestly, redirected to use) → **"good for"** tags
(crafting / sets / alignment). Age-reactive: the same card shows different alignment
value in different ages. *Court-card variant needed* for conditional Wild Suits.

### 7.3 Adept received modal *(Major Arcana — a tool you wield)*
Purple. "You drew an Adept." Tarot card with Roman numeral + eye marker → **its
power** → **how Adepts work** (placed face-up active / max 2 active / gambitable —
ward it) → open-slot note → **two choices**: Place active / Hold inactive
(deliberative — you decide).

### 7.4 Fate received modal *(Major Arcana — an event that befalls you)*
Blue, cosmic. "The Fates intervene." Structurally *opposite* the Adept: shows a
**fired effect now** (+/− rows, e.g. +3 draw / −1 reagent), not a power to wield;
**single "Accept your fate"** button (no choice — it already resolved); note that
Fates resolve on draw and stay face-up after. Effect varies per Fate (boon / penalty
/ table-wide).

### 7.5 Card type behavior summary

| Type | Where it lives | Movable? | On receipt | Inspect |
|---|---|---|---|---|
| Minor Arcana | Spread / Hand | Yes, when unlocked (Commune, Winter) | silent into hand/spread | full inspect modal |
| Adept (Major) | Arcanum | No (place/hold) | reveal modal, you choose | (own reveal) |
| Fate (Major) | Arcanum | No (auto face-up) | reveal modal, auto-resolves | (own reveal) |

---

## 8. Motion & Feedback

- **Dice:** ~12 tumble frames @ 70ms, then settle; value announced; ties reroll.
- **Wheel roll (Spring/Opposition sign):** segments flicker ~14 frames, meeple
  settles on the landed sign; aspect match announced.
- **Token movement (results):** stones/meeples *slide* between positions rather than
  teleport — the board change is shown, not just stated.
- **Forge/transmute moments** deserve a flourish (cards dissolving, salt
  precipitating, stone glowing gold) — these are where theme earns its keep. Keep
  short (<1s) so repeat actions don't drag.
- **Selection:** gold-bright 2px border + slight brightness lift. **Disabled/locked:**
  dim to ~0.4–0.6 + lock icon. **Over-limit/error:** red edge, never a blocking modal.

---

## 9. Accessibility Notes

- Alignment never relies on color alone — a corner **dot** + the numeric **value**
  carry the signal alongside the hue.
- Line charts use distinct **point shapes** (circle/square/triangle) per series in
  addition to color.
- Every visual scene carries an off-screen `h2` summary describing its content for
  screen readers.
- Loading messages for serious/none-here; kept boring for any heavy subject matter.

---

## 10. Open Items / To Build

Tracked design questions and unbuilt screens noted during the workshop:

- **Court-card inspect variant** — conditional Wild Suits under certain ages.
- **Set-awareness assist** — flag near-complete Activation/formula sets in Commune,
  Activate, and the inspect modal so players don't cannibalize them.
- **Direct card-tap dueling** — tap a specific Spread card in the card table to duel
  for *that* card.
- **Multiplayer turn-handoff** — pass-and-play / async "your turn" + hidden-hand
  protection between players.
- **Tutorial / onboarding** — the guided first game ("How to play").
- **Codex** — searchable reference for cards, the 12 ages & effects, states, glossary.
- **Besieged Bonus state** — round-level tracking of stacking opposition-defense bonus.
- **"No re-gambit this round"** — lock a defender after a failed gambit.
- **Mode-awareness** — Quickplay/Magnus rule deltas must propagate to Build cost,
  Trade ratio, Opposition tally, Light-cauldron, etc.
- **Hub-style consistency** — align Spring (wheel + rail) and Winter (checklist)
  hub treatments if desired.
- **Skippable intros** — a veteran toggle for the season/age ceremonies.

---

*Kismeta: Alchemists of the Great Year © 2026 Goodmagik Games. Production UI
specification — responsive full-viewport layout; 380×844 design baseline.*
