using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class CardInspectBindings
    {
        public static void BindInspectModal(
            VisualElement root,
            GameSession session,
            string cardInstanceId)
        {
            if (root == null) return;
            var db = session.Rules?.CardDatabase;
            var inst = session.GetCard(cardInstanceId);
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null) return;

            var nameLbl = root.Q<Label>("inspect-name");
            if (nameLbl != null)
                nameLbl.text = def.IsMinorArcana
                    ? $"{def.Rank} of {def.Suit}"
                    : def.EffectText.Length > 0 ? def.EffectText.Split('\n')[0] : def.Id;

            var ageLbl = root.Q<Label>("inspect-align-age");
            var whyLbl = root.Q<Label>("inspect-align-why");
            var ptsLbl = root.Q<Label>("inspect-align-pts");

            var cosmic = session.Board.CosmicAgeSign;
            int pts = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);

            if (ageLbl != null)
                ageLbl.text = $"Age of {cosmic} · {Correspondence.PlanetFor(cosmic)} · {Correspondence.ElementFor(cosmic)}";

            if (whyLbl != null)
            {
                whyLbl.text = pts switch
                {
                    3 => "its sign matches the cosmic age",
                    2 => "its planet matches the age's planet",
                    1 => "its element matches the age's element",
                    _ => "no aspect matches this age"
                };
            }

            if (ptsLbl != null)
            {
                ptsLbl.text = pts > 0 ? $"+{pts}" : "0";
                ptsLbl.style.color = pts > 0
                    ? new StyleColor(new UnityEngine.Color(0.94f, 0.6f, 0.48f))
                    : new StyleColor(new UnityEngine.Color(0.72f, 0.6f, 0.43f));
            }
        }
    }
}
