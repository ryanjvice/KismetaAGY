# Batch 2 — Intros &amp; ceremonies (flow-map #7–10, 16, 28, 37, 45–46)

The frame-broken threshold screens: the four season intros (one shared template) and
the age open/close ceremonies that bracket each round.

## Screens in this batch

| # | Screen | UXML | C# | Per-screen USS |
|---|--------|------|----|----------------|
| 7 | Round-open (Agekeeper rolls) | `RoundOpen.uxml` | `RoundOpenController` | `AgeCeremony.uss` |
| 8–9 | Age reveal + Cosmic Effect + wager resolve | `AgeOpening.uxml` | `AgeOpeningController` | `AgeCeremony.uss` |
| 10 | Spring intro | `SpringIntro.uxml` | `SeasonIntroController` | `SeasonIntro.uss` |
| 16 | Summer intro | `SummerIntro.uxml` | `SeasonIntroController` | `SeasonIntro.uss` |
| 28 | Autumn intro | `AutumnIntro.uxml` | `SeasonIntroController` | `SeasonIntro.uss` |
| 37 | Winter intro | `WinterIntro.uxml` | `SeasonIntroController` | `SeasonIntro.uss` |
| 45–46 | Age-close + standings recap | `AgeClosing.uxml` | `AgeClosingController` | `AgeCeremony.uss` |

Flow-map #7, #8, #9 are folded: #7 is the die-roll (`RoundOpen`), #8–9 are the reveal +
wager resolution (`AgeOpening`). #45–46 likewise fold into `AgeClosing`.

## Shared template approach

**Season intros** all use one UXML structure + `SeasonIntro.uss`. The season's hue is a
root modifier class — `intro--spring` / `intro--summer` / `intro--autumn` / `intro--winter` —
which drives the hero bg, sigil, name, tagline, and tagline colors. One controller,
`SeasonIntroController`, serves all four; set its `Season` field and `OnBegin` hook.

The intros differ only in: sigil glyph, copy, the preview rows (Spring/Winter show
numbered **steps**; Summer/Autumn show **action groups**), and the tip semantic color
(ok / caution / danger / ok).

**Age ceremonies** share `AgeCeremony.uss`. `AgeOpening` (rising) and `AgeClosing`
(setting) mirror each other via `ceremony-hero--open` / `--close` and
`ceremony-sigil--open` / `--close` modifiers (the close variant is smaller + dimmed).

## NATIVE NOTES in this batch

- **Cosmic Age die** (`RoundOpen`) — animate `#age-die` (sprite-swap coroutine or 3D
  die); on settle, set `die-face`, resolve any pending wager, push `AgeOpening`.
- **Starfield + constellation + sigil glow** (both ceremonies) — background sprites on
  `.ceremony-hero` and the sigil; constellation as sprite/LineRenderer; glow via 9-slice.
- **Season intro motes** (seeds/sun/leaves/snow) — optional particle overlay or sprite
  on `.intro-hero`; purely decorative.

## Controller usage

```csharp
// Any season intro:
var intro = GetComponent<SeasonIntroController>();
intro.Season = Season.Autumn;
intro.OnBegin = () => router.Push("AutumnMain");

// Age opening, after the die settles:
var open = GetComponent<AgeOpeningController>();
open.SetAge("Scorpio", "Mars", "Water",
            "Court Cups are a Wild Suit",
            "every Court Cup counts as any suit you need", "Iolanthe");
open.OnEnter = () => router.Push("SpringIntro");
```

## Status

#7–10, 16, 28, 37, 45–46 layout-complete with controllers. Die animation, starfield
sprites, and the live standings recap rebuild are integration work. Next: **Batch 3 —
Spring &amp; Winter steps** (#11–15, 38–44), which also back-fills the C# for the Spring
and Winter hubs already built.
