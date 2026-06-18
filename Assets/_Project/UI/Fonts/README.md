# Kismeta UI Fonts

Import these font assets into this folder before ceremonial screens render with the correct typography.

## Required

| Asset | Source | USS reference |
|-------|--------|---------------|
| `Kismeta-Serif.asset` | Serif display font for wordmarks and ceremonial titles (e.g. Cinzel, Cormorant Garamond) | `TitleScreen.uss`, `AgeCeremony.uss`, `SeasonIntro.uss`, `Contests.uss`, `EndAndOverlays.uss` |
| `tabler-icons.asset` | [Tabler Icons](https://tabler.io/icons) TTF converted to Unity Font asset | `.ti-icon` in `Kismeta.uss` |

## Import steps

1. Drop `.ttf` / `.otf` files into this folder.
2. Select each font in the Project window and create a **Font Asset** (UI Toolkit) if needed.
3. Ensure asset names match the paths in USS: `Kismeta-Serif.asset`, `tabler-icons.asset`.
4. Until fonts are imported, screens fall back to the default UI font.

## Icons alternative

If the Tabler font is unavailable, replace `Label` icon elements with `Image` + sprite sheets in UXML (one-time pass during Phase 1).
