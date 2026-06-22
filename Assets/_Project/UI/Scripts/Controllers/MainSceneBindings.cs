using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    /// <summary>Shared bind helpers for season main scenes.</summary>
    internal static class MainSceneBindings
    {
        public static void ApplySeasonClass(VisualElement? root, Season season)
        {
            if (root == null) return;
            root.RemoveFromClassList("screen--spring");
            root.RemoveFromClassList("screen--summer");
            root.RemoveFromClassList("screen--autumn");
            root.RemoveFromClassList("screen--winter");
            root.AddToClassList($"screen--{season.ToString().ToLowerInvariant()}");
        }

        public static void BindStatusBar(VisualElement? root, GameSession session, GameLoop loop)
        {
            if (root == null) return;

            var seasonLbl = root.Q<Label>("status-season");
            if (seasonLbl != null)
                seasonLbl.text = session.Phase.CurrentSeason.ToString();

            var stepLbl = root.Q<Label>("status-step");
            if (stepLbl != null)
                stepLbl.text = session.Phase.CurrentStep.Name;

            var roundLbl = root.Q<Label>("status-round");
            if (roundLbl != null)
                roundLbl.text = $"Round {session.Board.RoundNumber}";

            var hintLbl = root.Q<Label>("hint-label");
            if (hintLbl != null)
            {
                hintLbl.text = session.IsOver
                    ? $"Game over — winner P{session.WinnerPlayerId}"
                    : FormatTurnHint(loop);
            }
        }

        public static void BindCosmicAgeBanner(VisualElement? root, GameSession session)
        {
            if (root == null) return;

            var sign = session.Board.CosmicAgeSign;
            var signLbl = root.Q<Label>("age-sign");
            if (signLbl != null)
                signLbl.text = sign == ZodiacSign.None ? "—" : sign.ToString();

            var planetLbl = root.Q<Label>("age-planet");
            if (planetLbl != null)
                planetLbl.text = Correspondence.PlanetFor(sign).ToString();

            var elementLbl = root.Q<Label>("age-element");
            if (elementLbl != null)
                elementLbl.text = Correspondence.ElementFor(sign).ToString();
        }

        static string FormatTurnHint(GameLoop loop)
        {
            if (loop.PendingHumanController != null)
            {
                int pid = loop.ActivePlayerId >= 0
                    ? loop.ActivePlayerId
                    : loop.PendingHumanController.Slot.Index;
                return $"Your turn · {loop.PendingHint} · P{pid}";
            }

            if (loop.PendingHint != ActionHint.None)
                return $"Resolving · {loop.PendingHint}";

            return "Waiting for next step…";
        }

        public static void BindPassButton(VisualElement? root, GameSession session, CommandBridge bridge)
        {
            var pass = root?.Q<Button>("pass-btn");
            if (pass == null) return;

            var hint = bridge.PendingHint;
            bool canPass = bridge.CanSubmit && !session.IsOver &&
                hint is ActionHint.SummerAction or ActionHint.AutumnAction or ActionHint.WinterAction;
            pass.SetEnabled(canPass);
        }

        public static void BindStepRail(VisualElement? rail, int currentStepIndex, int stepCount, string activeAccentClass)
        {
            if (rail == null) return;

            for (int i = 0; i < stepCount; i++)
            {
                var step = rail.Q<VisualElement>($"step-{i}");
                if (step == null) continue;

                var dot = step.Q(className: "step__dot");
                if (dot == null) continue;

                dot.RemoveFromClassList("step__dot--done");
                dot.RemoveFromClassList("step__dot--active");
                dot.RemoveFromClassList("step__dot--locked");
                dot.RemoveFromClassList(activeAccentClass);

                if (i < currentStepIndex)
                    dot.AddToClassList("step__dot--done");
                else if (i == currentStepIndex)
                {
                    dot.AddToClassList("step__dot--active");
                    dot.AddToClassList(activeAccentClass);
                }
                else
                    dot.AddToClassList("step__dot--locked");
            }
        }

        public static PublicPlayerView? LocalPlayer(GamePublicView view, int localPlayerId)
        {
            foreach (var p in view.Players)
            {
                if (p.PlayerId == localPlayerId)
                    return p;
            }
            return view.Players.Count > 0 ? view.Players[0] : null;
        }

        public static void SetHandFabActive(VisualElement? root, bool showHand)
        {
            var btn = root?.Q<Button>("hand-btn");
            if (btn == null) return;
            btn.text = showHand ? "Spread" : "Hand";
            btn.EnableInClassList("menu-btn-fab--active", showHand);
        }

        public static void BindDockStrip(
            VisualElement? root,
            GameSession session,
            int localPlayerId,
            bool showHand,
            Action<string>? onInspect)
        {
            if (root == null) return;

            var zoneLbl = root.Q<Label>("dock-zone-label");
            if (zoneLbl != null)
                zoneLbl.text = showHand ? "hand" : "spread";

            var local = LocalPlayer(GamePublicView.From(session), localPlayerId);
            IReadOnlyList<string> cardIds = showHand
                ? PlayerPrivateView.From(session, localPlayerId).Hand
                : local?.Spread ?? Array.Empty<string>();

            var countLbl = root.Q<Label>("spread-count");
            if (countLbl != null)
                countLbl.text = cardIds.Count.ToString();

            var strip = root.Q<VisualElement>("spread-strip");
            if (strip == null) return;
            strip.Clear();

            var db = session.Rules?.CardDatabase;
            if (db == null) return;

            foreach (var cardId in cardIds)
            {
                var inst = session.GetCard(cardId);
                if (inst == null) continue;
                var def = db.GetById(inst.DefinitionId);
                if (def == null) continue;
                strip.Add(CardChipFactory.CreateFromDefinition(
                    def.Rank.ToString(), def.Id, db,
                    instanceId: cardId, onInspect: onInspect));
            }
        }
    }
}
