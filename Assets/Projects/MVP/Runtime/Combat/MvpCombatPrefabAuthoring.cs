using Unity.Entities;
using UnityEngine;

namespace BounceFit.Mvp
{
    /// <summary>Component layout for the prefabs referenced by PoolContainer and baked in ForestCombat.</summary>
    public sealed class MvpCombatPrefabAuthoring : MonoBehaviour
    {
        public int kind;
        public int variant;

        class CombatBaker : Baker<MvpCombatPrefabAuthoring>
        {
            public override void Bake(MvpCombatPrefabAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<CombatPosition>(entity);
                AddComponent<CombatVelocity>(entity);
                AddComponent<CombatBaseVelocity>(entity);
                AddComponent<CombatEffect>(entity);
                AddComponent(entity, new CombatBody { Kind = authoring.kind, Variant = authoring.variant });
                AddComponent<CombatHealth>(entity);
                AddComponent<CombatLifetime>(entity);
                if (authoring.kind == 0)
                {
                    AddComponent<CombatEnemy>(entity);
                    if (authoring.variant == 1) AddComponent<CombatEnemyShot>(entity);
                }
                if (authoring.kind == 1)
                {
                    AddComponent<CombatBallHit>(entity);
                    AddComponent<CombatBallFlight>(entity);
                }
                if (authoring.kind == 2) AddComponent<CombatProjectileMotion>(entity);
            }
        }
    }
}
