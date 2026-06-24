using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    /// <summary>
    /// Binds local reagent counts into the inventory toolbar on the active hub screen.
    /// </summary>
    public sealed class PlayerHudController : MonoBehaviour
    {
        ViewportLayout? _layout;

        public void Configure(VisualTreeAsset? uxml) { }

        void Awake() => _layout = GetComponent<ViewportLayout>();

        public void SetVisible(bool visible)
        {
            var hud = ResolveHudRoot();
            if (hud != null)
                hud.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Hide() => SetVisible(false);

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            var hud = ResolveHudRoot();
            if (hud == null)
                return;

            int playerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (playerId < 0 || playerId >= session.Players.Count)
                return;

            PlayerHudBindings.Bind(hud, session.Players[playerId]);
        }

        VisualElement? ResolveHudRoot() => _layout?.ContentScreenRoot?.Q<VisualElement>("player-hud");
    }
}
