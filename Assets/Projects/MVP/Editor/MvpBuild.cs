using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace BounceFit.Mvp.Editor
{
    public static class MvpBuild
    {
        [MenuItem("Bounce Fit/MVP/Build Mac Test Player")]
        public static void Schedule() => Build();

        static void Build()
        {
            var settings=AddressableAssetSettingsDefaultObject.Settings;
            var previousAddressables=settings?settings.BuildAddressablesWithPlayerBuild:AddressableAssetSettings.PlayerBuildOption.PreferencesValue;
            var previousGraphics=GraphicsSettings.defaultRenderPipeline;
            var previousQuality=QualitySettings.renderPipeline;
            var previousMode=PlayerSettings.fullScreenMode;
            var previousName=PlayerSettings.productName;
            Directory.CreateDirectory("Captures/MVP");
            try
            {
                // The standalone MVP uses scene references and Resources only. Its build does
                // not need the separate framework's Lobby/Game Addressable content.
                if(settings) settings.BuildAddressablesWithPlayerBuild=AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
                GraphicsSettings.defaultRenderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Projects/MVP/MvpPipeline.asset");
                QualitySettings.renderPipeline=GraphicsSettings.defaultRenderPipeline;
                PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
                PlayerSettings.productName="Forest Archmage MVP";
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{"Assets/Projects/MVP/ForestMvp.unity"},
                    locationPathName="Builds/MVP/ForestArchmage.app",
                    target=BuildTarget.StandaloneOSX,
                    options=BuildOptions.Development|BuildOptions.DetailedBuildReport
                });
                string result=$"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nBytes: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\n";
                foreach(var step in report.steps) foreach(var message in step.messages)
                    if(message.type==LogType.Error||message.type==LogType.Exception) result+=message.content+"\n";
                File.WriteAllText("Captures/MVP/mac-build.txt",result);
                Debug.Log("MVP Mac build: "+result);
            }
            finally
            {
                if(settings) { settings.BuildAddressablesWithPlayerBuild=previousAddressables;EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings); }
                GraphicsSettings.defaultRenderPipeline=previousGraphics;
                QualitySettings.renderPipeline=previousQuality;
                PlayerSettings.fullScreenMode=previousMode;PlayerSettings.productName=previousName;
            }
        }
    }
}
