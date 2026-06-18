using System.Collections.Generic;

namespace Kismeta.UI.Components
{
    /// <summary>
    /// Tracks per-key reagent spend against supply and an optional cap.
    /// Reused by Place wards and later Fire / contest fee UIs.
    /// </summary>
    public sealed class ReagentStepper
    {
        readonly Dictionary<string, int> _spent = new();
        readonly Dictionary<string, int> _have = new();

        public int Cap = int.MaxValue;

        public int Total
        {
            get
            {
                int t = 0;
                foreach (var v in _spent.Values) t += v;
                return t;
            }
        }

        public int Of(string key) => _spent.TryGetValue(key, out var v) ? v : 0;

        public void SetHave(string key, int n) => _have[key] = n;

        public void Reset()
        {
            _spent.Clear();
        }

        public bool TryAdd(string key)
        {
            int have = _have.TryGetValue(key, out var h) ? h : int.MaxValue;
            if (Of(key) < have && Total < Cap)
            {
                _spent[key] = Of(key) + 1;
                return true;
            }
            return false;
        }

        public bool TryRemove(string key)
        {
            if (Of(key) > 0)
            {
                _spent[key] = Of(key) - 1;
                return true;
            }
            return false;
        }
    }
}
