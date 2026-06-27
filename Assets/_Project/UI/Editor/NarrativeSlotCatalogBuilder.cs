using System.IO;
using Kismeta.UI.Narrative;
using UnityEditor;
using UnityEngine;

namespace Kismeta.UI.Editor
{
    public static class NarrativeSlotCatalogBuilder
    {
        const string AssetPath = "Assets/_Project/UI/Resources/NarrativeSlotCatalog.asset";

        [MenuItem("Kismeta/UI/Build Narrative Slot Catalog")]
        public static void BuildCatalog()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath)!);

            var catalog = AssetDatabase.LoadAssetAtPath<NarrativeSlotCatalog>(AssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<NarrativeSlotCatalog>();
                AssetDatabase.CreateAsset(catalog, AssetPath);
            }

            catalog.SetEntries(NarrativeSlotCatalogDefaults.AllEntries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            NarrativeSlotCatalog.ClearCache();

            Debug.Log($"[Kismeta.UI] Narrative slot catalog saved to {AssetPath} " +
                      $"({NarrativeSlotCatalogDefaults.AllEntries.Count} entries).");
        }
    }
}
