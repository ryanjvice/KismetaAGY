# Kismeta UI Toolkit — conversion manifest

Tracks every screen from the flow map (1–55) and its conversion status.
Each completed screen has: `UXML` (layout), `Scripts` (C# controller), and is
covered by a per-batch README. Shared styling lives in `USS/Kismeta.uss`;
screen-specific styling in `USS/<Screen>.uss` only where noted.

Legend: ✅ done · 🔲 todo · ⬛ N/A

| # | Screen | Region | UXML | C# | Batch | Notes |
|---|--------|--------|------|----|----|-------|
| 1 | Title screen | Shell | ✅ | ✅ | 1 | frame-broken ceremony |
| 2 | Setup — compact sheet | Shell | ✅ | ✅ | 1 | |
| 3 | Setup — dedicated | Shell | ✅ | ✅ | 1 | UXML pre-existing |
| 4 | Resume / Join | Shell | ✅ | ✅ | 1 | |
| 5 | How to play | Shell | ✅ | ✅ | 1 | placeholder content |
| 6 | Codex | Shell | ✅ | ✅ | 1 | placeholder content |
| 7 | Round-open (Agekeeper rolls) | Age open | ✅ | ✅ | 2 | dice native note |
| 8 | Age reveal + Cosmic Effect | Age open | ✅ | ✅ | 2 | |
| 9 | Pending wager resolves | Age open | ✅ | ✅ | 2 | |
| 10 | Spring intro | Spring | ✅ | ✅ | 2 | season-intro template |
| 11 | Roll your sign | Spring | ✅ | ✅ | 3 | wheel native note |
| 12 | Harvest tally | Spring | ✅ | ✅ | 3 | |
| 13 | Post-harvest hub | Spring | ✅ | ✅ | (pre) | UXML done; C# in batch 3 |
| 14 | Commune | Spring | ✅ | ✅ | 3 | tap-to-swap |
| 15 | Card Lock → Summer | Spring | ✅ | ✅ | 3 | transition |
| 16 | Summer intro | Summer | ✅ | ✅ | 2 | |
| 17 | Summer main scene | Summer | ✅ | 🔲 | (pre) | UXML done; C# in batch 4 |
| 18 | Craft & build sheet | Summer | 🔲 | 🔲 | 4 | |
| 19 | Craft reagent | Summer | 🔲 | 🔲 | 4 | interactive |
| 20 | Forge result | Summer | 🔲 | 🔲 | 4 | |
| 21 | Activate crucible card | Summer | 🔲 | 🔲 | 4 | |
| 22 | Light cauldron (explainer) | Summer | 🔲 | 🔲 | 4 | |
| 23 | Build astral house | Summer | 🔲 | 🔲 | 4 | |
| 24 | Place wards | Summer | 🔲 | 🔲 | 4 | |
| 25 | Consort sheet | Summer | 🔲 | 🔲 | 4 | |
| 26 | Trade | Summer | 🔲 | 🔲 | 5 | |
| 27 | End Summer CTA | Summer | 🔲 | 🔲 | 4 | |
| 28 | Autumn intro | Autumn | ✅ | ✅ | 2 | |
| 29 | Autumn main scene | Autumn | ✅ | 🔲 | (pre) | UXML done; C# in batch 6 |
| 30 | Manage cards (locked) | Autumn | 🔲 | 🔲 | 6 | |
| 31 | Fire your stone | Autumn | 🔲 | 🔲 | 6 | modal |
| 32 | Temper your stone | Autumn | 🔲 | 🔲 | 6 | modal |
| 33 | Opposition result → Stasis | Autumn | 🔲 | 🔲 | 5 | |
| 34 | Leave Stasis | Autumn | 🔲 | 🔲 | 6 | |
| 35 | Leave Stasis result | Autumn | 🔲 | 🔲 | 6 | |
| 36 | End Autumn CTA | Autumn | 🔲 | 🔲 | 6 | |
| 37 | Winter intro | Winter | ✅ | ✅ | 2 | |
| 38 | Hub — post-unlock | Winter | ✅ | ✅ | 3 | hub variant |
| 39 | Unlock | Winter | ✅ | ✅ | 3 | tap-to-swap |
| 40 | Fateful Wager | Winter | ✅ | ✅ | 3 | |
| 41 | Hub — post-wager | Winter | ✅ | ✅ | (pre) | UXML done; C# in batch 3 |
| 42 | Card limits | Winter | ✅ | ✅ | 3 | |
| 43 | Hub — post-limits | Winter | ✅ | ✅ | 3 | |
| 44 | Transit the age | Winter | ✅ | ✅ | 3 | |
| 45 | Age-close ceremony | Age close | ✅ | ✅ | 2 | |
| 46 | Standings + age recap | Age close | ✅ | ✅ | 2 | |
| 47 | Duel | Contest | 🔲 | 🔲 | 5 | shared wizard |
| 48 | Gambit | Contest | 🔲 | 🔲 | 5 | |
| 49 | Opposition | Contest | 🔲 | 🔲 | 5 | |
| 50 | Victory coronation | Game end | 🔲 | 🔲 | 7 | frame-broken |
| 51 | Chronicle | Game end | 🔲 | 🔲 | 7 | chart native note |
| 52 | Card table | Overlay | 🔲 | 🔲 | 7 | |
| 53 | Minor Arcana inspect | Overlay | 🔲 | 🔲 | 7 | |
| 54 | Adept received | Overlay | 🔲 | 🔲 | 7 | |
| 55 | Fate received | Overlay | 🔲 | 🔲 | 7 | |

## Batch plan (flow-map order)

- **Batch 1** — Shell (#1–6): title, setups, resume/join, how-to-play, codex
- **Batch 2** — Intros & ceremonies (#7–10, 16, 28, 37, 45–46): age open/close + 4 season intros
- **Batch 3** — Spring & Winter steps (#11–15, 38–44): step screens + hub C# controllers
- **Batch 4** — Summer actions (#18–25, 27): sheets, craft, activate, build, wards, end-CTA
- **Batch 5** — Contests (#26, 33, 47–49): Trade, Duel, Gambit, Opposition + results
- **Batch 6** — Autumn actions (#30–32, 34–36): manage, fire, temper, leave stasis + main C#
- **Batch 7** — Game-end & overlays (#50–55): victory, chronicle, card table, card modals

Pre-existing UXML (5 main scenes from the proof-of-pattern) get their C# controllers
folded into the relevant batch as noted.
