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
buttons, and optionally `Bind()` to push game state. Attach it to the same GameObject
as the screen's `UIDocument`.

**Naming hooks.** Every interactive element in UXML has a `name`. Controllers query by
that name (`Btn("new-game-btn")`). Keep names stable — they are the contract between
UXML and C#.

**Navigation** is exposed as `System.Action` fields on each controller (e.g.
`OnNewGame`, `OnBegin`). Wire these from a higher-level screen router / state machine;
the controllers don't assume how navigation works.

**Styling.** Shared classes live in `USS/Kismeta.uss` (imported by every UXML). A
screen gets its own `.uss` only when it has genuinely unique styling — so far only
`TitleScreen.uss` (the ceremonial hero treatment).

## NATIVE NOTES in this batch

- **Title hero** — the radial gold glow + sacred-geometry backdrop were CSS gradients;
  in Unity use a background sprite/Image on `#title-hero` and the serif font asset for
  the wordmark and sigil (`-unity-font-definition` paths are placeholders).
- **Setup sheet** — present as an overlay with a dimmed scrim; animate the rise
  (translateY) in C# via `experimental.animation`.
- **Codex** — content is data-driven; bind `#codex-search` and populate `#codex-list`
  from a ScriptableObject/JSON of cards, ages, states, glossary. Rows shown are samples.

## Wiring example

```csharp
// On the Title screen GameObject (has UIDocument + TitleScreen.uxml):
var title = GetComponent<TitleScreenController>();
title.OnNewGame   = () => router.Push("SetupSheet");
title.OnResume    = () => router.Push("Resume");
title.OnJoin      = () => router.Push("Join");
title.OnHowToPlay = () => router.Push("HowToPlay");
title.OnCodex     = () => router.Push("Codex");
```

## Status

All of #1–6 are layout-complete with controllers. Card/age/state content in the Codex
and the actual navigation router are left as integration work. Next: **Batch 2 —
intros & ceremonies** (#7–10, 16, 28, 37, 45–46).
