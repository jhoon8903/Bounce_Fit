using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BounceFit.Mvp.Tests
{
    public sealed class MvpBurstBatchTests
    {
        [Test]
        public void FortyShootersGenerateBulletsInOneParallelBatch()
        {
            var config = MvpConfigSet.CreateTransient();
            try
            {
                config.enemy.shooterStartSeconds = 0f;
                using var combat = new MvpCombat(91, config);
                var create = typeof(MvpCombat).GetMethod("CreateBody", BindingFlags.Instance | BindingFlags.NonPublic);
                var manager = (EntityManager)typeof(MvpCombat).GetField("entities", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
                for (int i = 0; i < 40; i++)
                {
                    var enemy = (Entity)create.Invoke(combat, new object[]
                    {
                        new float2(6f, 8f + i * .01f), float2.zero, .34f, 0, 1, 1000f, 1000f, -1f
                    });
                    Assert.That(enemy, Is.Not.EqualTo(Entity.Null));
                    manager.SetComponentData(enemy, new CombatEnemyShot { Cooldown = 0f });
                }
                combat.Step(.05f, float2.zero, new float2(0f, 1f), new CombatStats
                {
                    BallCount = 1, DamageMultiplier = 1f, FireRateMultiplier = 1f,
                    MoveSpeedMultiplier = 1f, PickupRadius = 1f
                });
                int bullets = 0;
                foreach (var visual in combat.Visuals) if (visual.Kind == 2) bullets++;
                Assert.That(bullets, Is.EqualTo(40));
            }
            finally { Object.DestroyImmediate(config); }
        }
    }
}
