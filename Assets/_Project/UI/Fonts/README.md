# Kismeta UI Fonts

Production typography for Unity UI Toolkit. Font roles and USS utility classes live in `Kismeta.uss`.

## Typefaces

| File | Role | USS class | Use for |
|------|------|-----------|---------|
| `Amarante-Regular.asset` | Display / lore | `.font-display`, `.font-lore`, `.h1` | Titles, wordmarks, ceremonial names, immersive lore copy (non-instructional) |
| `GermaniaOne-Regular.asset` | Section headings | `.font-heading`, `.h2`, `.h3` | Screen section titles, season taglines, modal headings |
| `FuturaCyrillicDemi.asset` | UI headings | `.font-ui`, `.h4`, `.h5`, `.h6` | Smaller headings, labels, buttons, data chrome (h4 and below) |
| `FuturaCyrillicBook.asset` | Body | *(default on `.kismeta-root`)*, `.font-body`, `.p` | Instructional body text, rules, lists, normal paragraphs |
| `NotoColorEmoji-Regular.asset` | Symbols | `.font-emoji` | Zodiac glyphs (♈–♓), suit emojis (🪄🍷🪙🗡️), and other emoji |
| `tabler-icons.asset` | Icons | `.ti-icon` | Tabler icon glyphs in `Kismeta.uss` |

## Hierarchy (quick reference)

- **Title / h1** → Amarante
- **h2 & h3** → Germania One
- **h4 and lower** → Futura Cyrillic Demi
- **Normal p** → Futura Cyrillic Book
- **Lore p** → Amarante (add class `font-lore` or `p-lore`)
- **Emoji / symbols** → Noto Color Emoji (add class `font-emoji`, or use `SymbolGlyphs` in C#)
- **Tabler icons** → `tabler-icons.asset` via `.ti-icon` (`-unity-font-definition`)

## C# helpers

`SymbolGlyphs` (`Assets/_Project/UI/Scripts/Components/SymbolGlyphs.cs`) centralizes zodiac and suit unicode and tags elements with `.font-emoji`:

```csharp
var glyph = SymbolGlyphs.CreateEmojiLabel(SymbolGlyphs.Zodiac(sign));
chip.Add(SymbolGlyphs.CreateEmojiLabel(SymbolGlyphs.SuitGlyph(suit), "card-chip__suit"));
```

## Import steps

1. TTF source files live in this folder; open the project so Unity imports them (`.meta` files appear).
2. Run **Kismeta → UI → Create UI Font Assets** once to generate the six `.asset` FontAssets (with atlas sub-assets).
3. `Kismeta.uss` references FontAssets via `-unity-font-definition` (required in Unity 6 — the panel theme's default FontAsset overrides legacy `-unity-font` TTF rules).
4. Run **Kismeta → UI → Verify Font Imports** to confirm all TTFs and FontAssets load cleanly.
5. Until FontAssets exist, screens fall back to the default theme font (Inter/Liberation sans).

## Icons alternative

If the Tabler font is unavailable, replace `Label` icon elements with `Image` + sprite sheets in UXML (one-time pass during Phase 1).
