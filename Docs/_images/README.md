# Reference Images

Visual reference for the Great Year mobile game.

Images here are for **documentation and development reference only**. Copy assets needed at runtime to `Assets/Art/UI/` (or the appropriate `Assets/` subfolder).

All images are tracked by Git LFS (configured in `/.gitattributes`).

---

## Folder structure

```
_images/
  mockups/     ← Primary UI reference (screen flows, table layout, phase modals)
  board/       ← Great Year board art asset (central wheel / zodiac)
```

Full mockup index: [`mockups/README.md`](mockups/README.md).

---

## mockups/

Screen mockups for every major UI flow — Spring through Winter, duel/gambit, opposition, stasis, win state, and the labeled full-table layout.

When implementing a screen, start with the matching entry in [`UI.md`](../UI.md) and the row in [`mockups/README.md`](mockups/README.md).

---

## board/

| File | Description |
|------|-------------|
| [`Kismeta_gameBoard_final.png`](board/Kismeta_gameBoard_final.png) | Great Year board art — zodiac wheel, transmutation path (Lead → Gold), mantle/crucible ring, corner player reference |

Used by the `BoardView` prefab described in [`UI.md`](../UI.md#3-main-table-layout).

---

## Naming convention

New mockups should follow:

`{phase}_{action}_{step}.png`

Examples: `spring_harvest.png`, `duel_roll.png`, `winter_fatefulWager_select.png`

---

## Adding images

1. Drop the file into `mockups/` or `board/` as appropriate.
2. Add a row to [`mockups/README.md`](mockups/README.md) (or the board table above).
3. Add or update the catalog entry in [`UI.md`](../UI.md).
4. Reference in docs using a relative path, e.g.:

   ```markdown
   ![Spring harvest](../_images/mockups/spring_harvest.png)
   ```
