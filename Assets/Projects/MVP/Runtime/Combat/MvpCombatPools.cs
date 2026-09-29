using Framework.Poolable.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace BounceFit.Mvp
{
    public static class MvpCombatPools
    {
        public static readonly string[] Names = { "Mvp.Player", "Mvp.BaseBall", "Mvp.FireBall", "Mvp.IceBall", "Mvp.StormBall", "Mvp.FusionBall", "Mvp.Enemy", "Mvp.Shooter", "Mvp.Boss", "Mvp.EnemyBullet", "Mvp.Xp" };
        public static readonly int[] Kinds = { -1, 1, 1, 1, 1, 1, 0, 0, 0, 2, 3 };
        public static readonly int[] Variants = { 0, 0, 1, 2, 3, 4, 0, 1, 2, 0, 0 };
        public static readonly int[] WarmCounts = { 1, 16, 4, 4, 4, 4, 64, 32, 1, 128, 256 };
        public static PoolId Id(int kind, int variant)
        {
            int index = kind == -1 ? 0 : kind == 1 ? 1 + variant : kind == 0 ? 6 + variant : kind == 2 ? 9 : 10;
            return PoolId.FromName(Names[index]);
        }
        public static bool IsReady(World world)
        {
            if (world == null || !world.IsCreated) return false;
            using var ready = world.EntityManager.CreateEntityQuery(typeof(EntityPoolConfig), typeof(EntityPoolReady));
            if (ready.CalculateEntityCount() != 1) return false;
            using var query = world.EntityManager.CreateEntityQuery(typeof(EntityPool));
            if (query.CalculateEntityCount() != 1) return false;
            var pool = query.GetSingleton<EntityPool>();
            foreach (var name in Names)
                if (!pool.TryGetStats(PoolId.FromName(name), out _, out _, out _)) return false;
            return true;
        }

        // Isolated simulation tests have no streamed scene. Production always uses the baked default world.
        internal static Entity CreateIsolatedTestPool(EntityManager manager)
        {
            var pool = new EntityPool(Names.Length, Allocator.Persistent);
            for (int i = 0; i < Names.Length; i++)
            {
                var prefab = manager.CreateEntity(typeof(Prefab), typeof(LocalTransform), typeof(CombatPosition),
                    typeof(CombatVelocity), typeof(CombatBaseVelocity), typeof(CombatEffect), typeof(CombatBody),
                    typeof(CombatHealth), typeof(CombatLifetime));
                manager.SetComponentData(prefab, LocalTransform.Identity);
                if (Kinds[i] == 0)
                {
                    manager.AddComponent<CombatEnemy>(prefab);
                    if (Variants[i] == 1) manager.AddComponent<CombatEnemyShot>(prefab);
                }
                if (Kinds[i] == 1)
                {
                    manager.AddComponent<CombatBallHit>(prefab);
                    manager.AddComponent<CombatBallFlight>(prefab);
                }
                if (Kinds[i] == 2) manager.AddComponent<CombatProjectileMotion>(prefab);
                pool.Register(manager, new PoolDefinition { Id = Id(Kinds[i], Variants[i]), Prefab = prefab,
                    InitialCapacity = WarmCounts[i], MaxCapacity = 0, Name = new FixedString64Bytes(Names[i]) });
                pool.Prewarm(manager, Id(Kinds[i], Variants[i]));
            }
            return manager.CreateSingleton(pool);
        }
    }
}
