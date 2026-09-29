using System.Reflection;
using Framework.Poolable.Components;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BounceFit.Mvp.Tests
{
    public sealed class MvpPoolingTests
    {
        MvpConfigSet config;

        [SetUp]
        public void SetUp() => config = MvpConfigSet.CreateTransient();

        [TearDown]
        public void TearDown()
        {
            if (config) UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void CollectedBallReturnsToPoolAndIsReused()
        {
            using var combat = new MvpCombat(31, config);
            combat.Step(.01f, float2.zero, new float2(0f, 1f), Stats());

            EntityManager entities = Entities(combat);
            Entity ball = ActiveBall(entities);
            PoolStats before = Stats(entities, PoolEntity(combat), MvpCombatPools.Id(1, 0));
            Assert.That(before.Active, Is.EqualTo(1));

            entities.SetComponentData(ball, new CombatPosition { Value = combat.PlayerPosition });
            entities.SetComponentData(ball, new CombatVelocity());
            entities.SetComponentData(ball, new CombatBaseVelocity());
            entities.SetComponentData(ball, new CombatBallFlight { Returning = 1 });
            combat.Step(.01f, float2.zero, new float2(0f, 1f), Stats());

            PoolStats returned = Stats(entities, PoolEntity(combat), MvpCombatPools.Id(1, 0));
            Assert.That(combat.ReturnedBalls, Is.EqualTo(1));
            Assert.That(returned.Active, Is.EqualTo(0));
            Assert.That(returned.Free, Is.EqualTo(before.Free + 1));
            Assert.That(returned.Total, Is.EqualTo(before.Total));

            for (int i = 0; i < 20 && !HasActiveBall(entities); i++)
                combat.Step(.05f, float2.zero, new float2(0f, 1f), Stats());

            Assert.That(ActiveBall(entities), Is.EqualTo(ball));
            PoolStats reused = Stats(entities, PoolEntity(combat), MvpCombatPools.Id(1, 0));
            Assert.That(reused.Active, Is.EqualTo(1));
            Assert.That(reused.Total, Is.EqualTo(before.Total));
        }

        [Test]
        public void ResetReusesBallWithFlightHitAndEffectStateCleared()
        {
            using var combat = new MvpCombat(32, config);
            combat.Step(.01f, float2.zero, new float2(0f, 1f), Stats());

            EntityManager entities = Entities(combat);
            Entity ball = ActiveBall(entities);
            entities.SetComponentData(ball, new CombatEffect { SlowRemaining = 8f });
            entities.SetComponentData(ball, new CombatBallHit { Last = ball, Cooldown = 5f, Pierce = 7, Splash = .5f, Slow = 3f });
            entities.SetComponentData(ball, new CombatBallFlight { Returning = 1, Age = 9f, Level = 4 });

            combat.Reset();
            PoolStats afterReset = Stats(entities, PoolEntity(combat), MvpCombatPools.Id(1, 0));
            Assert.That(afterReset.Active, Is.EqualTo(0));

            combat.Step(.01f, float2.zero, new float2(0f, 1f), Stats());
            Entity reused = ActiveBall(entities);
            Assert.That(reused, Is.EqualTo(ball));
            Assert.That(entities.GetComponentData<CombatEffect>(reused).SlowRemaining, Is.LessThanOrEqualTo(0));
            Assert.That(entities.GetComponentData<CombatBallHit>(reused).Cooldown, Is.LessThanOrEqualTo(0));
            Assert.That(entities.GetComponentData<CombatBallHit>(reused).Last, Is.EqualTo(Entity.Null));
            Assert.That(entities.GetComponentData<CombatBallFlight>(reused).Returning, Is.Zero);
            Assert.That(entities.GetComponentData<CombatBallFlight>(reused).Age, Is.EqualTo(.01f).Within(.001f));
            Assert.That(entities.GetComponentData<CombatBallFlight>(reused).Level, Is.Zero);
        }

        static CombatStats Stats() => new CombatStats
        {
            BallCount = 1,
            DamageMultiplier = 1f,
            FireRateMultiplier = 1f,
            MoveSpeedMultiplier = 1f,
            PickupRadius = 1f,
        };

        static EntityManager Entities(MvpCombat combat) => (EntityManager)Field("entities").GetValue(combat);
        static Entity PoolEntity(MvpCombat combat) => (Entity)Field("poolEntity").GetValue(combat);

        static FieldInfo Field(string name) => typeof(MvpCombat).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        static Entity ActiveBall(EntityManager entities)
        {
            using var query = entities.CreateEntityQuery(typeof(CombatBody), typeof(CombatBallFlight));
            using var all = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < all.Length; i++)
            {
                if (entities.GetComponentData<CombatBody>(all[i]).Kind == 1 &&
                    entities.GetComponentData<CombatBody>(all[i]).Variant == 0) return all[i];
            }

            Assert.Fail("Expected an active base ball.");
            return Entity.Null;
        }

        static bool HasActiveBall(EntityManager entities)
        {
            using var query = entities.CreateEntityQuery(typeof(CombatBody), typeof(CombatBallFlight));
            using var all = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < all.Length; i++)
                if (entities.GetComponentData<CombatBody>(all[i]).Kind == 1 && entities.GetComponentData<CombatBody>(all[i]).Variant == 0) return true;
            return false;
        }

        static PoolStats Stats(EntityManager entities, Entity poolEntity, PoolId id)
        {
            EntityPool pool = entities.GetComponentData<EntityPool>(poolEntity);
            Assert.That(pool.TryGetStats(id, out int free, out int active, out int total), Is.True);
            return new PoolStats { Free = free, Active = active, Total = total };
        }

        struct PoolStats
        {
            public int Free;
            public int Active;
            public int Total;
        }
    }
}
