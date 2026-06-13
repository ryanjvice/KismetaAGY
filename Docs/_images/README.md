# Reference Images

Photography and visual reference for Kismeta: Alchemists of the Great Year.

Images here are for **documentation and development reference only** — they show the physical game in play and are used to guide the mobile UI implementation. If an image is needed as a runtime asset in the game build, copy it to `Assets/_Project/` instead.

All images are tracked by Git LFS (configured in `/.gitattributes`).

---

## Folder structure

```
_images/
  gameplay/
    spring/     ← Phase 1 table shots (dice rolls, Harvest, card layout)
    summer/     ← Phase 2 table shots (Crafting, Duels, Gambits, Trades)
    autumn/     ← Phase 3 table shots (Opposition, Forge, Stasis)
    winter/     ← Phase 4 table shots (Card Unlock, Fateful Wager, reset)
  setup/        ← Table before the first round (board, Codex cards, tokens)
  components/   ← Individual component close-ups (dice, tokens, Stones, Meeples)
  board/        ← Great Year Board full-table overheads; Zodiac Wheel detail
  cards/        ← Card photography or scans (Kismeta deck, Crucible deck)
```

---

## Naming convention

`{subject}_{detail}_{variant}.{ext}`

| Segment | Examples |
|---------|---------|
| `subject` | `table`, `board`, `hand`, `spread`, `forge`, `token`, `card` |
| `detail` | `4player`, `opposition`, `harvest`, `duel`, `stasis`, `overview` |
| `variant` | `01`, `02`, `wide`, `close`, `red`, `blue` |

Examples:
- `table_4player_spring_01.jpg`
- `board_zodiac-wheel_overview.jpg`
- `card_fool_front_01.png`
- `token_philosopher-stone_red.png`
- `spread_4cards_summer_close.jpg`

---

## Adding images

1. Drop the file into the appropriate subfolder.
2. Follow the naming convention above.
3. Add a one-line entry to the table in this file (below) so it's discoverable.
4. Reference it in a doc file using a relative path, e.g.:

   ```markdown
   ![4-player table during Spring phase](../../_images/gameplay/spring/table_4player_spring_01.jpg)
   ```

---

## Image index

| File | Folder | Description |
|------|--------|-------------|
| _(add images here as you go)_ | | |
