# Card Zone Click Handling

Hand-maintained guide for building reliable tap/click interaction in card zones (Spread, Hand, Arcanum, pickers, commune arrangement, etc.).

This is a recurring source of bugs. Follow this pattern unless there is a documented reason not to.

---

## The two failure modes

### 1. Clicks do not register (or register inconsistently)

Usually caused by:

- **Zone rebuild on every `BindState`** — `zone.Clear()` destroys chips and their `Clickable` manipulators while the player is tapping.
- **Child elements stealing hits** — labels, art, alignment dots, or inspect icons intercept pointer events.
- **Overlapping pickable elements** — swap indicators, headers, or overlays above the card row without `picking-mode="Ignore"`.

### 2. Click works for one frame, then reverts

Usually caused by:

- **Re-seeding local UI state from session on refresh** — the player moves a card in local `_spreadIds` / `_handIds`, then `BindState` runs again and `SeedFromPlayer()` restores the pre-commune session layout (session is unchanged until the player submits a command).
- **Calling a full “enter phase” helper on every refresh** — e.g. `EnterCommuneUi()` inside `BindState` instead of only on the first transition into that phase.

Symptom: card flashes in the target zone, then jumps back.

---

## Standard: wire clicks through `CardChipFactory.WireTap`

All interactive card chips should use `CardChipFactory.WireTap` — not raw `RegisterCallback<ClickEvent>` on the chip root.

```csharp
CardChipFactory.WireTap(chip, () => onTap(cardId, fromSpread));
```

What `WireTap` does (`Assets/_Project/UI/Scripts/Components/CardChipFactory.cs`):

1. Sets the chip to `PickingMode.Position`.
2. Sets **all existing children** to `PickingMode.Ignore` so art/text do not eat clicks.
3. Adds a full-size `card-chip__hit` overlay (`Kismeta.uss`) as the sole click target.
4. Attaches a `Clickable` manipulator to that hit target.

For tap-to-swap commune zones, build chips via `TapSwapBindings.BuildChip`, which already calls `WireTap`.

**Inspect vs tap:** Major Arcana / inspect flows use `inspectViaButton: true` or a dedicated inspect affordance. Do not stack inspect and swap on the same chip without a clear hit target split.

---

## Standard: zone rebuild rules

### When to rebuild (full `Clear()` + recreate chips)

| Trigger | Rebuild? |
| -------- | -------- |
| First entry into an interactive phase | Yes |
| Player tap / drag completed a local move | Yes |
| Card IDs in zones actually changed (layout key differs) | Yes |
| Passive `BindState` / `RefreshActiveScreen` with same layout | **No** |
| Inventory mutation event before command submitted | **No** (keep local arrangement) |

### Reference implementations

| Screen / flow | Controller / bindings | Notes |
| ------------- | --------------------- | ----- |
| Standalone commune | `CommuneController` | `_initialized`, `_zonesBuilt`, `_builtForRoot` |
| Spring Hub commune (Step 4) | `SpringHubController` | `_communeInitialized`, `_communeZonesBuilt`; mandatory `ActionHint.Commune` |
| Winter unlock tap-swap | `WinterUnlockZoneBindings.ZoneBindState` | Also tracks `LastLayoutKey` to skip redundant rebuilds |
| Shared swap UI | `TapSwapBindings` | `RebuildZones` / `BuildChip` |
| Harvest dealing tableau | `TableauBindings` | `RebuildDealingZones` on `SpringHarvest`; commune uses `RebuildCommuneZones` on `SpringHub` |

---

## Standard: controller state for interactive zones

Any screen where the player rearranges cards **before** submitting a command needs **local lists** separate from `GameSession`:

```csharp
readonly List<string> _spreadIds = new();
readonly List<string> _handIds = new();

bool _initialized;           // seeded from session once per phase entry
bool _zonesBuilt;            // chips match current local lists
VisualElement? _builtForRoot;
int _arrangementPlayerId = -1;
```

### `BindState` pattern

```csharp
// 1) Transition commune ONCE per player/hint entry
if (hint == ActionHint.Commune && !_communeInitialized)
    SeedFromPlayer(session);

// 2) Reset when player changes
if (_playerId != _arrangementPlayerId)
{
    _arrangementPlayerId = _playerId;
    _initialized = false;
    _zonesBuilt = false;
}

// 3) Seed once
if (!_initialized)
    SeedFromPlayer(session);

// 4) Rebuild only if needed
RenderZonesIfNeeded();

// 5) On tap — update local lists, then rebuild and mark built
void OnTapMove(string cardId, bool fromSpread) { /* mutate lists */ RefreshZones(); _zonesBuilt = true; }
```

