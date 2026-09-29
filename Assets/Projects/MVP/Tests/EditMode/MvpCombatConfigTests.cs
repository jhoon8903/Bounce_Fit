using NUnit.Framework;
using UnityEngine;
using Unity.Mathematics;

namespace BounceFit.Mvp.Tests
{
    public sealed class MvpCombatConfigTests
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
        public void LiveBallSpeedChangePreservesOutboundDirection()
        {
            using var combat = new MvpCombat(1, config);
            var stats = DefaultStats();
            combat.Step(.01f, float2.zero, new float2(0f, 1f), stats);
            float before = FirstBall(combat).Position.y;

            config.ball.speed = 20f;
            combat.Step(.05f, float2.zero, new float2(0f, 1f), stats);
            float distance = FirstBall(combat).Position.y - before;

            Assert.That(distance, Is.EqualTo(1f).Within(.001f));
        }

        [Test]
        public void LivePlayerMoveSpeedChangesNextStep()
        {
            using var combat = new MvpCombat(2, config);
            var stats = DefaultStats();
            config.player.moveSpeed = 2f;
            combat.Step(.05f, new float2(1f, 0f), new float2(0f, 1f), stats);
            float firstStep = combat.PlayerPosition.x;

            config.player.moveSpeed = 6f;
            combat.Step(.05f, new float2(1f, 0f), new float2(0f, 1f), stats);

            Assert.That(firstStep, Is.EqualTo(.1f).Within(.001f));
            Assert.That(combat.PlayerPosition.x - firstStep, Is.EqualTo(.3f).Within(.001f));
        }

        [Test]
        public void AsymmetricWallsReflectAtTheirOwnBoundaries()
        {
            float2 position = new(-4.9f, 1f);
            float2 velocity = new(-2f, 0f);
            var flight = new CombatBallFlight();

            MvpBallFlight.Resolve(ref position, ref velocity, ref flight, .2f, float2.zero, .01f,
                13.5f, -6.4f, -4f, 8f, 14f, .15f, 45f);

            Assert.That(velocity.x, Is.GreaterThan(0f));
            Assert.That(position.x, Is.GreaterThanOrEqualTo(-3.8f));
        }

        [Test]
        public void ValidationReplacesNonFiniteValues()
        {
            config.ball.speed = float.NaN;
            config.enemy.bossHealth = float.PositiveInfinity;
            config.experience.magnetSpeed = float.NegativeInfinity;

            config.ValidateAll();

            Assert.That(config.ball.speed, Is.EqualTo(9f));
            Assert.That(config.enemy.bossHealth, Is.EqualTo(4500f));
            Assert.That(config.experience.magnetSpeed, Is.EqualTo(5f));
        }

        static CombatStats DefaultStats() => new CombatStats
        {
            BallCount = 1,
            DamageMultiplier = 1f,
            FireRateMultiplier = 1f,
            MoveSpeedMultiplier = 1f,
            PickupRadius = 1f,
        };

        static CombatVisual FirstBall(MvpCombat combat)
        {
            for (int i = 0; i < combat.Visuals.Count; i++)
            {
                if (combat.Visuals[i].Kind == 1) return combat.Visuals[i];
            }

            Assert.Fail("Expected an active ball.");
            return default;
        }
    }
}
