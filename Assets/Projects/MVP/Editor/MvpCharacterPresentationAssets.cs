using UnityEditor;
using UnityEngine;

namespace BounceFit.Mvp.Editor
{
    public static class MvpCharacterPresentationAssets
    {
        const string Path = "Assets/Projects/MVP/Resources/MVP/CharacterPresentation.asset";
        [MenuItem("Bounce Fit/MVP/Configuration/Create Character Presentation Defaults")]
        public static void Create()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MvpCharacterPresentationConfig>(Path);
            if (existing) { Selection.activeObject = existing; return; }
            var asset = ScriptableObject.CreateInstance<MvpCharacterPresentationConfig>();
            AssetDatabase.CreateAsset(asset, Path);
            AssetDatabase.SaveAssetIfDirty(asset);
            Selection.activeObject = asset;
        }
    }
}
