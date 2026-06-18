# Kismeta UI (Assets/_Project/UI)

Unity UI Toolkit assets ported from `Docs/wireframes/`.

## Layout

| Path | Contents |
|------|----------|
| `USS/` | Shared `Kismeta.uss` + per-screen stylesheets |
| `UXML/batchN/` | 35 screen layouts (batches 1–7) |
| `Scripts/` | `Kismeta.UI` assembly — framework + setup types |
| `Settings/` | `KismetaPanelSettings.asset` (380×844 reference) |
| `Fonts/` | Import serif + Tabler icon fonts (see README) |
| `Editor/` | Menu items under **Kismeta → UI** |

## Phase 0 complete

- Folder tree and asset import
- `Kismeta.uss` reviewed for Unity 6 (tokens on `:root`, `.screen`, `.sheet`, `.kismeta-root`)
- UXML `Style src` paths fixed to `../../USS/`
- `Kismeta.UI.asmdef` references Core + Data
- `UiSetupConfig` + mapper uses `Kismeta.Core.Domain.GameMode`
- `UiShellHost` + PanelSettings + editor test scene menu

## Quick test

1. Open Unity and let scripts compile.
2. **Kismeta → UI → Create UI Test Scene**
3. Press Play — Title screen should render (default font until serif is imported).

## Next: Phase 1

Import `ScreenController`, build `ScreenRouter`, `GamePresenter`, `CommandBridge`, and reusable components.
