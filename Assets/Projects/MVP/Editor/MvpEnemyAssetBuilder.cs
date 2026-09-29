using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BounceFit.Mvp.Editor
{
    /// <summary>
    /// Creates presentation-only enemy prefabs from the shared-canvas enemy art.
    /// Each generated child remains at the root origin so the source canvases stay aligned.
    /// </summary>
    public static class MvpEnemyAssetBuilder
    {
        private const string ArtRoot = "Assets/Projects/MVP/Art/Enemies/Stage1";
        private const string PrefabRoot = "Assets/Projects/MVP/Prefabs/Enemies/Stage1";

        private static readonly string[] EnemyIds =
        {
            "E01_LoggingCart", "E02_HunterCart", "E03_Bulldozer", "E04_LogTruck",
            "E05_Sidecar", "E06_SurveyRover", "E07_ArmoredRover", "E08_DemolitionCarrier",
            "E09_Tanker", "E10_RepairSidecar", "E11_CommandCart", "E12_DrillTruck",
            "E13_RoadRoller", "E14_CoilRover", "E15_SignalSidecar"
        };

        private static readonly Part[] EnemyParts =
        {
            new Part("Rear", 0), new Part("Rider", 10), new Part("Front", 20), new Part("Equipment", 30)
        };

        private static readonly Part[] BossParts =
        {
            new Part("Base", 0), new Part("Core", 10), new Part("LeftTurret", 20), new Part("RightTurret", 20)
        };

        [MenuItem("Bounce Fit/MVP/Art/Create Missing Stage 1 Enemy Prefabs")]
        public static void CreateMissing()
        {
            AssetDatabase.Refresh();
            EnsureFolder(PrefabRoot);

            var missing = new List<string>();
            foreach (var enemyId in EnemyIds)
                CreatePrefabIfComplete(enemyId, EnemyParts, 512, missing);

            CreatePrefabIfComplete("B01_SawmillTower", BossParts, 1024, missing);
            AssetDatabase.SaveAssets();

            if (missing.Count == 0)
                Debug.Log("[MVP] Created all missing Stage 1 enemy prefabs.");
            else
                Debug.LogWarning("[MVP] Stage 1 enemy prefab creation skipped incomplete sets:\n" + string.Join("\n", missing));
        }

        private static void CreatePrefabIfComplete(string id, Part[] parts, int expectedSize, List<string> missing)
        {
            var prefabPath = PrefabRoot + "/" + id + ".prefab";
            var sprites = new Sprite[parts.Length];
            var absentParts = new List<string>();
            for (var i = 0; i < parts.Length; i++)
            {
                var sourcePath = ArtRoot + "/" + id + "/" + parts[i].Name + ".png";
                if (!ConfigureSpriteImport(sourcePath, expectedSize))
                {
                    absentParts.Add(parts[i].Name);
                    continue;
                }

                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
                if (sprites[i] == null)
                    absentParts.Add(parts[i].Name);
            }

            if (absentParts.Count > 0)
            {
                missing.Add(id + ": " + string.Join(", ", absentParts));
                return;
            }

            // Existing prefabs are user-owned. Their source art is still imported consistently,
            // but the prefab itself is never opened or rewritten by this builder.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return;

            var root = new GameObject(id);
            root.AddComponent<SortingGroup>();
            try
            {
                for (var i = 0; i < parts.Length; i++)
                    AddSpritePart(root.transform, id, parts[i], sprites[i]);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static bool ConfigureSpriteImport(string path, int expectedSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return false;

            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);

            var changed = importer.textureType != TextureImporterType.Sprite ||
                          importer.spriteImportMode != SpriteImportMode.Single ||
                          importer.spritePixelsPerUnit != 256 ||
                          textureSettings.spriteAlignment != (int)SpriteAlignment.Center ||
                          textureSettings.spriteMeshType != SpriteMeshType.FullRect ||
                          importer.filterMode != FilterMode.Bilinear ||
                          importer.mipmapEnabled ||
                          importer.wrapMode != TextureWrapMode.Clamp ||
                          !importer.alphaIsTransparency ||
                          !importer.sRGBTexture ||
                          importer.textureCompression != TextureImporterCompression.Uncompressed;

            if (changed)
            {
                textureSettings.spriteAlignment = (int)SpriteAlignment.Center;
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 256;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return texture != null && texture.width == expectedSize && texture.height == expectedSize;
        }

        private static void AddSpritePart(Transform parent, string enemyId, Part part, Sprite sprite)
        {
            var child = new GameObject(part.Name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = Vector3.zero;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = GetSortingOrder(enemyId, part);
        }

        private static int GetSortingOrder(string enemyId, Part part)
        {
            // Cargo and rear-mounted gear must remain behind the rider. These IDs mirror
            // the production_v002 layer recipes; keep this explicit so prefab generation
            // has no dependency on source-only JSON files.
            if (part.Name == "Equipment" &&
                (enemyId == "E04_LogTruck" || enemyId == "E06_SurveyRover" ||
                 enemyId == "E09_Tanker" || enemyId == "E14_CoilRover"))
                return 5;

            return part.SortingOrder;
        }

        private static void EnsureFolder(string assetPath)
        {
            var current = "Assets";
            foreach (var segment in assetPath.Substring("Assets/".Length).Split('/'))
            {
                var next = current + "/" + segment;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segment);
                current = next;
            }
        }

        private readonly struct Part
        {
            public readonly string Name;
            public readonly int SortingOrder;

            public Part(string name, int sortingOrder)
            {
                Name = name;
                SortingOrder = sortingOrder;
            }
        }
    }
}
