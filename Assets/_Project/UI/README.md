# Kismeta UI (Assets/_Project/UI)

Unity UI Toolkit assets ported from `Docs/wireframes/`. This folder is the **production** UI; wireframe docs under `Docs/wireframes/` are design references and may lag the shipped layout.

## File layout

| Path | Contents |
|------|----------|
| `USS/` | Shared `Kismeta.uss` (canonical) + per-screen stylesheets |
| `UXML/batch1/` | Title, SetupSheet, Join, Resume, Codex, AgekeeperContest |
| `UXML/main/` | Season main scenes: SpringHub, SummerMain, AutumnMain, WinterHub |
| `UXML/batch2/` | Ceremony screens: RoundOpen, AgeOpening, season intros, AgeClosing |
| `UXML/batch3/` | Spring/Winter step screens: Commune, WinterUnlock, FatefulWager, CardLimits |
| `UXML/batch4/` | Summer action overlays: SummerSheets, CraftReagent, ActivateCard, BuildHouse, PlaceWards, EndSummer |
| `UXML/shell/` | `AppShell.uxml`, `GameplayHud` (dev fallback), `WaitingHud` |
| `Scripts/Framework/` | ViewportLayout, ScreenRouter, GamePresenter, CommandBridge |
| `Scripts/Controllers/` | Shell + season main scene controllers |
| `Scripts/Components/` | CardChipFactory, RivalStripBuilder, ReagentStepper |
| `Scripts/Setup/` | `UiSetupConfig`, mapper to Core `GameMode` |
| `Settings/` | `KismetaPanelSettings.asset` (380×844 **reference** only) |
| `Editor/` | Menu items under **Kismeta → UI** |

## Architecture (single-host model)

Production UI uses **one `UIDocument` on `GameBootstrap`**, not one per screen. The wireframe prototype pattern (each screen with its own UIDocument + Source Asset) is obsolete.

```mermaid
flowchart TB
  GameBootstrap --> GamePresenter
  GamePresenter --> ScreenRouter
  ScreenRouter --> ViewportLayout
  ViewportLayout --> UIDocument
  UIDocument --> AppShellUXML["AppShell.uxml (fixed Source Asset)"]
  AppShellUXML --> contentLayer["content-layer"]
  ScreenRouter -->|"SetScreen()"| contentLayer
  ViewportLayout --> overlayLayer["overlay-layer (sheets/modals)"]
```

| Piece | Role |
|-------|------|
| **`AppShell.uxml`** | The **only** UIDocument Source Asset. Provides `content-layer` + `overlay-layer`. Never assign TitleScreen or ad-hoc layouts here. |
| **`ScreenRouter`** | Maps screen ids to UXML; calls `ViewportLayout.SetScreen()` to swap children in `content-layer`. |
| **`ViewportLayout`** | Safe-area padding, viewport classes, modal/sheet overlays, full-bleed stretch for instantiated screens. |
| **`GamePresenter`** | Ceremony-first routing via `CeremonyGate`; hint-based step screens (`Commune`, `CardLimits`); season hubs when human pending; `WaitingHud` for AI turns. |
| **`ScreenController`** | Attach/detach/refresh on the host GameObject when the router loads a screen. |
| **`CommandBridge`** | Submits `IGameCommand` to `HotSeatController` (e.g. Pass). |
| **`GameBootstrap`** | `Awake`: sets AppShell on UIDocument; `Start`: Title → Setup sheet → Begin starts the loop. |

Controllers (`TitleScreenController`, etc.) live on the **same GameObject** as `ScreenRouter`. Call `ScreenRouter.RefreshControllers()` after adding components at runtime.

## Phase 6 complete — Batch 4 Summer actions

