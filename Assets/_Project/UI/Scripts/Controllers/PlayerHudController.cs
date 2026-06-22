using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    /// <summary>
    /// Persistent bottom HUD mounted in AppShell's player-hud-layer.
    /// Survives screen swaps and shows local reagent inventory.
    /// </summary>
    public sealed class PlayerHudController : MonoBehaviour
    {
        VisualTreeAsset? _uxml;
        ViewportLayout? _layout;
        VisualElement? _hudRoot;
        bool _mounted;

        public void Configure(VisualTreeAsset? uxml) => _uxml = uxml;

        void Awake() => _layout = GetComponent<ViewportLayout>();

        public void EnsureMounted()
        {
            if (_mounted || _layout == null || _uxml == null)
                return;

            var layer = _layout.EnsurePlayerHudLayer();
            if (layer == null)
                return;

            layer.Clear();

            var host = new VisualElement();
            host.AddToClassList("kismeta-root");
            ApplyAssetStylesheets(host, _uxml);
            layer.Add(host);
            _uxml.CloneTree(host);

            _hudRoot = host.Q("player-hud") ?? (host.childCount > 0 ? host[0] : host);
            if (_hudRoot != null)
                ApplyAssetStylesheets(_hudRoot, _uxml);

            _mounted = true;
        }

        public void SetVisible(bool visible)
        {
            if (visible)
                EnsureMounted();

            var layer = _layout?.PlayerHudLayer;
            if (layer != null)
                layer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Hide() => SetVisible(false);

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            if (!_mounted)
                EnsureMounted();
            if (_hudRoot == null)
                return;

            int playerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (playerId < 0 || playerId >= session.Players.Count)
                return;

            PlayerHudBindings.Bind(_hudRoot, session.Players[playerId]);
        }

        static void ApplyAssetStylesheets(VisualElement target, VisualTreeAsset asset)
        {
            if (target == null || asset == null)
                return;

            foreach (var sheet in asset.stylesheets)
            {
                if (sheet != null && !target.styleSheets.Contains(sheet))
                    target.styleSheets.Add(sheet);
            }
        }
    }
}
