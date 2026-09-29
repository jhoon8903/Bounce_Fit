using System;
using System.Collections.Generic;
using Framework.Poolable.Scriptable;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace BounceFit.Mvp.Editor
{
    public static class MvpConfigAssets
    {
        public const string Folder="Assets/Projects/MVP/Configuration";
        public const string RootPath="Assets/Projects/MVP/Resources/MVP/DefaultConfig.asset";
        [MenuItem("Bounce Fit/MVP/Configuration/Create and Connect Defaults")]
        public static void Connect()
        {
            if(Application.isPlaying) { Debug.LogWarning("Exit Play mode to wire scene assets. Existing config values can be edited live.");return; }
            var config=EnsureConfig();
            foreach(var host in UnityEngine.Object.FindObjectsByType<MvpGame>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(host.config)continue; // Preserve deliberately assigned alternative sets.
                Undo.RecordObject(host,"Connect MVP configuration");host.config=config;
                EditorUtility.SetDirty(host);EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
                EditorSceneManager.SaveScene(host.gameObject.scene);
            }
            Selection.activeObject=config;
            Debug.Log("MVP feature configs connected. Select a referenced SO and edit its values during Play.");
        }
        [MenuItem("Bounce Fit/MVP/Configuration/Select Default Config")]
        public static void Select()=>Selection.activeObject=AssetDatabase.LoadAssetAtPath<MvpConfigSet>(RootPath);

        [MenuItem("Bounce Fit/MVP/Configuration/Verify Reconnect Preserves Tuning")]
        public static void VerifyReconnectPreservesTuning()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit Play mode before checking saved tuning.");
            var root=AssetDatabase.LoadAssetAtPath<MvpConfigSet>(RootPath);
            var pool=AssetDatabase.LoadAssetAtPath<EntityPoolContainer>(MvpPoolSetup.ContainerPath);
            if(!root || !pool)throw new InvalidOperationException("Existing configuration assets are required.");

            var assets=new List<UnityEngine.Object> { root, pool };
            foreach(var feature in new UnityEngine.Object[] { root.ball, root.player, root.stage, root.enemy,
                root.experience, root.progression, root.input, root.presentation })
                if(feature)assets.Add(feature);
            var before=new string[assets.Count];
            for(int i=0;i<assets.Count;i++)before[i]=EditorJsonUtility.ToJson(assets[i]);

            EnsureConfig();
            MvpPoolSetup.Connect();

            for(int i=0;i<assets.Count;i++)
                if(before[i]!=EditorJsonUtility.ToJson(assets[i]))
                    throw new InvalidOperationException("Reconnect changed tuning asset: "+AssetDatabase.GetAssetPath(assets[i]));
            Debug.Log("MVP configuration preservation PASS: Config Set, eight feature SOs and PoolContainer are unchanged.");
        }
        public static MvpConfigSet EnsureConfig()
        {
            // Reopening or rebuilding must never normalize or save an existing tuning asset.
            var root=AssetDatabase.LoadAssetAtPath<MvpConfigSet>(RootPath);
            if(root)return root;

            MakeFolder(Folder);MakeFolder("Assets/Projects/MVP/Resources/MVP");
            root=Ensure<MvpConfigSet>(RootPath);
            root.ball=Ensure<MvpBallConfig>(Folder+"/Ball.asset");
            root.player=Ensure<MvpPlayerConfig>(Folder+"/Player.asset");
            root.stage=Ensure<MvpStageConfig>(Folder+"/Stage.asset");
            root.enemy=Ensure<MvpEnemyConfig>(Folder+"/Enemy.asset");
            root.experience=Ensure<MvpExperienceConfig>(Folder+"/Experience.asset");
            root.progression=Ensure<MvpProgressionConfig>(Folder+"/Progression.asset");
            root.input=Ensure<MvpInputConfig>(Folder+"/Input.asset");
            root.presentation=Ensure<MvpPresentationConfig>(Folder+"/Presentation.asset");
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssetIfDirty(root);
            return root;
        }
        static T Ensure<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset)return asset;
            asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
        static void MakeFolder(string path)
        {
            string current="Assets";
            foreach(var part in path.Substring("Assets/".Length).Split('/'))
            { string next=current+"/"+part;if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,part);current=next; }
        }
    }
}
