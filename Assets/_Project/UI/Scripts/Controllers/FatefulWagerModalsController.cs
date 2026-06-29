using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class FatefulWagerModalsController : OverlayController
    {
        static readonly string[] AllModalRoots = { "wager-confirm-modal", "wager-result-modal" };

        GameSession? _session;

        public Action? OnConfirm;
        public Action? OnCancel;
        public Action? OnDismiss;

        protected override void Wire()
        {
            Btn("confirm-commit-btn")!.clicked += () => OnConfirm?.Invoke();
            Btn("confirm-cancel-btn")!.clicked += () => OnCancel?.Invoke();
            Btn("confirm-cancel-btn-2")!.clicked += () => OnCancel?.Invoke();
            Btn("result-continue-btn")!.clicked += () => OnDismiss?.Invoke();
        }

        public void ShowConfirm()
        {
            SetActiveModal("wager-confirm-modal");
        }

        public void ShowResult()
        {
            SetActiveModal("wager-result-modal");
        }

        void SetActiveModal(string active)
        {
            if (Root == null) return;
            foreach (var name in AllModalRoots)
            {
                var el = El(name);
                if (el != null)
                    el.style.display = name == active ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void BindConfirm(GameSession session, ZodiacSign sign, IReadOnlyList<string> cardIds)
        {
            _session = session;
            ShowConfirm();

            if (Lbl("confirm-sign-glyph") is { } glyph)
            {
                SymbolGlyphs.TagZodiac(glyph);
                glyph.text = SymbolGlyphs.Zodiac(sign);
            }
            SetLabel("confirm-sign-name", sign.ToString());
            SetLabel("confirm-stake-line", $"Staking {cardIds.Count} card(s)");

            var tray = El("confirm-stake-cards");
            if (tray != null)
            {
                tray.Clear();
                var db = session.Rules?.CardDatabase;
                foreach (var id in cardIds)
                {
                    var inst = session.GetCard(id);
                    var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                    if (def == null) continue;
                    var chip = CardChipFactory.CreateFromDefinition(def, selected: true);
                    tray.Add(chip);
                }
            }

            int count = cardIds.Count;
            SetLabel("confirm-stakes-copy",
                count > 0
                    ? $"If {sign} rules next age, your {count} staked card(s) double. If wrong, they're lost. Resolves next Spring."
                    : "If wrong, these cards are lost to the Fates. Resolves next Spring.");
        }

        public void BindResult(FatefulWagerResolvedEvent resolved)
        {
            ShowResult();

            bool won = resolved.Won;
            var modal = El("wager-result-modal");
            modal?.EnableInClassList("wager-result--won", won);
            modal?.EnableInClassList("wager-result--lost", !won);

            SetLabel("result-eyebrow", won ? "Fortune favors you" : "The Fates have spoken");
            SetLabel("result-subtitle", won ? "your wager doubles" : "your wager is lost");

            BindSignLabel("result-predicted-glyph", "result-predicted-name", resolved.PredictedSign);
            BindSignLabel("result-age-glyph", "result-age-name", resolved.Sign);

            int count = resolved.CardCount;
            SetLabel("result-outcome-line", won
                ? $"Correct! Your {count} card(s) return doubled — {count * 2} in hand."
                : $"Your prediction missed. {count} card(s) lost to the Fates.");
        }

        void BindSignLabel(string glyphName, string nameLabel, ZodiacSign sign)
        {
            if (Lbl(glyphName) is { } glyph)
            {
                SymbolGlyphs.TagZodiac(glyph);
                glyph.text = SymbolGlyphs.Zodiac(sign);
            }
            SetLabel(nameLabel, sign.ToString());
        }

        void SetLabel(string name, string text)
        {
            var lbl = Lbl(name);
            if (lbl != null)
                lbl.text = text;
        }
    }
}
