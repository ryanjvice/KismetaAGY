# Kismeta UI (Assets/_Project/UI)

Unity UI Toolkit assets ported from `Docs/wireframes/`.

## Layout

| Path | Contents |
|------|----------|
| `USS/` | Shared `Kismeta.uss` + per-screen stylesheets |
| `UXML/batchN/` | 35 screen layouts (batches 1–7) |
| `UXML/shell/` | Phase 1 shell screens (GameplayHud, WaitingHud) |
| `Scripts/Framework/` | ViewportLayout, ScreenRouter, GamePresenter, CommandBridge |
| `Scripts/Controllers/` | Title, GameplayHud, WaitingHud controllers |
| `Scripts/Components/` | CardChipFactory, RivalStripBuilder |
| `Settings/` | `KismetaPanelSettings.asset` (380×844 **reference** only) |
| `Editor/` | Menu items under **Kismeta → UI** |

## Phase 1 complete — UI spine

- **`ScreenController`** — attach/detach pattern for router-driven screens
- **`ScreenRouter`** — content-layer navigation + setup sheet overlay
- **`GamePresenter`** — routes by `GameLoop` hint / pending human controller
- **`CommandBridge`** — submits commands to `HotSeatController` (Pass, etc.)
- **`GameBootstrap`** — production UI path: Title → Setup → start loop on Begin
- **Placeholder HUD** — `GameplayHud` / `WaitingHud` until batch screens land

## Bootstrap setup

1. Open `Bootstrap.unity` (or your play scene with `GameBootstrap`).
2. **Kismeta → UI → Configure Panel Settings (Responsive)**
3. **Kismeta → UI → Wire Bootstrap UI References**
4. Enable **Use Production Ui** on `GameBootstrap`; optional **Debug Ui Fallback** for IMGUI overlay.
5. Press Play → New game → Begin → gameplay HUD with Pass during free-action phases.

## Responsive rules (summary)

| Token / class | Purpose |
|---------------|---------|
| PanelSettings | Scale With Screen Size (380×844 reference) |
| `ViewportLayout` | Safe area, app shell, overlays |
| `.viewport--tablet` | Shortest side ≥ 600dp |

**Test in Game view:** 390×844, 428×926, 768×1024 portrait.

## Next: Phase 2 — Batch 1 Shell wiring

Dedicated setup screen, Join/Resume, Codex data binding, and replacing the gameplay placeholder with real season flows.