- **Overlay navigation:** Summer sub-screens use `ViewportLayout` overlays via `SummerOverlayHost`; `SummerMain` stays the active `ScreenRouter` screen during `ActionHint.SummerAction`.
- **Hub:** Craft & build sheet, Consort sheet (Trade/Duel/Gambit disabled — Batch 5), atomic **Activate**, **Pass** → **End Summer** confirmation modal.
- **Actions wired:** `CraftReagentCommand`, `ActivateCrucibleCommand`, `BuildAstralHouseCommand`, `PlaceCardWardCommand`, `PassCrucibleActionCommand`.
- **Deferred:** Trade (#26), Duel/Gambit contests (#33, 47–49), Card Table (`menu-btn` stub).

**Summer play-test path:**

Reach **SummerMain** → **Craft & build** → craft reagent (forge result) → **Activate** dormant crucible → **Build** astral house (planet-matching card) → **Place wards** (one ward per action) → **Pass** → **End Summer** → repeat passes until **AutumnIntro**

## Phase 5 complete — Batch 3 Spring & Winter steps

- **Hint routing:** `ActionHint.Commune` → **Commune**; `ActionHint.DiscardToLimit` → **CardLimits**; `ActionHint.WinterAction` → **WinterHub** (sub-screens opened from hub CTAs).
- **Spring:** **SpringHub** shows zodiac sign + harvest tally after auto roll/harvest; **Commune** tap-swap → `CommuneCommand` → auto Card Lock.
- **Winter:** **WinterHub** → **WinterUnlock** (per-tap `WinterMoveCardCommand`) / **FatefulWager** (`PlaceFatefulWagerCommand` or Pass skip) → **CardLimits** (`DiscardToLimitCommand`) → **AgeClosing**.
- **Deferred:** Full `SpringRollHarvest` screen (display-only on SpringHub); `TransitAge.uxml` (transit via AgeClosing ceremony).

**Full-round play-test path:**

Title → setup → agekeeper → RoundOpen → AgeOpening → SpringIntro → **SpringHub** (sign + harvest) → **Commune** → Summer → Autumn → WinterIntro → **WinterHub** → unlock / wager / pass → **CardLimits** → **AgeClosing** → next round

## Phase 4 complete — Batch 2 ceremonies

- **CeremonyGate:** `GameLoop` pauses at `RoundOpen`, `AgeOpening`, four season intros, and `AgeClosing`; UI calls `Complete()` / `CompleteWithCommand()` to resume.
- **Flow:** Agekeeper contest → **RoundOpen** (roll) → **AgeOpening** → **SpringIntro** → Spring gameplay → season intros at Summer/Autumn/Winter → **AgeClosing** → next round.
- **AI agekeeper:** Non-human agekeeper auto-applies `RollCosmicAgeCommand`; humans still see **AgeOpening** and intros.
- **TransitAge** moved to **AgeClosing** CTA (`next-age-btn`).

## Phase 3 complete — Season main scenes

- **Routing:** `GamePresenter` maps `Season` → `SpringHub` / `SummerMain` / `AutumnMain` / `WinterHub` on human turns; `WaitingHud` while AI decides.
- **Bind:** Status bar, rivals strip, spread dock, step rails (Spring/Winter), cauldrons (Summer), stone label (Autumn) from `GamePublicView`.
- **Pass:** Summer, Autumn, and Winter main scenes wire **Pass** via `CommandBridge` during free-action phases (Summer opens **End Summer** modal first).
- **Summer actions:** Craft, activate, build, ward overlays via `SummerOverlayHost`.
- **Stubs:** Card table (`menu-btn`); Consort contests until Batch 5.
- **`GameplayHud`** remains registered for dev fallback but is no longer used in normal play routing.

## Phase 2 complete — Batch 1 shell

- **Title** → New game (setup sheet), **Resume**, **Join**, **How to play** (Codex → Terms), **Codex**
- **Join** — room-code entry; back returns to title (multiplayer stub)
- **Resume** — empty-state list; persistence stub
- **Codex** — tabbed reference + search filter (static glossary samples)
- **Setup sheet** → agekeeper contest → game loop
- **Agekeeper contest** — zodiac die roll; winner passed to session setup

## Phase 1 — UI spine (foundation)

- Title → setup → agekeeper → season main scenes / `WaitingHud`
- Setup sheet uses `SetupSheetState`; maps wireframe "Magnus" to `GameMode.MagnusAlchemist`
- IMGUI `GameDebugUI` remains available via **Debug Ui Fallback** on `GameBootstrap`

## Bootstrap setup

Open `Bootstrap.unity` (or your play scene with `GameBootstrap`), then:

| Menu item | Purpose |
|-----------|---------|
| **Kismeta → UI → Configure Panel Settings (Responsive)** | Scale With Screen Size (380×844 ref), match 0.5, opaque `--panel-dark` clear color |
| **Kismeta → UI → Wire Bootstrap UI References** | Assigns AppShell, screens, PanelSettings on `GameBootstrap` |
| **Kismeta → UI → Setup Bootstrap Scene** | Adds full UI stack to `GameBootstrap`, clears legacy Initial Screen |

**Inspector checklist after setup:**

- UIDocument **Source Asset = AppShell** (not TitleScreen, not "Root Layout")
- ViewportLayout **Initial Screen = None**
- GameBootstrap **Use Production Ui** = on
- **Save the scene** (`Ctrl+S`)

**Play-test:** Title → New game → Start Game → agekeeper contest → **RoundOpen** (roll die) → AgeOpening → SpringIntro → **SpringHub** → **Commune** → season main scenes; season intros between seasons; Winter unlock/wager/limits → **AgeClosing** at Winter end.

## Full-bleed / responsive rules

380×844 is the **design baseline** for proportions and PanelSettings reference resolution — not a layout width cap.

**Flex chain** (every new screen must participate):

```
panel root → .kismeta-root → .app-shell → .content-layer → .screen-host → .screen
```

- **`Instantiate()` shrink-wraps** UXML in a wrapper. `ViewportLayout.SetScreen()` adds `.screen-host` and inline stretch styles. Do not rely on `height: 100%` alone on `.screen`.
- **Panel root** is stretched in `EnsurePanelRootFillsViewport()`.
- **USS canonical copy:** `Assets/_Project/UI/USS/Kismeta.uss` (sync `Docs/wireframes/Kismeta.uss` when changing shared tokens).
- **No runtime USS custom-property writes** — Unity does not support writing USS variables from C#. PanelSettings scales typography; `ViewportLayout` applies safe-area padding and `.viewport--*` classes only.

| Token / class | Purpose |
|---------------|---------|
| PanelSettings | Scale With Screen Size (380×844 reference), opaque clear color |
| `ViewportLayout` | Safe area, overlays, screen stretch |
| `.screen-host` | UXML instantiate wrapper — must fill content layer |
| `.viewport--tablet` | Shortest side ≥ 600dp |

**Test in Game view:** 390×844, 428×926, 768×1024 portrait.

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| UI flashes then blue screen | TitleScreen or custom layout as UIDocument Source Asset; tree reload wipes `content-layer` | Source Asset = AppShell only; run **Setup Bootstrap Scene** |
| UI centered with blue bars top/bottom | `TemplateContainer` from `Instantiate()` not stretching | `.screen-host` + `StretchToContentLayer()` in ViewportLayout (automatic if using router) |
| Title buttons do nothing | Title menu not wired before session exists | `GamePresenter.InitializeForTitle()` at startup |
| Missing script on controllers | Components added while scripts had compile errors | Remove broken components; re-run **Setup Bootstrap Scene** |
| `GameMode.Magnus` compile error | Wireframe shorthand vs Core enum | Use `GameMode.MagnusAlchemist` — see `UiSetupConfigMapper` |
| Font `MissingAssetReference` warnings | Serif/icon fonts not imported | See `Fonts/README.md` — non-blocking, falls back to default UI font |

## Design reference

Visual tokens, components, and per-screen specs: [`Docs/wireframes/UI_styleGuide.md`](../../Docs/wireframes/UI_styleGuide.md).

## Next: Phase 5 — Batch 3 Spring and Winter steps

Wire Commune sheet, Spring harvest/roll UI, and Winter unlock/wager/limits/transit screens to `CommandBridge`.
