using System;
using Framework.Poolable.Authoring;
using Framework.Poolable.Scriptable;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scenes;

namespace BounceFit.Mvp.Editor
{
    public static class MvpPoolSetup
    {
        public const string ContainerPath = "Assets/Projects/Assets/Configuration/PoolContainer.asset";
        public const string ScenePath = "Assets/Projects/MVP/ForestCombat.unity";
        const string PrefabFolder = "Assets/Projects/MVP/Prefabs";

        [MenuItem("Bounce Fit/MVP/Connect Baked Combat Pools")]
        public static void Connect()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            var host = UnityEngine.Object.FindFirstObjectByType<MvpGame>();
            if (!host) throw new InvalidOperationException("Open ForestMvp first.");
            var main = host.gameObject.scene;
            // Entities Graphics initializes before MvpGame.Awake; SRP must already be selected.
            if (host.pipeline) UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = host.pipeline;
            var container = AssetDatabase.LoadAssetAtPath<EntityPoolContainer>(ContainerPath);
            if (!container) throw new InvalidOperationException("PoolContainer asset could not be loaded. Preserve its GUID and fix the script reference.");
            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Projects/MVP", "Prefabs");
            var serialized = new SerializedObject(container);
            var entries = serialized.FindProperty("entries");
            bool addedAny = false;
            for (int i = 0; i < MvpCombatPools.Names.Length; i++)
            {
                string id = MvpCombatPools.Names[i];
                int index = -1;
                for (int j = 0; j < entries.arraySize; j++)
                    if (entries.GetArrayElementAtIndex(j).FindPropertyRelative("id").stringValue == id) { index = j; break; }
                // Existing entries may contain designer-selected prefabs and capacities.
                if (index >= 0) continue;

                string path = PrefabFolder + "/" + id + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab)
                {
                    var source = new GameObject(id);
                    var authoring = source.AddComponent<MvpCombatPrefabAuthoring>();
                    authoring.kind = MvpCombatPools.Kinds[i];
                    authoring.variant = MvpCombatPools.Variants[i];
                    prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
                    UnityEngine.Object.DestroyImmediate(source);
                }
                index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("id").stringValue = id;
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                entry.FindPropertyRelative("initialCapacity").intValue = MvpCombatPools.WarmCounts[i];
                entry.FindPropertyRelative("maxCapacity").intValue = 0; // Expand on demand; arena limits bound gameplay.
                addedAny = true;
            }
            if (addedAny)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(container);
            }

            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                var authoring = new GameObject("Combat Pool Catalog").AddComponent<EntityPoolAuthoring>();
                var data = new SerializedObject(authoring);
                data.FindProperty("poolSet").objectReferenceValue = container;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorSceneManager.CloseScene(scene, true);
            }
            SceneManager.SetActiveScene(main);
            SubScene sub = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<SubScene>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.SceneAsset == AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) sub = candidate;
            if (!sub)
            {
                sub = new GameObject("Forest Combat SubScene").AddComponent<SubScene>();
                sub.SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                sub.AutoLoadScene = true;
                EditorSceneManager.MarkSceneDirty(main);
                EditorSceneManager.SaveScene(main);
            }
            Selection.activeObject = container;
            Debug.Log("ForestCombat SubScene connected: 11 PoolContainer prefab definitions bake and prewarm before gameplay.");
        }
    }
}
