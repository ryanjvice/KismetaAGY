# Batch 1 — Shell (flow-map #1–6)

The front-of-app region: title, setup, lobby, and reference entry points. This batch
also establishes the file conventions used by every later batch, and back-fills the
C# controllers for the five proof-of-pattern main scenes.

## Screens in this batch

| # | Screen | UXML | C# controller | Per-screen USS |
|---|--------|------|---------------|----------------|
| 1 | Title screen | `TitleScreen.uxml` | `TitleScreenController.cs` | `TitleScreen.uss` |
| 2 | Setup — compact sheet | `SetupSheet.uxml` | `SetupSheetController.cs` (in `ShellControllers.cs`) | — |
| 3 | Setup — dedicated | `SetupScreen.uxml` | `SetupController.cs` | — |
| 4 | Join a game | `JoinScreen.uxml` | `JoinController.cs` (in `ShellControllers.cs`) | — |
| 5 | How to play | *(uses Codex shell)* | — | — |
| 6 | Codex | `CodexScreen.uxml` | `CodexController.cs` (in `ShellControllers.cs`) | — |

Resume (#4's sibling) is a trivial saved-game list — omitted as boilerplate; reuse the
Join shell with a list instead of a code field.

## Also delivered (back-fill)

- `ScreenController.cs` — base class all controllers inherit (query helpers, state-class swap).
- `MainSceneControllers.cs` — C# for the 5 pre-existing scenes: Autumn, Summer, Spring hub,
  Winter hub (Setup dedicated already covered by `SetupController`).

## Conventions (apply to all batches)

**File naming.** `PascalCaseScreen.uxml` / `PascalCaseScreenController.cs` /
`PascalCaseScreen.uss` (only when a screen needs unique styling). Small related
controllers may share one file (e.g. `ShellControllers.cs`).

**Every controller** derives from `ScreenController`, overrides `Wire()` to hook
buttons, and optionally `Bind()` to push game state. In **production**, controllers
attach to the **`GameBootstrap` UI host** (same GameObject as `ScreenRouter` and
`ViewportLayout`) — not to a per-screen `UIDocument`. The router calls
`AttachTo(screenRoot)` when a screen UXML is loaded into `content-layer`.

The wireframe prototype in `Docs/wireframes/batch1/ScreenController.cs` assumed one
UIDocument per screen; the shipped base class is
`Assets/_Project/UI/Scripts/Framework/ScreenController.cs`.

**Naming hooks.** Every interactive element in UXML has a `name`. Controllers query by
that name (`Btn("new-game-btn")`). Keep names stable — they are the contract between
UXML and C#.

**Navigation** is exposed as `System.Action` fields on each controller (e.g.
`OnNewGame`, `OnBegin`). Wire these from **`GamePresenter`** (or `ScreenRouter`);
controllers don't assume how navigation works.

**Setup enums.** Wireframe docs use "Magnus"; Core uses `GameMode.MagnusAlchemist`.
Map via `UiSetupConfig` / `UiSetupConfigMapper` in `Assets/_Project/UI/Scripts/Setup/`.

**Styling.** Shared classes live in `Assets/_Project/UI/USS/Kismeta.uss` (imported by
every UXML). A screen gets its own `.uss` only when it has genuinely unique styling —
so far only `TitleScreen.uss` (the ceremonial hero treatment).

## NATIVE NOTES in this batch

- **Title hero** — the radial gold glow + sacred-geometry backdrop were CSS gradients;
  in Unity use a background sprite/Image on `#title-hero` and the serif font asset for
  the wordmark and sigil (`-unity-font-definition` paths are placeholders).
- **Setup sheet** — present as an overlay with a dimmed scrim; animate the rise
  (translateY) in C# via `experimental.animation`.
- **Codex** — content is data-driven; bind `#codex-search` and populate `#codex-list`
  from a ScriptableObject/JSON of cards, ages, states, glossary. Rows shown are samples.

## Wiring example (production)

```csharp
// On GameBootstrap (UIDocument + ScreenRouter + GamePresenter + controllers):
// GamePresenter.InitializeForTitle() wires title menu at startup:

var title = router.GetController<TitleScreenController>(ScreenIds.Title);
title.OnNewGame   = () => router.ShowSetupSheet(onBegin: () => presenter.NotifySetupBegin(config));
title.OnResume    = () => Debug.Log("[UI] Resume not implemented.");
title.OnJoin      = () => router.GoTo(ScreenIds.Join);  // Phase 2
title.OnHowToPlay = () => Debug.Log("[UI] How to play not implemented.");
title.OnCodex     = () => router.GoTo(ScreenIds.Codex); // Phase 2
```

See [`Assets/_Project/UI/README.md`](../../../Assets/_Project/UI/README.md) for bootstrap
setup and troubleshooting.

## Status

All of #1–6 are layout-complete with controllers. Card/age/state content in the Codex
and the actual navigation router are left as integration work. Next: **Batch 2 —
intros & ceremonies** (#7–10, 16, 28, 37, 45–46).
