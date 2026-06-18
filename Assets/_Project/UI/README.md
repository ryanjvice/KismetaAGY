# Kismeta UI (Assets/_Project/UI)

Unity UI Toolkit assets ported from `Docs/wireframes/`.

## Layout

| Path | Contents |
|------|----------|
| `USS/` | Shared `Kismeta.uss` + per-screen stylesheets |
| `UXML/batchN/` | 35 screen layouts (batches 1–7) |
| `Scripts/` | `Kismeta.UI` assembly — framework + setup types |
| `Settings/` | `KismetaPanelSettings.asset` (380×844 **reference** only) |
| `Fonts/` | Import serif + Tabler icon fonts (see README) |
| `Editor/` | Menu items under **Kismeta → UI** |

## Phase 0b complete — responsive foundation

- **Full-bleed layout:** `.screen` / `.sheet` fill the viewport (no fixed 380px width)
- **`ViewportLayout`:** safe-area padding, `--ui-scale` tokens, overlay layer for modals/sheets
- **`UiShellHost`:** alias for `ViewportLayout` (backward compatible)
- **`UiResponsiveTest`:** Title → setup sheet (New game) / inspect modal (Codex) smoke demo
- **PanelSettings:** Scale With Screen Size, 380×844 reference, match 0.5
- **Docs synced:** `UI_styleGuide.md`, `Docs/wireframes/Kismeta.uss`

## Responsive rules (summary)

| Token / class | Purpose |
|---------------|---------|
| `--ui-scale` | Informational only on `ViewportLayout`; panel scaling via PanelSettings |
| `--touch-min` | Minimum 44px touch targets |
| `--card-chip-w/h` | Scaled card chips |
| `.overlay-layer` | Full-screen scrim for modals |
| `.overlay-layer--sheet` | Bottom-anchored sheets |
| `.viewport--tablet` | Shortest side ≥ 600dp |

**Test in Game view:** 390×844, 428×926, 768×1024 portrait.

## Quick test

1. Open Unity and let scripts compile.
2. **Kismeta → UI → Configure Panel Settings (Responsive)** (once, if PanelSettings exists)
3. **Kismeta → UI → Create UI Test Scene**
4. Press Play — full-bleed Title; **New game** opens setup sheet; **Codex** opens card inspect modal.

## Next: Phase 1

Import `ScreenController`, build `ScreenRouter`, `GamePresenter`, `CommandBridge`, and reusable components (all using responsive tokens).
