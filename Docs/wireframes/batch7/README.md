# Kismeta — Unity UI Toolkit port

> **STATUS: complete.** All 55 screens (flow-map #1–55) are converted to UXML + C#
> controllers, organized into 7 batches. See `MANIFEST.md` for the per-screen status
> table and `Batch{1-7}_README.md` for each batch. This file covers project-level setup.


UXML + USS translation of the main game-screen mockups, ready to drop into a Unity
project using **UI Toolkit** (UIElements). This is a *layout and styling* port — the
dynamic, painted, and interactive parts are documented below as native work to do in C#.

## What's in here

```
kismeta-uitoolkit/
├── USS/
│   └── Kismeta.uss            ← all design tokens + reusable component classes
└── UXML/
    ├── SetupScreen.uxml        ← players + difficulty
    ├── SpringHub.uxml          ← zodiac-wheel hub + step rail
    ├── SummerMainScene.uxml    ← cauldron-state center + 3-button bar
    ├── AutumnMainScene.uxml    ← crucible-spiral center + Fire/Temper/Oppose
    └── WinterHub.uxml          ← closing-rites step hub
```

## Setup in Unity (production)

Wireframe sources live under `Docs/wireframes/`; shipped assets are under
`Assets/_Project/UI/`. Production UI uses a **single-host** architecture:

1. **`AppShell.uxml`** is the only UIDocument Source Asset on `GameBootstrap` (fixed
   shell with `content-layer` + `overlay-layer`).
2. **`ScreenRouter`** loads batch UXML into `content-layer` at runtime — do not assign
   individual screens as the UIDocument Source Asset.
3. **`Kismeta.uss`** canonical copy: `Assets/_Project/UI/USS/Kismeta.uss`. Each UXML
   imports it via `<engine:Style src="../../USS/Kismeta.uss" />` (adjust depth per folder).
4. **PanelSettings:** Scale With Screen Size, reference 380×844, match ≈ 0.5. Screens
   are full-bleed via `.screen-host` + flex stretch (not a centered 380px card).

**Editor menus:** Kismeta → UI → Configure Panel Settings / Wire Bootstrap UI References /
Setup Bootstrap Scene.

Full bootstrap steps, architecture, and troubleshooting:
[`Assets/_Project/UI/README.md`](../../../Assets/_Project/UI/README.md).
Design tokens and layout rules: [`UI_styleGuide.md`](../UI_styleGuide.md) §4.

## What translated cleanly (USS is a CSS subset)

- **Flexbox layout** — UI Toolkit is flexbox-native; `flex-direction`, `flex-grow`,
  `align-items`, `justify-content` all map 1:1.
- **Color tokens** — defined as USS custom properties on `:root`; reference with
  `var(--gold)` etc. exactly like CSS.
- **Borders, border-radius, padding, margin, font-size, opacity** — all supported.
- **Component classes** — `.card-chip`, `.btn`, `.rival`, `.steprail`, etc. reused
  across screens, same BEM-ish naming as the mockups.

## What needs native (C#) work — search the UXML for "NATIVE NOTE"

USS does **not** support: CSS gradients (beyond limited cases), `box-shadow`, inline
SVG, pseudo-elements, or `calc()`. And UXML/USS is purely presentational — no logic.
So these parts are stubbed with placeholder hosts and flagged inline:

| Mockup feature | UXML placeholder | Build in Unity as |
|---|---|---|
| **Zodiac wheel** (Spring) | `#wheel-host` | `VisualElement.generateVisualContent` + `Painter2D` arcs, **or** a sprite wheel + an absolutely-positioned meeple token rotated by sign index. |
| **Crucible spiral** (Autumn) | `#board-stage` | Painter2D custom paint, **or** sprite background + child VisualElements per stone, positioned absolutely. |
| **Four cauldrons** (Summer) | `#cauldron-stage` | Can be pure USS: four `.cauldron` circles positioned N/E/S/W, toggle `.cauldron--lit` / `.cauldron--dormant` in C#. No painting needed. |
| **Dice roll-off** (contests) | (not in these screens) | Animate via `experimental.animation` or a coroutine swapping pip layouts; settle on a value from your RNG. |
| **Radial gradient backgrounds** (`.stage`, ceremonies) | flat fill | Flat color is fine; for the glow, use a 9-slice sprite or a subtle background `Image`. |
| **Tabler icons** | `.ti-icon` Labels with `&#x....;` codepoints | Import the Tabler TTF as a Font asset, assign via `-unity-font-definition` (see USS §8), **or** replace each with an `Image` + sprite. |
| **All interactivity** (`onclick`, game logic) | named Buttons (`#fire-btn`, etc.) | Query by name in C# (`root.Q<Button>("fire-btn")`) and wire `.clicked += ...`. |

## Wiring buttons in C# (pattern)

Every interactive element has a `name`. Example controller:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class AutumnSceneController : MonoBehaviour
{
    [SerializeField] UIDocument doc;

    void OnEnable()
    {
        var root = doc.rootVisualElement;
        root.Q<Button>("fire-btn").clicked   += OnFire;
        root.Q<Button>("temper-btn").clicked += OnTemper;
        root.Q<Button>("oppose-btn").clicked += OnOppose;
        root.Q<Button>("manage-cards-btn").clicked += OnManageCards;
        root.Q<Button>("menu-btn").clicked   += OpenCardTable;
    }

    void OnFire()   { /* open Fire modal */ }
    void OnTemper() { /* open Temper modal */ }
    // ...
}
```

## Populating dynamic content (pattern)

The card strips, rival stats, harvest counts, and step states are static samples in
the UXML for layout. Build them from game state at runtime:

```csharp
// rebuild the spread strip from data
var strip = root.Q<VisualElement>("spread-strip");
strip.Clear();
foreach (var card in player.Spread)
{
    var chip = new VisualElement();
    chip.AddToClassList("card-chip");
    chip.AddToClassList($"card-chip--{card.Suit.ToString().ToLower()}");
    if (card.AlignsTo(currentAge)) chip.AddToClassList("card-chip--selected");
    var rank = new Label(card.RankLabel);
    rank.AddToClassList("card-chip__rank");
    chip.Add(rank);
    strip.Add(chip);
}
```

Toggle step-rail and cauldron states by swapping classes
(`AddToClassList` / `RemoveFromClassList`) — e.g. `step__dot--locked` →
`step__dot--active` → `step__dot--done` as the player advances.

## Notes & gotchas

- **`white-space: normal`** is set on labels that must wrap (notes, tips); UI Toolkit
  defaults to no-wrap.
- **`-unity-text-align`** replaces CSS `text-align`; `-unity-font-style` replaces
  `font-weight`/`font-style`.
- **Colors are `rgb()` / `rgba()`** — USS doesn't take hex shorthand in custom
  properties as reliably; everything here is `rgb()` for safety.
- **No `:last-child` margin tricks needed?** They're used here and supported, but if
  you build strips in C#, just set margins per-element instead.
- The **other ~50 screens** (modals, contests, ceremonies, results, the rest of the
  step screens) follow the exact same patterns — reuse `Kismeta.uss` and the component
  classes; each new screen is a new UXML assembled from the same building blocks.

---

*Kismeta: Alchemists of the Great Year © 2026 Goodmagik Games.*
