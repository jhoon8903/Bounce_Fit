using Framework.Scene;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Framework.BootStrap
{
    /// <summary>
    /// BootScene의 Composition Root.
    /// Scene 전환에 필요한 구현체를 이곳에서만 연결하고, 실행 컴포넌트는 인터페이스만 의존한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BootLifetimeScope : LifetimeScope
    {
        [Header("Scene Services")]
        [SerializeField] private SceneCatalog sceneCatalog;
        [SerializeField] private SceneEntryResolver sceneEntryResolver;
        [SerializeField] private TransitionView transitionView;
        [SerializeField] private LoadingView loadingView;

        [Header("Boot Components")]
        [SerializeField] private Bootstrapper bootstrapper;
        [SerializeField] private SceneSwitcher sceneSwitcher;

        protected override void Configure(IContainerBuilder builder)
        {
            if (sceneCatalog == null) throw new System.InvalidOperationException("BootLifetimeScope에 SceneCatalog가 없습니다.");
            if (sceneEntryResolver == null) throw new System.InvalidOperationException("BootLifetimeScope에 SceneEntryResolver가 없습니다.");
            if (bootstrapper == null) throw new System.InvalidOperationException("BootLifetimeScope에 Bootstrapper가 없습니다.");
            if (sceneSwitcher == null) throw new System.InvalidOperationException("BootLifetimeScope에 SceneSwitcher가 없습니다.");

            builder.RegisterInstance(sceneCatalog).As<ISceneCatalog>();
            builder.RegisterInstance(sceneEntryResolver);

            if (transitionView != null)
                builder.RegisterComponent(transitionView).As<ITransitionView>();
            else
                builder.Register<NullTransitionView>(Lifetime.Singleton).As<ITransitionView>();

            if (loadingView != null)
                builder.RegisterComponent(loadingView).As<ILoadingView>();
            else
                builder.Register<NullLoadingView>(Lifetime.Singleton).As<ILoadingView>();

            builder.RegisterComponent(sceneSwitcher).As<ISceneSwitcher>();
            builder.RegisterComponent(bootstrapper);
        }
    }
}
