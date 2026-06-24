using System.IO;
using Kismeta.UI.Components;
using UnityEditor;
using UnityEngine;

namespace Kismeta.UI.Editor
{
    public static class CrucibleForgeArtBuilder
    {
        const string AssetPath = "Assets/_Project/UI/Resources/CrucibleForgeArt.asset";
        const string CrucibleDir = "Assets/_Project/UI/img/crucible";

        static readonly string[] SpriteFileNames =
        {
            "forge_0",
            "forge_Lead",
            "forge_2",
            "forge_Bronze",
            "forge_4",
            "forge_Silver",
            "forge_6",
            "forge_Gold",
            "forge_Altar",
        };

        [MenuItem("Kismeta/UI/Create Crucible Forge Art Catalog")]
        public static void CreateCatalog()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath)!);

            var catalog = AssetDatabase.LoadAssetAtPath<CrucibleForgeArt>(AssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CrucibleForgeArt>();
                AssetDatabase.CreateAsset(catalog, AssetPath);
            }

            var so = new SerializedObject(catalog);
            var array = so.FindProperty("_byPosition");
            array.arraySize = SpriteFileNames.Length;

            for (int i = 0; i < SpriteFileNames.Length; i++)
            {
                var path = $"{CrucibleDir}/{SpriteFileNames[i]}.png";
                array.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(path);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Kismeta.UI] Crucible forge art catalog saved to {AssetPath}.");
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
