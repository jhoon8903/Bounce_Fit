using UnityEngine;
namespace BounceFit.Mvp
{
    [CreateAssetMenu(menuName="Bounce Fit/Config/Config Set",fileName="MvpConfigSet")]
    public sealed class MvpConfigSet : ScriptableObject
    {
        public MvpBallConfig ball;
        public MvpPlayerConfig player;
        public MvpStageConfig stage;
        public MvpEnemyConfig enemy;
        public MvpExperienceConfig experience;
        public MvpProgressionConfig progression;
        public MvpInputConfig input;
        public MvpPresentationConfig presentation;
        static MvpConfigSet cachedDefault;
        readonly System.Collections.Generic.List<ScriptableObject> ownedFallbacks=new();
        public static MvpConfigSet Default
        {
            get
            {
                if(!cachedDefault) cachedDefault=Resources.Load<MvpConfigSet>("MVP/DefaultConfig");
                if(!cachedDefault) cachedDefault=CreateTransient();
                return cachedDefault;
            }
        }
        public static MvpConfigSet CreateTransient()
        {
            var value=CreateInstance<MvpConfigSet>();value.hideFlags=HideFlags.HideAndDontSave;
            value.ValidateAll();return value;
        }
        T Missing<T>() where T:ScriptableObject
        {
            var value=CreateInstance<T>();value.hideFlags=HideFlags.HideAndDontSave;ownedFallbacks.Add(value);return value;
        }
        public void ValidateAll()
        {
            if(!ball)ball=Missing<MvpBallConfig>();if(!player)player=Missing<MvpPlayerConfig>();if(!stage)stage=Missing<MvpStageConfig>();
            if(!enemy)enemy=Missing<MvpEnemyConfig>();if(!experience)experience=Missing<MvpExperienceConfig>();if(!progression)progression=Missing<MvpProgressionConfig>();
            if(!input)input=Missing<MvpInputConfig>();if(!presentation)presentation=Missing<MvpPresentationConfig>();
            ball.Validate();player.Validate();stage.Validate();enemy.Validate();experience.Validate();progression.Validate();input.Validate();presentation.Validate();
        }
        void OnDestroy()
        {
            foreach(var value in ownedFallbacks) if(value) { if(Application.isPlaying)Destroy(value);else DestroyImmediate(value); }
            ownedFallbacks.Clear();
        }
    }
}
