using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace BounceFit.Mvp.Tests
{
    public sealed class MvpSharedBallTests
    {
        MvpConfigSet config;

        [SetUp]
        public void SetUp() => config = MvpConfigSet.CreateTransient();

        [TearDown]
        public void TearDown()
        {
            if (config) Object.DestroyImmediate(config);
        }

        [Test]
        public void ConfigurationExposesOneSharedSpeedAndFireInterval()
        {
            var type = typeof(MvpBallConfig);

            Assert.That(type.GetField("speed"), Is.Not.Null);
            Assert.That(type.GetField("fireInterval"), Is.Not.Null);
            Assert.That(type.GetField("activeBaseSpeed"), Is.Null);
            Assert.That(type.GetField("activeSpeedPerLevel"), Is.Null);
            Assert.That(type.GetField("activeInterval"), Is.Null);
        }

        [Test]
        public void LiveSharedSpeedChangeMovesBaseAndActiveBallsAtTheSameSpeed()
        {
            using var combat = new MvpCombat(7, config);
            var stats = new CombatStats
            {
                BallCount = 1,
                DamageMultiplier = 1f,
                FireRateMultiplier = 1f,
                MoveSpeedMultiplier = 1f,
                PickupRadius = 1f,
                FireLevel = 1,
            };

            combat.Step(.01f, float2.zero, new float2(0f, 1f), stats);
            CombatVisual baseBefore = Ball(combat, 0);
            CombatVisual activeBefore = Ball(combat, 1);

            config.ball.speed = 20f;
            combat.Step(.05f, float2.zero, new float2(0f, 1f), stats);

            CombatVisual baseAfter = Ball(combat, 0);
            CombatVisual activeAfter = Ball(combat, 1);
            Assert.That(math.distance(baseAfter.Position, baseBefore.Position), Is.EqualTo(1f).Within(.001f));
            Assert.That(math.distance(activeAfter.Position, activeBefore.Position), Is.EqualTo(1f).Within(.001f));
        }

        static CombatVisual Ball(MvpCombat combat, int variant)
        {
            for (int i = 0; i < combat.Visuals.Count; i++)
            {
                CombatVisual visual = combat.Visuals[i];
                if (visual.Kind == 1 && visual.Variant == variant) return visual;
            }

            Assert.Fail($"Expected ball variant {variant}.");
            return default;
        }
    }
}
