using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class FatefulWagerController : ScreenController
    {
        public override string ScreenId => ScreenIds.FatefulWager;

        public Action? OnCompleted;
        public Action? OnBack;

        static readonly string[] SignSlugs =
        {
            "aries", "taurus", "gemini", "cancer", "leo", "virgo",
            "libra", "scorpio", "sagittarius", "capricorn", "aquarius", "pisces"
        };

        readonly HashSet<string> _staked = new();
        string _selectedSlug = "pisces";
        string _lastTrayLayoutKey = "";

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        protected override void Unwire()
        {
            _lastTrayLayoutKey = "";
        }

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();

            foreach (var slug in SignSlugs)
            {
                var s = slug;
                var b = Btn($"sign-{s}");
                if (b != null) b.clicked += () => SelectSign(s);
            }

            var placeBtn = Btn("place-btn");
            if (placeBtn != null) placeBtn.clicked += OnPlace;
            var skipBtn = Btn("skip-btn");
            if (skipBtn != null) skipBtn.clicked += OnSkip;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            SelectSign(_selectedSlug);
            RebuildStakeTray();
            UpdateOddsLine();
            UpdatePlaceButton();
            NarrativeSlotBindings.BindById(Root, "winter.wager");
        }

        void SelectSign(string slug)
        {
            _selectedSlug = slug;
            foreach (var s in SignSlugs)
                Btn($"sign-{s}")?.EnableInClassList("sign-cell--selected", s == slug);
            UpdateOddsLine();
        }

        void RebuildStakeTray()
        {
            var tray = El("stake-cards");
            if (tray == null || _session == null) return;

            var layoutKey = BuildTrayLayoutKey();
            if (layoutKey == _lastTrayLayoutKey)
            {
                if (Lbl("stake-count") != null)
                    Lbl("stake-count")!.text = $"{_staked.Count} staked";
                return;
            }

            _lastTrayLayoutKey = layoutKey;
            tray.Clear();

            var player = _session.Players[_playerId];
            var db = _session.Rules?.CardDatabase;
            foreach (var id in player.Spread)
                AddStakeChip(tray, id, db);
            foreach (var id in player.Hand)
                AddStakeChip(tray, id, db);

            if (Lbl("stake-count") != null)
                Lbl("stake-count")!.text = $"{_staked.Count} staked";
        }

        void AddStakeChip(VisualElement tray, string cardId, ICardDatabase? db)
        {
            if (_session == null || !TapSwapBindings.IsMinorArcana(_session, cardId)) return;
            var inst = _session.GetCard(cardId);
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null) return;

            bool staked = _staked.Contains(cardId);
            var chip = CardChipFactory.CreateFromDefinition(def, selected: staked);
            chip.userData = cardId;
            CardChipFactory.WireTap(chip, () =>
            {
                if (_staked.Contains(cardId)) _staked.Remove(cardId);
                else _staked.Add(cardId);
                RebuildStakeTray();
                UpdatePlaceButton();
            });
            tray.Add(chip);
        }

        string BuildTrayLayoutKey()
        {
            if (_session == null || _playerId < 0) return "";
            var player = _session.Players[_playerId];
            var spread = new List<string>();
            foreach (var id in player.Spread)
            {
                if (TapSwapBindings.IsMinorArcana(_session, id))
                    spread.Add(id);
            }
            var hand = new List<string>();
            foreach (var id in player.Hand)
            {
                if (TapSwapBindings.IsMinorArcana(_session, id))
                    hand.Add(id);
            }
            spread.Sort();
            hand.Sort();
            var staked = new List<string>(_staked);
            staked.Sort();
            return string.Join(",", spread) + "|" + string.Join(",", hand) + "|" + string.Join(",", staked);
        }

        void UpdateOddsLine()
        {
            var line = Lbl("odds-line");
            if (line == null) return;
            var sign = SlugToSign(_selectedSlug);
            string signName = sign == ZodiacSign.None ? "—" : sign.ToString();
            line.text = _staked.Count > 0
                ? $"If {signName} rules next age, your {_staked.Count} staked card(s) double. If wrong, they're lost. Resolves next Spring."
                : "Select a sign and stake cards from your spread or hand. Skip if you prefer.";
        }

        void UpdatePlaceButton()
        {
            var place = Btn("place-btn");
            if (place == null) return;
            bool valid = _staked.Count > 0 && SlugToSign(_selectedSlug) != ZodiacSign.None;
            place.SetEnabled(valid);
            place.EnableInClassList("btn--disabled", !valid);
            UpdateOddsLine();
        }

        void OnPlace()
        {
            if (_bridge == null) return;
            var sign = SlugToSign(_selectedSlug);
            if (sign == ZodiacSign.None || _staked.Count == 0) return;
            if (_bridge.TrySubmit(new PlaceFatefulWagerCommand(_playerId, sign, new List<string>(_staked))))
            {
                _staked.Clear();
                OnCompleted?.Invoke();
            }
        }

        void OnSkip()
        {
            if (_bridge?.TrySubmit(new PassActionCommand(_playerId)) == true)
                OnCompleted?.Invoke();
        }

        static ZodiacSign SlugToSign(string slug) => slug switch
        {
            "aries" => ZodiacSign.Aries,
            "taurus" => ZodiacSign.Taurus,
            "gemini" => ZodiacSign.Gemini,
            "cancer" => ZodiacSign.Cancer,
            "leo" => ZodiacSign.Leo,
            "virgo" => ZodiacSign.Virgo,
            "libra" => ZodiacSign.Libra,
            "scorpio" => ZodiacSign.Scorpio,
            "sagittarius" => ZodiacSign.Sagittarius,
            "capricorn" => ZodiacSign.Capricorn,
            "aquarius" => ZodiacSign.Aquarius,
            "pisces" => ZodiacSign.Pisces,
            _ => ZodiacSign.None
        };
    }
}
