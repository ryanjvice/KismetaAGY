using System.IO;
using Kismeta.UI.Components;
using UnityEditor;
using UnityEngine;

namespace Kismeta.UI.Editor
{
    public static class UiArtCatalogBuilder
    {
        const string AssetPath = "Assets/_Project/UI/Resources/UiArtCatalog.asset";

        [MenuItem("Kismeta/UI/Create UI Art Catalog")]
        public static void CreateCatalog()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath)!);

            var catalog = AssetDatabase.LoadAssetAtPath<UiArtCatalog>(AssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UiArtCatalog>();
                AssetDatabase.CreateAsset(catalog, AssetPath);
            }

            var so = new SerializedObject(catalog);
            so.FindProperty("_tableFeltVignette").objectReferenceValue = LoadSprite("Assets/_Project/Art/UI/tableFelt_vignette.png");
            so.FindProperty("_heroBurgundy").objectReferenceValue = LoadSprite("Assets/_Project/Art/GameSplash/splashBackground.png");
            so.FindProperty("_starChart").objectReferenceValue = LoadSprite("Assets/_Project/Art/GameSplash/starChart.png");
            so.FindProperty("_zodiacWheel").objectReferenceValue = LoadSprite("Assets/_Project/Art/GameSplash/zodiacWheel.png");
            so.FindProperty("_mantleRing").objectReferenceValue = LoadSprite("Assets/_Project/Art/GameSplash/mantleRing.png");
            so.FindProperty("_crucibleForge").objectReferenceValue = LoadSprite("Assets/_Project/Art/GameSplash/crucibleForge.png");
            so.FindProperty("_kismetaMetallic").objectReferenceValue = LoadSprite("Assets/_Project/Art/GameSplash/kismeta_metallic_1.png");
            so.FindProperty("_alchemistsMetallic").objectReferenceValue = LoadSprite("Assets/_Project/Art/GameSplash/alchemists_metallic.png");
            so.FindProperty("_cauldronWands").objectReferenceValue = LoadSprite("Assets/_Project/Art/cauldrons/cauldron_Wands.png");
            so.FindProperty("_cauldronCups").objectReferenceValue = LoadSprite("Assets/_Project/Art/cauldrons/cauldron_Cups.png");
            so.FindProperty("_cauldronSwords").objectReferenceValue = LoadSprite("Assets/_Project/Art/cauldrons/cauldron_Swords.png");
            so.FindProperty("_cauldronPentacles").objectReferenceValue = LoadSprite("Assets/_Project/Art/cauldrons/cauldron_Pentacles.png");

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Kismeta.UI] UI art catalog saved to {AssetPath}.");
        }

        static Sprite? LoadSprite(string assetPath)
        {
            if (!File.Exists(assetPath))
                return null;

            EnsureSpriteImport(assetPath);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
                return sprite;

            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (obj is Sprite subSprite)
                    return subSprite;
            }

            return null;
        }

        static void EnsureSpriteImport(string assetPath)
        {
            if (!File.Exists(assetPath))
                return;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            if (importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
    }
}
