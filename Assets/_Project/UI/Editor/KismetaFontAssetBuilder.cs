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

        // Latin display copy + zodiac names + common punctuation used in UI copy.
        private const string CommonUiCharset =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            " .,!?'\"-:;·—–()&" +
            "ARIES TAURUS GEMINI CANCER LEO VIRGO LIBRA SCORPIO SAGITTARIUS CAPRICORN AQUARIUS PISCES" +
            "KISMETAAlchemists of the Great Year";

        private const string ZodiacEmojiCharset = "♈♉♊♋♌♍♎♏♐♑♒♓";

        [MenuItem("Kismeta/UI/Create UI Font Assets")]
        public static void CreateAllFontAssets()
        {
            var created = 0;
            // Futura first so display fonts can use it as fallback in the same batch.
            created += CreateFontAsset("FuturaCyrillicBook.ttf") ? 1 : 0;
            created += CreateFontAsset("FuturaCyrillicDemi.ttf") ? 1 : 0;
            created += CreateFontAsset("Amarante-Regular.ttf") ? 1 : 0;
            created += CreateFontAsset("GermaniaOne-Regular.ttf") ? 1 : 0;
            created += CreateFontAsset("NotoColorEmoji-Regular.ttf", ZodiacEmojiCharset) ? 1 : 0;
            created += CreateFontAsset("tabler-icons.ttf") ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Kismeta.UI] Created/updated {created} FontAsset(s) in {FontsDir}. USS must use -unity-font-definition to reference them.");
        }

        static bool CreateFontAsset(string ttfFileName, string warmCharset = null)
        {
            var charset = string.IsNullOrEmpty(warmCharset) ? CommonUiCharset : warmCharset;
            var ttfPath = $"{FontsDir}/{ttfFileName}";
            var assetPath = $"{FontsDir}/{Path.GetFileNameWithoutExtension(ttfFileName)}.asset";
            var assetName = Path.GetFileNameWithoutExtension(ttfFileName);

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

            fontAsset.name = assetName;
            NameSubAssets(fontAsset, assetName);

            WarmAtlas(fontAsset, assetPath, charset);

            AssetDatabase.CreateAsset(fontAsset, assetPath);
            PersistSubAssets(fontAsset);

            if (ttfFileName.StartsWith("Amarante") || ttfFileName.StartsWith("Germania"))
            {
                var futura = AssetDatabase.LoadAssetAtPath<FontAsset>($"{FontsDir}/FuturaCyrillicBook.asset");
                if (futura != null && fontAsset.fallbackFontAssetTable != null)
                    fontAsset.fallbackFontAssetTable.Add(futura);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();

            var reloaded = AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath);
            var atlasOk = reloaded != null
                && reloaded.atlasTextures != null
                && reloaded.atlasTextures.Length > 0
                && reloaded.atlasTextures[0] != null
                && reloaded.material != null;
            var glyphCount = reloaded != null && reloaded.glyphTable != null ? reloaded.glyphTable.Count : 0;

            if (!atlasOk)
            {
                Debug.LogError($"[Kismeta.UI] FontAsset save incomplete: {assetPath} — atlas/material sub-assets missing after reload.");
                return false;
            }

            Debug.Log($"[Kismeta.UI] FontAsset ready: {assetPath} (material={reloaded.material.name}, atlas={reloaded.atlasTextures.Length}, glyphs={glyphCount})");
            return true;
        }

        static void NameSubAssets(FontAsset fontAsset, string assetName)
        {
            if (fontAsset.material != null)
                fontAsset.material.name = $"{assetName} Material";

            if (fontAsset.atlasTextures == null)
                return;

            for (var i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                var texture = fontAsset.atlasTextures[i];
                if (texture == null)
                    continue;

                texture.name = fontAsset.atlasTextures.Length == 1
                    ? $"{assetName} Atlas"
                    : $"{assetName} Atlas {i}";
            }
        }

        static void PersistSubAssets(FontAsset fontAsset)
        {
            if (fontAsset.material != null)
            {
                fontAsset.material.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                EditorUtility.SetDirty(fontAsset.material);
            }

            if (fontAsset.atlasTextures == null)
                return;

            foreach (var texture in fontAsset.atlasTextures)
            {
                if (texture == null)
                    continue;

                texture.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(texture, fontAsset);
                EditorUtility.SetDirty(texture);
            }
        }

        static void WarmAtlas(FontAsset fontAsset, string assetPath, string charset)
        {
            try
            {
                fontAsset.TryAddCharacters(charset, out string missing);
                if (string.IsNullOrEmpty(missing))
                    return;

                Debug.LogWarning($"[Kismeta.UI] {assetPath}: could not bake [{missing}]");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Kismeta.UI] WarmAtlas failed for {assetPath}: {ex.Message}");
            }
        }
    }
}
