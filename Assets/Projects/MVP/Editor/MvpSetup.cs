using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BounceFit.Mvp.Editor
{
    public static class MvpSetup
    {
        const string Root="Assets/Projects/MVP";
        [MenuItem("Bounce Fit/MVP/Set Landscape Game View")]
        public static void SetLandscapeView()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singleton.GetProperty("instance").GetValue(null);
            var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{System.Enum.Parse(groupType,"Standalone")});
            var labels=(string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group,null);
            for(int i=0;i<labels.Length;i++) if(labels[i].Contains("16:9"))
            {
                var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
                view.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(view,i);
                view.Repaint();return;
            }
            Debug.LogWarning("No built-in 16:9 Game View preset found. Select 16:9 in Game View manually.");
        }

        [MenuItem("Bounce Fit/MVP/Create or Open Playable Scene")]
        public static void CreateScene()
        {
            var config=MvpConfigAssets.EnsureConfig();
            const string path=Root+"/ForestMvp.unity";
            if(System.IO.File.Exists(path)) { EditorSceneManager.OpenScene(path); MvpConfigAssets.Connect(); MvpPoolSetup.Connect(); return; }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var renderer=ScriptableObject.CreateInstance<UniversalRendererData>(); renderer.name="MVP Forward Renderer";
            AssetDatabase.CreateAsset(renderer,Root+"/MvpRenderer.asset");
            var pipeline=UniversalRenderPipelineAsset.Create(renderer);pipeline.name="MVP Pipeline";
            pipeline.msaaSampleCount=2;pipeline.supportsHDR=false;
            AssetDatabase.CreateAsset(pipeline,Root+"/MvpPipeline.asset");
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.name="MVP Solid";
            AssetDatabase.CreateAsset(material,Root+"/MvpSolid.mat");
            var spriteMaterial=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));spriteMaterial.name="MVP Sprites";
            AssetDatabase.CreateAsset(spriteMaterial,Root+"/MvpSprites.mat");
            var host=new GameObject("Forest MVP").AddComponent<MvpGame>();host.config=config;host.pipeline=pipeline;host.solidMaterial=material;host.spriteMaterial=spriteMaterial;
            var camera=new GameObject("Main Camera",typeof(Camera));camera.tag="MainCamera";camera.transform.position=new Vector3(0,22,-17);camera.transform.rotation=Quaternion.Euler(48,0,0);camera.GetComponent<Camera>().fieldOfView=47;
            var light=new GameObject("Sun",typeof(Light));light.GetComponent<Light>().type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,-25,0);
            EditorSceneManager.SaveScene(scene,path);
            var scenes=new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(!scenes.Exists(s=>s.path==path)) {scenes.Insert(0,new EditorBuildSettingsScene(path,true));EditorBuildSettings.scenes=scenes.ToArray();}
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            Selection.activeGameObject=host.gameObject;
            MvpPoolSetup.Connect();
            Debug.Log("Bounce Fit MVP ready: open ForestMvp and press Play.");
        }
    }
}
