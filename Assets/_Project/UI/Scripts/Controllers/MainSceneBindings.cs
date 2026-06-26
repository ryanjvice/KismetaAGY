using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public enum DockZone
    {
        Spread,
        Hand,
        Arcanum
    }

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

            BindCosmicAgeSigil(root, sign);
        }

        static void BindCosmicAgeSigil(VisualElement root, ZodiacSign sign)
        {
            var sigil = root.Q(className: "cosmic-age-banner__sigil");
            if (sigil == null) return;

            var glyph = sigil.Q<Label>("cosmic-age-glyph");
            if (glyph == null)
            {
                glyph = SymbolGlyphs.CreateZodiacLabel(
                    sign == ZodiacSign.None ? "?" : SymbolGlyphs.Zodiac(sign),
                    "cosmic-age-banner__glyph");
                glyph.name = "cosmic-age-glyph";
                sigil.Clear();
                sigil.Add(glyph);
                return;
            }

            SymbolGlyphs.TagZodiac(glyph);
            glyph.text = sign == ZodiacSign.None ? "?" : SymbolGlyphs.Zodiac(sign);
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

            for (int i = 0; i < StepRailBuilder.MaxStepSlots; i++)
            {
                var step = rail.Q<VisualElement>($"step-{i}");
                if (step == null) continue;

                if (i >= stepCount)
                {
                    step.style.display = DisplayStyle.None;
                    continue;
                }

                step.style.display = DisplayStyle.Flex;

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

        public static void BindActionGroupRail(VisualElement? rail, int groupCount, int? activeIndex)
        {
            if (rail == null) return;

            for (int i = 0; i < StepRailBuilder.MaxStepSlots; i++)
            {
                var step = rail.Q<VisualElement>($"step-{i}");
                if (step == null) continue;

                if (i >= groupCount)
                {
                    step.style.display = DisplayStyle.None;
                    continue;
                }

                step.style.display = DisplayStyle.Flex;

                var dot = step.Q(className: "step__dot");
                if (dot == null) continue;

                dot.RemoveFromClassList("step__dot--done");
                dot.RemoveFromClassList("step__dot--active");
                dot.RemoveFromClassList("step__dot--locked");
                dot.RemoveFromClassList("step__dot--available");

                if (activeIndex == i)
                    dot.AddToClassList("step__dot--active");
                else
                    dot.AddToClassList("step__dot--available");
            }
        }

        public static int ResolveLocalPlayerId(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            int humanId = loop.LocalHumanPlayerId;
            if (humanId >= 0 && humanId < session.Players.Count)
                return humanId;

            var hs = bridge.PendingController;
            if (hs != null && hs.Slot.Index >= 0 && hs.Slot.Index < session.Players.Count)
                return hs.Slot.Index;

            return 0;
        }

        public static void BindSpectatorTurnBanner(
            VisualElement? root,
            GameSession session,
            GameLoop loop,
            string locationPhrase)
        {
            if (root == null) return;

            int activeId = loop.ActivePlayerId;
            var banner = root.Q<Label>("turn-banner");
            var hint = root.Q<Label>("hint-label");

            if (activeId >= 0 && activeId < session.Players.Count
                && activeId != loop.LocalHumanPlayerId)
            {
                var name = CeremonyBindings.PlayerName(session, activeId);
                if (banner != null)
                    banner.text = $"{name} is {locationPhrase}";
            }
            else if (banner != null)
            {
                banner.text = "Rivals are taking their turns…";
            }

            if (hint != null)
                hint.text = "Review your board while you wait";
        }

        public static void BindCauldrons(VisualElement? root, PublicPlayerView? local) =>
            CauldronHubBindings.Bind(root, local);

        public static PublicPlayerView? LocalPlayer(GamePublicView view, int localPlayerId)
        {
            foreach (var p in view.Players)
            {
                if (p.PlayerId == localPlayerId)
                    return p;
            }
            return view.Players.Count > 0 ? view.Players[0] : null;
        }

        public static void BindPlayerStrip(VisualElement? root, GameSession session, int localPlayerId)
        {
            if (root == null) return;
            if (localPlayerId < 0 || localPlayerId >= session.Players.Count)
                return;

            var player = session.Players[localPlayerId];
            var sign = player.CurrentSign;

            var dot = root.Q("player-strip-dot");
            if (dot != null)
                dot.style.backgroundColor = new StyleColor(PlayerUiNames.PlayerColor(localPlayerId));

            var glyph = root.Q<Label>("player-sign-glyph");
            if (glyph != null)
            {
                SymbolGlyphs.TagZodiac(glyph);
                glyph.text = sign == ZodiacSign.None ? "?" : SymbolGlyphs.Zodiac(sign);
            }

            var signLbl = root.Q<Label>("player-sign");
            if (signLbl != null)
                signLbl.text = sign == ZodiacSign.None ? "no sign" : sign.ToString();

            var planetLbl = root.Q<Label>("player-planet");
            if (planetLbl != null)
                planetLbl.text = sign == ZodiacSign.None ? "—" : Correspondence.PlanetFor(sign).ToString();

            var elementLbl = root.Q<Label>("player-element");
            if (elementLbl != null)
                elementLbl.text = sign == ZodiacSign.None ? "—" : Correspondence.ElementFor(sign).ToString();
        }

        public static void SetDockZoneFabActive(VisualElement? root, DockZone zone)
        {
            var handBtn = root?.Q<Button>("hand-btn");
            if (handBtn != null)
            {
                handBtn.text = zone == DockZone.Hand ? "Spread" : "Hand";
                handBtn.EnableInClassList("menu-btn-fab--active", zone == DockZone.Hand);
            }

            var arcanumBtn = root?.Q<Button>("arcanum-btn");
            if (arcanumBtn != null)
                arcanumBtn.EnableInClassList("menu-btn-fab--active", zone == DockZone.Arcanum);
        }

        public static void SetHandFabActive(VisualElement? root, bool showHand) =>
            SetDockZoneFabActive(root, showHand ? DockZone.Hand : DockZone.Spread);

        public static void BindDockStrip(
            VisualElement? root,
            GameSession session,
            int localPlayerId,
            DockZone zone,
            Action<string>? onInspect)
        {
            if (root == null) return;
            if (localPlayerId < 0 || localPlayerId >= session.Players.Count)
                return;

            var zoneLbl = root.Q<Label>("dock-zone-label");
            if (zoneLbl != null)
            {
                zoneLbl.text = zone switch
                {
                    DockZone.Hand => "hand",
                    DockZone.Arcanum => "arcanum",
                    _ => "spread"
                };
            }

            var local = LocalPlayer(GamePublicView.From(session), localPlayerId);
            IReadOnlyList<string> cardIds = zone switch
            {
                DockZone.Hand => PlayerPrivateView.From(session, localPlayerId).Hand,
                DockZone.Arcanum => session.Players[localPlayerId].Arcanum,
                _ => local?.Spread ?? Array.Empty<string>()
            };

            var countLbl = root.Q<Label>("spread-count");
            if (countLbl != null)
                countLbl.text = cardIds.Count.ToString();

            var strip = root.Q<VisualElement>("spread-strip");
            if (strip == null) return;

            var zonePrefix = zone switch
            {
                DockZone.Hand => "H:",
                DockZone.Arcanum => "A:",
                _ => "S:"
            };
            var signature = zonePrefix + (onInspect != null ? "I:" : "i:")
                + string.Join(",", cardIds);
            if (strip.userData as string == signature)
                return;

            strip.userData = signature;
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
                    def, instanceId: cardId, onInspect: onInspect, inspectViaButton: true));
            }
        }
    }
}
