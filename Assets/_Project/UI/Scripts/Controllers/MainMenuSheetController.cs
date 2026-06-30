using System;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class MainMenuSheetController
    {
        VisualElement? _root;
        Button? _closeBtn;
        Button? _returnBtn;
        Button? _settingsBtn;
        Button? _rulesBtn;
        Button? _quitBtn;

        public Action? OnReturnToMainMenu;
        public Action? OnSettings;
        public Action? OnRules;
        public Action? OnQuit;
        public Action? OnClose;

        public void Attach(VisualElement root)
        {
            Detach();
            _root = root;
            _closeBtn = root.Q<Button>("main-menu-close-btn");
            _returnBtn = root.Q<Button>("main-menu-return-btn");
            _settingsBtn = root.Q<Button>("main-menu-settings-btn");
            _rulesBtn = root.Q<Button>("main-menu-rules-btn");
            _quitBtn = root.Q<Button>("main-menu-quit-btn");

            _closeBtn?.RegisterCallback<ClickEvent>(OnCloseClick);
            _returnBtn?.RegisterCallback<ClickEvent>(OnReturnClick);
            _settingsBtn?.RegisterCallback<ClickEvent>(OnSettingsClick);
            _rulesBtn?.RegisterCallback<ClickEvent>(OnRulesClick);
            _quitBtn?.RegisterCallback<ClickEvent>(OnQuitClick);
        }

        public void Detach()
        {
            _closeBtn?.UnregisterCallback<ClickEvent>(OnCloseClick);
            _returnBtn?.UnregisterCallback<ClickEvent>(OnReturnClick);
            _settingsBtn?.UnregisterCallback<ClickEvent>(OnSettingsClick);
            _rulesBtn?.UnregisterCallback<ClickEvent>(OnRulesClick);
            _quitBtn?.UnregisterCallback<ClickEvent>(OnQuitClick);

            _root = null;
            _closeBtn = null;
            _returnBtn = null;
            _settingsBtn = null;
            _rulesBtn = null;
            _quitBtn = null;
        }

        void OnCloseClick(ClickEvent _) => OnClose?.Invoke();
        void OnReturnClick(ClickEvent _) => OnReturnToMainMenu?.Invoke();
        void OnSettingsClick(ClickEvent _) => OnSettings?.Invoke();
        void OnRulesClick(ClickEvent _) => OnRules?.Invoke();
        void OnQuitClick(ClickEvent _) => OnQuit?.Invoke();
    }
}