```csharp
void RenderZonesIfNeeded()
{
    if (_zonesBuilt && ReferenceEquals(Root, _builtForRoot)) return;
    TapSwapBindings.RebuildZones(Root, _session, _spreadIds, _handIds, ...);
    _zonesBuilt = true;
    _builtForRoot = Root;
}
```

### `SeedFromPlayer` must set `_initialized = true` and `_zonesBuilt = false`

Seeding clears local lists and copies minors from `player.Spread` / `player.Hand`. After seeding, zones are stale until the next explicit rebuild.

### `Unwire` / player change must reset all flags

---

## Optional: layout-key skip (Winter unlock)

When the same root can receive `BindState` with unchanged card IDs, use a layout key to avoid redundant rebuilds:

```csharp
// WinterUnlockZoneBindings.cs
var layoutKey = string.Join(",", spread) + "|" + string.Join(",", hand);
if (state.ZonesBuilt && ReferenceEquals(root, state.BuiltForRoot) && layoutKey == state.LastLayoutKey)
    return;
```

Use this in addition to — not instead of — the initialized/built-for-root guards.

---

## UXML / USS checklist

- [ ] Decorative overlays (swap badge, readout, narrative charge) use `picking-mode="Ignore"` where they overlap card rows.
- [ ] Card chips in swap zones use `TapSwapBindings.BuildChip` or `CardChipFactory.WireTap`.
- [ ] Zone container names match shared bindings: `spread-cards`, `hand-cards`, `arcanum-cards`.
- [ ] Do not rely on clicking card art or rank labels directly.

---

## Anti-patterns (do not do this)

```csharp
// BAD: full commune setup on every BindState refresh
if (hint == ActionHint.Commune)
    ResetAndSeedCommuneEveryRefresh(); // re-seeds from session, wipes taps

// BAD: unconditional zone rebuild during commune
if (hint == ActionHint.Commune)
    TableauBindings.RebuildCommuneZones(...); // every RefreshActiveScreen

// BAD: click on chip root with children still pickable
chip.RegisterCallback<ClickEvent>(_ => onTap());
```

---

## Adding a new interactive card zone

1. Use `TapSwapBindings` or `TableauBindings` if the interaction is tap-to-swap or commune-style.
2. Store local card ID lists if the command is submitted later (commune, winter unlock, limits rearrange).
3. Add `_initialized` / `_zonesBuilt` / `_builtForRoot` (or `ZoneBindState`) to the controller.
4. Wire taps only through `CardChipFactory.WireTap`.
5. Rebuild zones on user action; skip rebuild on passive `BindState` when layout unchanged.
6. Manual test: tap several cards in quick succession, then wait 2–3 seconds without tapping — arrangement must not revert.

## Commune on Spring Hub (Step 4)

Post-harvest commune runs on **Spring Hub** (`ActionHint.Commune`), not Spring Harvest. The hub shell provides review affordances during arrangement:

- Header: cosmic age + rival strip
- Inventory dock: spread/hand/arcanum inspect
- Toolbar: card table, season intro, Effects/Codex/Wards FABs

Canonical controller: `SpringHubController.BindCommune()` with `TableauBindings.RebuildCommuneZones()`.

During `SpringAction`, **Review the Tableau** (`review-tableau-btn`) reopens the same commune subview for optional last-minute rearrangement; **Lock The Tableau** submits `CommuneCommand` and returns to the hub board without ending the turn.

While arranging cards, **Review Effects** (`review-effects-btn`) opens the Active Effects overlay for quick reference (same panel as the dock Effects FAB).

---

## Related code

- `Assets/_Project/UI/Scripts/Components/CardChipFactory.cs` — `WireTap`, `SetIgnorePicking`
- `Assets/_Project/UI/Scripts/Controllers/TapSwapBindings.cs` — shared swap zone rebuild
- `Assets/_Project/UI/Scripts/Controllers/TableauBindings.cs` — harvest tableau zones
- `Assets/_Project/UI/Scripts/Components/WinterUnlockZoneBindings.cs` — layout-key variant
- `Assets/_Project/UI/USS/Kismeta.uss` — `.card-chip__hit`, `.card-chip--tappable`

## See also

- [Card zones (player-facing)](../Reference/card-zones.md)
- [Development index](README.md)
