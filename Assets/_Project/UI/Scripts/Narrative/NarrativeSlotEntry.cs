using System;
using Kismeta.Core.Domain;
using UnityEngine;

namespace Kismeta.UI.Narrative
{
    [Serializable]
    public sealed class NarrativeSlotEntry
    {
        [SerializeField] string _id = string.Empty;
        [SerializeField] Season _season;
        [SerializeField] NarrativeSlotTier _tier;
        [SerializeField] int _order;
        [TextArea(2, 6)] [SerializeField] string _beat = string.Empty;
        [TextArea(2, 4)] [SerializeField] string _charge = string.Empty;
        [TextArea(2, 4)] [SerializeField] string _stakes = string.Empty;
        [SerializeField] string[] _verbs = Array.Empty<string>();
        [SerializeField] string _breadcrumb = string.Empty;

        public string Id => _id;
        public Season Season => _season;
        public NarrativeSlotTier Tier => _tier;
        public int Order => _order;
        public string Beat => _beat;
        public string Charge => _charge;
        public string Stakes => _stakes;
        public string[] Verbs => _verbs ?? Array.Empty<string>();
        public string Breadcrumb => _breadcrumb;

        public bool HasStakes => !string.IsNullOrWhiteSpace(_stakes);

        public NarrativeSlotEntry() { }

        public NarrativeSlotEntry(
            string id,
            Season season,
            NarrativeSlotTier tier,
            int order,
            string beat,
            string charge,
            string stakes,
            string[] verbs,
            string breadcrumb = "")
        {
            _id = id;
            _season = season;
            _tier = tier;
            _order = order;
            _beat = beat;
            _charge = charge;
            _stakes = stakes ?? string.Empty;
            _verbs = verbs ?? Array.Empty<string>();
            _breadcrumb = breadcrumb ?? string.Empty;
        }
    }
}
