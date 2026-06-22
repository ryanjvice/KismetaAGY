using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace Kismeta.UI.Editor
{
    /// <summary>
    /// Builds TextCore FontAssets with atlas sub-assets. Unity 6's default panel theme
    /// applies -unity-font-definition, which overrides legacy -unity-font TTF rules in USS.
    /// </summary>
    public static class KismetaFontAssetBuilder
    {
        private const string FontsDir = "Assets/_Project/UI/Fonts";
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 4;
        private const int AtlasSize = 1024;

        [MenuItem("Kismeta/UI/Create UI Font Assets")]
        public static void CreateAllFontAssets()
        {
            var created = 0;
            created += CreateFontAsset("Amarante-Regular.ttf") ? 1 : 0;
            created += CreateFontAsset("GermaniaOne-Regular.ttf") ? 1 : 0;
            created += CreateFontAsset("FuturaCyrillicBook.ttf") ? 1 : 0;
            created += CreateFontAsset("FuturaCyrillicDemi.ttf") ? 1 : 0;
            created += CreateFontAsset("NotoColorEmoji-Regular.ttf") ? 1 : 0;
            created += CreateFontAsset("tabler-icons.ttf") ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Kismeta.UI] Created/updated {created} FontAsset(s) in {FontsDir}. USS must use -unity-font-definition to reference them.");
        }

        static bool CreateFontAsset(string ttfFileName)
        {
            var ttfPath = $"{FontsDir}/{ttfFileName}";
            var assetPath = $"{FontsDir}/{Path.GetFileNameWithoutExtension(ttfFileName)}.asset";

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogError($"[Kismeta.UI] Font TTF not found: {ttfPath}");
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var fontAsset = FontAsset.CreateFontAsset(
                font,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SMOOTH,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic);

            if (fontAsset == null)
            {
                Debug.LogError($"[Kismeta.UI] FontAsset.CreateFontAsset failed for {ttfPath}. Enable Include Font Data on the TTF importer.");
                return false;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(ttfFileName);
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            if (fontAsset.atlasTextures != null)
            {
                foreach (var texture in fontAsset.atlasTextures)
                {
                    if (texture != null && texture != fontAsset)
                        AssetDatabase.AddObjectToAsset(texture, fontAsset);
                }
            }

            if (fontAsset.material != null && fontAsset.material != fontAsset)
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            Debug.Log($"[Kismeta.UI] FontAsset ready: {assetPath} (material={(fontAsset.material != null)}, atlas={(fontAsset.atlasTextures?.Length ?? 0)})");
            return true;
        }
    }
}
