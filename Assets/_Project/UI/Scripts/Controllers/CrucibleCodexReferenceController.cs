using System;
using UnityEngine;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CrucibleCodexReferenceController : OverlayController
    {
        public Action? OnClose;
        public Action<int>? OnClaimFoolAltar;

        GameSession? _session;
        CommandBridge? _bridge;
        GameLoop? _loop;
        int _bindKey = int.MinValue;
        int _localPlayerId = -1;

        protected override void Wire()
        {
            Btn("crucible-codex-close")!.clicked += () => OnClose?.Invoke();
            ApplyHostLayout(tall: true);
        }

        protected override void Unwire()
        {
            ApplyHostLayout(tall: false);
            _bindKey = int.MinValue;
            _localPlayerId = -1;
        }

        void ApplyHostLayout(bool tall)
        {
            Root?.parent?.EnableInClassList("overlay-clone-host--active-effects", tall);
            Root?.parent?.parent?.EnableInClassList("overlay-layer--active-effects", tall);
        }

        public void BindState(GameSession session, GameLoop? loop, CommandBridge bridge)
        {
            _session = session;
            _loop = loop;
            _bridge = bridge;
            if (Root == null) return;

            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (_localPlayerId < 0) return;

            int bindKey = ComputeBindKey(session, _localPlayerId);
            if (bindKey == _bindKey) return;
            _bindKey = bindKey;

            CrucibleCodexRows.Populate(Root, session, _localPlayerId);
            BindFoolAltarBanner(session);
        }

        void BindFoolAltarBanner(GameSession session)
        {
            var host = El("fool-altar-banner");
            if (host == null) return;

            host.Clear();
            var altarId = session.Board.FoolAltarCrucibleCardId;
            if (string.IsNullOrEmpty(altarId))
            {
                host.style.display = DisplayStyle.None;
                return;
            }

            host.style.display = DisplayStyle.Flex;
            var db = session.Rules?.CardDatabase;
            var inst = session.GetCard(altarId);
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;

            host.Add(new Label("Fool Altar — first to complete the formula claims this card")
            {
                style = { fontSize = 10, whiteSpace = WhiteSpace.Normal, marginBottom = 4 }
            });
            host.Add(new Label(def?.Name ?? "Crucible card")
            {
                style = { fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 }
            });
            if (!string.IsNullOrWhiteSpace(def?.AlchemicalFormula))
            {
                host.Add(new Label(def!.AlchemicalFormula)
                {
                    style = { fontSize = 10, whiteSpace = WhiteSpace.Normal, marginBottom = 6 }
                });
            }

            var player = session.Players[_localPlayerId];
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                if (player.CrucibleSlots[i].State != CrucibleCardState.Dormant) continue;
                int slotIndex = i;
                var btn = new Button(() => OnClaimFoolAltar?.Invoke(slotIndex))
                {
                    text = $"Claim into dormant slot {slotIndex + 1}"
                };
                btn.AddToClassList("btn");
                btn.AddToClassList("btn--secondary");
                btn.style.marginBottom = 4;
                host.Add(btn);
            }
        }

        static int ComputeBindKey(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            int spreadHash = 0;
            foreach (var id in player.Spread)
                spreadHash = spreadHash * 31 + id.GetHashCode();

            int slotHash = 0;
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                slotHash = slotHash * 31 + (int)slot.State;
                slotHash = slotHash * 31 + slot.CardInstanceId.GetHashCode();
            }

            return playerId * 10000
                   + (int)player.AssignedCodex * 100
                   + (session.Board.FoolAltarCrucibleCardId?.GetHashCode() ?? 0)
                   + spreadHash
                   + slotHash;
        }
    }
}
