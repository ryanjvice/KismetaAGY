using Kismeta.Core.Entities;
using Kismeta.UI;
using Kismeta.UI.Components;

namespace Kismeta.UI.Controllers
{
    public sealed class ManageCardsController : OverlayController
    {
        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        public System.Action? OnClose;

        protected override void Wire()
        {
            Btn("close-btn")!.clicked += () => OnClose?.Invoke();
            Btn("forge-btn")!.clicked += () => OnClose?.Invoke();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            var player = session.Players[_playerId];
            AutumnBoardBindings.PopulateCruciblePills(El("crucible-pills"), session, player);
            AutumnBoardBindings.PopulateCardStrip(El("spread-strip"), session, player.Spread);
            AutumnBoardBindings.PopulateCardStrip(El("hand-strip"), session, player.Hand);
            AutumnBoardBindings.PopulateReagentPanel(El("reagent-panel"), player);

            if (Lbl("context-note") != null)
                Lbl("context-note")!.text = AutumnBoardBindings.BuildContextNote(session, player);
        }
    }
}
