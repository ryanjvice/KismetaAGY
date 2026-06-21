using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SpringHarvestController : ScreenController
    {
        public override string ScreenId => ScreenIds.SpringHarvest;

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _bindKey = int.MinValue;

        protected override void Wire()
        {
            Btn("deal-btn")!.clicked += OnDeal;
        }

        protected override void Unwire()
        {
            _bindKey = int.MinValue;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            BindCosmicAgeBanner(session);

            int bindKey = ComputeBindKey(session, _playerId);
            if (bindKey == _bindKey) return;
            _bindKey = bindKey;

            var breakdown = HarvestBreakdownService.Build(session, _playerId);

            if (Lbl("harvest-total") != null)
                Lbl("harvest-total")!.text = breakdown.Total.ToString();

            if (Lbl("harvest-summary") != null)
            {
                Lbl("harvest-summary")!.text =
                    $"BASE {breakdown.Base} + BONUS {breakdown.BonusSubtotal} + BOON {breakdown.Boon}";
            }

            HarvestSourceRows.Populate(El("source-list"), breakdown.Sources);

            var dealBtn = Btn("deal-btn");
            if (dealBtn != null)
                dealBtn.text = $"→ Deal {breakdown.Total} & commune";
        }

        static int ResolvePlayerId(GameSession session, CommandBridge bridge)
        {
            int pid = bridge.ActivePlayerId;
            if (pid >= 0 && pid < session.Players.Count)
                return pid;

            var hs = bridge.PendingController;
            if (hs != null && hs.Slot.Index >= 0 && hs.Slot.Index < session.Players.Count)
                return hs.Slot.Index;

            return -1;
        }

        static int ComputeBindKey(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            int spreadHash = 0;
            foreach (var id in player.Spread)
                spreadHash = spreadHash * 31 + id.GetHashCode();
            return playerId * 1000
                   + (int)session.Board.CosmicAgeSign * 10
                   + (int)player.CurrentSign
                   + spreadHash;
        }

        void BindCosmicAgeBanner(GameSession session)
        {
            var sign = session.Board.CosmicAgeSign;
            if (Lbl("age-sign") != null)
                Lbl("age-sign")!.text = sign == ZodiacSign.None ? "—" : sign.ToString();
            if (Lbl("age-planet") != null)
                Lbl("age-planet")!.text = Correspondence.PlanetFor(sign).ToString();
            if (Lbl("age-element") != null)
                Lbl("age-element")!.text = Correspondence.ElementFor(sign).ToString();
        }

        void OnDeal()
        {
            if (_bridge == null || _playerId < 0) return;
            _bridge.TrySubmit(new HarvestCommand(_playerId, 0));
        }
    }
}
