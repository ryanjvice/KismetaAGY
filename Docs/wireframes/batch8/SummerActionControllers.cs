using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>Controller for SummerSheets.uxml — the two action menus.</summary>
    public class SummerSheetsController : ScreenController
    {
        public System.Action OnCraft, OnLight, OnBuild, OnWard;     // craft & build
        public System.Action OnTrade, OnDuel, OnGambit;             // consort
        public System.Action OnClose;

        protected override void Wire()
        {
            Btn("act-craft").clicked  += () => OnCraft?.Invoke();
            Btn("act-light").clicked  += () => OnLight?.Invoke();
            Btn("act-build").clicked  += () => OnBuild?.Invoke();
            Btn("act-ward").clicked   += () => OnWard?.Invoke();
            Btn("act-trade").clicked  += () => OnTrade?.Invoke();
            Btn("act-duel").clicked   += () => OnDuel?.Invoke();
            Btn("act-gambit").clicked += () => OnGambit?.Invoke();
            Btn("cb-close").clicked   += () => OnClose?.Invoke();
            Btn("con-close").clicked  += () => OnClose?.Invoke();
        }

        /// <summary>Show one sheet or the other.</summary>
        public void ShowCraftBuild(bool craftBuild)
        {
            El("craftbuild-sheet").style.display = craftBuild ? DisplayStyle.Flex : DisplayStyle.None;
            El("consort-sheet").style.display    = craftBuild ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// Reusable reagent-cost stepper logic. Used by Place wards and (later)
    /// Fire / Gambit fee / Opposition fee. Tracks a per-key spend against a cap.
    /// </summary>
    public class ReagentStepper
    {
        readonly Dictionary<string, int> _spent = new();
        readonly Dictionary<string, int> _have = new();
        public int Cap = int.MaxValue;

        public int Total { get { int t = 0; foreach (var v in _spent.Values) t += v; return t; } }
        public int Of(string key) => _spent.TryGetValue(key, out var v) ? v : 0;

        public void SetHave(string key, int n) => _have[key] = n;

        public bool TryAdd(string key)
        {
            int have = _have.TryGetValue(key, out var h) ? h : int.MaxValue;
            if (Of(key) < have && Total < Cap) { _spent[key] = Of(key) + 1; return true; }
            return false;
        }
        public bool TryRemove(string key)
        {
            if (Of(key) > 0) { _spent[key] = Of(key) - 1; return true; }
            return false;
        }
    }

    /// <summary>Controller for CraftReagent.uxml (#19) + result (#20).</summary>
    public class CraftReagentController : ScreenController
    {
        public System.Action OnForge, OnBack, OnDone;
        const int Need = 3;
        int _picked = 2; // demo seed

        protected override void Wire()
        {
            Btn("back-btn").clicked += () => OnBack?.Invoke();
            Btn("forge-btn").clicked += TryForge;
            Btn("result-done-btn").clicked += () => OnDone?.Invoke();

            foreach (var r in new[] { "sulphur", "vitriol", "aqua", "salt" })
            {
                var key = r;
                Btn($"pick-{r}")?.RegisterCallback<ClickEvent>(_ => PickReagent(key));
            }
            RefreshForgeBtn();
        }

        void PickReagent(string key)
        {
            foreach (var r in new[] { "sulphur", "vitriol", "aqua", "salt" })
                Btn($"pick-{r}")?.EnableInClassList("reagent-pick--active", r == key);
            // NATIVE: re-filter the card pool by the reagent's suit here.
        }

        public void SetPicked(int n) { _picked = n; RefreshForgeBtn(); }

        void RefreshForgeBtn()
        {
            var b = Btn("forge-btn");
            Lbl("pick-count").text = $"{_picked} / {Need}";
            if (_picked >= Need)
            {
                b.RemoveFromClassList("btn--disabled"); b.AddToClassList("btn--primary");
                b.text = "Forge the reagent";
            }
            else
            {
                b.AddToClassList("btn--disabled"); b.RemoveFromClassList("btn--primary");
                b.text = $"Forge — needs {Need - _picked} more card";
            }
        }

        void TryForge()
        {
            if (_picked < Need) return;
            El("forge-result").style.display = DisplayStyle.Flex;
            OnForge?.Invoke();
        }
    }

    /// <summary>
    /// One crucible slot's data. Per the Crucible Codex, a reagent needs ANY 3
    /// of a planet and lights a colored cauldron. (Salt is excluded — it needs
    /// no cauldron and lives on the Craft screen.)
    /// </summary>
    public class CrucibleSlotVM
    {
        public string Id;         // "A".."D"
        public string Reagent;    // "Sulphur", "Aqua Regia", ...
        public string Planet;     // "Mars", "Venus", "Jupiter", "Saturn"
        public string Cauldron;   // "Red", "Blue", "Green", "Yellow"  (also the USS color key, lowercased)
        public int Need = 3;
        public int Have;          // matching cards currently in spread/hand
        public bool Active;       // already lit / non-tappable
        public string ColorKey => Cauldron != null ? Cauldron.ToLower() : "red";
        public bool Ready => !Active && Have >= Need;
    }

    /// <summary>
    /// Controller for ActivateCard.uxml (#21) — four-slot selection + formula
    /// reveal. Tapping a dormant slot selects it, populates #formula-detail for
    /// that slot, and arms #activate-btn when the formula is complete.
    /// </summary>
    public class ActivateController : ScreenController
    {
        public System.Action OnBack;
        public System.Action<string> OnActivate;   // arg: slot id

        // Default seed mirrors the codex; replace Have/Active from game state.
        public System.Collections.Generic.List<CrucibleSlotVM> Slots = new()
        {
            new CrucibleSlotVM{ Id="A", Reagent="Sulphur",     Planet="Mars",   Cauldron="Red",    Have=2 },
            new CrucibleSlotVM{ Id="B", Reagent="Aqua Regia",  Planet="Venus",  Cauldron="Blue",   Have=1 },
            new CrucibleSlotVM{ Id="C", Reagent="Vitriol",     Planet="Jupiter",Cauldron="Green",  Active=true },
            new CrucibleSlotVM{ Id="D", Reagent="Quicksilver", Planet="Saturn", Cauldron="Yellow", Have=3 },
        };

        string _selected;

        protected override void Wire()
        {
            Btn("back-btn").clicked += () => OnBack?.Invoke();
            foreach (var s in Slots)
            {
                var id = s.Id;
                var btn = Btn($"slot-{id}");
                if (btn != null && !s.Active) btn.clicked += () => Select(id);
            }
            Btn("activate-btn").clicked += () =>
            {
                if (!Btn("activate-btn").ClassListContains("btn--disabled"))
                    OnActivate?.Invoke(_selected);
            };
        }

        protected override void Bind()
        {
            RefreshSlots();
            // hide the reveal until a slot is chosen
            if (El("formula-detail") != null)
                El("formula-detail").style.display = DisplayStyle.None;
            DisableActivate("Select a card to activate");
        }

        void RefreshSlots()
        {
            foreach (var s in Slots)
            {
                var btn = Btn($"slot-{s.Id}");
                if (btn == null) continue;
                bool sel = s.Id == _selected;
                btn.EnableInClassList("crucible-slot--selected", sel);
                btn.EnableInClassList($"slot-sel--{s.ColorKey}", sel);

                var prog = Lbl($"slot-{s.Id}-progress");
                if (prog != null && !s.Active)
                    prog.text = $"{s.Have} / {s.Need} {s.Planet}";
            }
        }

        void Select(string id)
        {
            _selected = id;
            RefreshSlots();
            var s = Slots.Find(x => x.Id == id);
            if (s == null) return;

            if (El("formula-detail") != null)
                El("formula-detail").style.display = DisplayStyle.Flex;

            Lbl("formula-reagent").text = s.Reagent;
            Lbl("formula-req").text = $"any 3 {s.Planet} · {s.Cauldron} cauldron";
            Lbl("formula-count").text = $"{s.Have}/{s.Need}";
            Lbl("formula-consequence").text =
                $"Activating moves the coal to the {s.Cauldron} cauldron, lighting it — then you can craft {s.Reagent}.";

            // NATIVE: rebuild #formula-chips here — Have filled chips (fslot-fill--COLOR
            // + gold border) then (Need - Have) dashed .formula-slot--empty chips.

            if (s.Ready)
            {
                Lbl("formula-status").text = $"formula complete — ready to light the {s.Cauldron} cauldron";
                ArmActivate($"Activate · light the {s.Cauldron} cauldron");
            }
            else
            {
                int missing = s.Need - s.Have;
                Lbl("formula-status").text =
                    $"need {missing} more {s.Planet} card{(missing > 1 ? "s" : "")} from your spread or hand";
                DisableActivate($"Need {missing} more {s.Planet} to activate");
            }
        }

        void ArmActivate(string label)
        {
            var b = Btn("activate-btn");
            b.RemoveFromClassList("btn--disabled");
            b.AddToClassList("btn--primary");
            b.text = label;
        }

        void DisableActivate(string label)
        {
            var b = Btn("activate-btn");
            b.RemoveFromClassList("btn--primary");
            b.AddToClassList("btn--disabled");
            b.text = label;
        }
    }

    /// <summary>Controller for BuildHouse.uxml (#23).</summary>
    public class BuildHouseController : ScreenController
    {
        public System.Action OnBuild, OnBack;
        protected override void Wire()
        {
            Btn("back-btn").clicked += () => OnBack?.Invoke();
            Btn("build-btn").clicked += () => OnBuild?.Invoke();
        }
    }

    /// <summary>Controller for PlaceWards.uxml (#24).</summary>
    public class PlaceWardsController : ScreenController
    {
        public System.Action OnSeal, OnBack;
        int _supply = 4;

        protected override void Wire()
        {
            Btn("back-btn").clicked += () => OnBack?.Invoke();
            Btn("seal-btn").clicked += () => OnSeal?.Invoke();
            HookCard("a");
            HookCard("h");
        }

        void HookCard(string id)
        {
            Btn($"ward-{id}-plus").clicked += () => Adjust(id, +1);
            Btn($"ward-{id}-minus").clicked += () => Adjust(id, -1);
        }

        void Adjust(string id, int delta)
        {
            var lbl = Lbl($"ward-{id}-val");
            int v = int.Parse(lbl.text);
            if (delta > 0 && _supply <= 0) return;
            if (delta < 0 && v <= 0) return;
            v += delta; _supply -= delta;
            lbl.text = v.ToString();
            if (Lbl("ward-supply") != null) Lbl("ward-supply").text = $"{_supply} left";
        }
    }

    /// <summary>Controller for EndSummer.uxml (#27).</summary>
    public class EndSummerController : ScreenController
    {
        public System.Action OnKeep, OnEnd;
        protected override void Wire()
        {
            Btn("keep-btn").clicked += () => OnKeep?.Invoke();
            Btn("close-btn").clicked += () => OnKeep?.Invoke();
            Btn("end-btn").clicked += () => OnEnd?.Invoke();
        }
    }

    /// <summary>Back-fill: controller for SummerMainScene.uxml (#17).</summary>
    public class SummerSceneController : ScreenController
    {
        public System.Action OnCraftBuild, OnConsort, OnActivate, OnOpenCardTable;
        protected override void Wire()
        {
            Btn("craftbuild-btn").clicked += () => OnCraftBuild?.Invoke();
            Btn("consort-btn").clicked    += () => OnConsort?.Invoke();
            Btn("activate-btn").clicked   += () => OnActivate?.Invoke();
            Btn("menu-btn").clicked       += () => OnOpenCardTable?.Invoke();
        }
    }
}
