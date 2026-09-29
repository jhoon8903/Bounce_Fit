using NUnit.Framework;
using Unity.Mathematics;

namespace BounceFit.Mvp.Tests
{
    public sealed class MvpCombatTests
    {
        private MvpConfigSet config;
        [SetUp] public void SetUp() => config = MvpConfigSet.CreateTransient();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(config);

        private const int Enemy = 0;
        private const int Ball = 1;
        private const int EnemyBullet = 2;
        private const int Xp = 3;
        private const int Boss = 2;

        [Test]
        public void ZeroStepDoesNotAdvanceTheSimulation()
        {
            using (var combat = new MvpCombat(11, config: config))
            {
                combat.Step(0f, float2.zero, new float2(0f, 1f), DefaultStats());

                Assert.That(combat.Elapsed, Is.Zero);
                Assert.That(combat.ScrollDistance, Is.Zero);
                Assert.That(combat.Visuals, Is.Empty);
            }
        }

        [Test]
        public void MovingPlayerRemainsInsideArenaBounds()
        {
            using (var combat = new MvpCombat(12, config: config))
            {
                for (var i = 0; i < 80; i++)
                {
                    combat.Step(.05f, new float2(1f, 1f), new float2(0f, 1f), DefaultStats());
                }

                Assert.That(combat.PlayerPosition.x, Is.InRange(-6.4f, 6.4f));
                Assert.That(combat.PlayerPosition.y, Is.InRange(-5f, 6f));
            }
        }

        [Test]
        public void InitialBallFiresForward()
        {
            using (var combat = new MvpCombat(13, config: config))
            {
                combat.Step(.01f, float2.zero, new float2(0f, 1f), DefaultStats());
                var ball = FindFirst(combat, Ball);

                Assert.That(ball.Kind, Is.EqualTo(Ball));
                Assert.That(ball.Position.y, Is.GreaterThan(combat.PlayerPosition.y));
                Assert.That(math.abs(ball.Position.x - combat.PlayerPosition.x), Is.LessThan(.001f));
            }
        }

        [Test]
        public void ShooterEnemiesProduceBulletsAfterOpeningGracePeriod()
        {
            using (var combat = new MvpCombat(14, config: config))
            {
                SpawnTestEnemy(combat,new float2(3f,8f),1,10000f);
                var sawEnemyBullet = false;
                for (var i = 0; i < 620 && !sawEnemyBullet; i++)
                {
                    combat.DebugSetHealth(100f);
                    combat.Step(.05f, float2.zero, new float2(1f, 1f), DefaultStats());
                    sawEnemyBullet = HasKind(combat, EnemyBullet);
                }

                Assert.That(combat.Elapsed, Is.GreaterThanOrEqualTo(10f));
                Assert.That(sawEnemyBullet, Is.True);
            }
        }

        [Test]
        public void OrdinaryKillDropsAtLeastThreeXpOrbs()
        {
            using (var combat = new MvpCombat(15, config: config))
            {
                SpawnTestEnemy(combat,new float2(0f,2f),0,10f);
                var stats = DefaultStats();
                stats.DamageMultiplier = 10f;
                var sawKillWithThreeDrops = false;
                for (var i = 0; i < 220 && !sawKillWithThreeDrops; i++)
                {
                    combat.DebugSetHealth(100f);
                    combat.Step(.05f, float2.zero, AimAtClosestEnemy(combat), stats);
                    if (combat.Kills > 0 && CountKind(combat, Xp) >= 3)
                    {
                        sawKillWithThreeDrops = true;
                    }
                }

                Assert.That(sawKillWithThreeDrops, Is.True);
            }
        }

        [Test]
        public void PickupRadiusDoesNotGrantXpWithoutXpOrbs()
        {
            using (var combat = new MvpCombat(16, config: config))
            {
                var stats = DefaultStats();
                stats.PickupRadius = 100f;
                for (var i = 0; i < 10; i++)
                {
                    combat.Step(.05f, float2.zero, new float2(1f, 1f), stats);
                }

                Assert.That(combat.CollectedXp, Is.Zero);
            }
        }

        [Test]
        public void BossTimerSpawnsBossWithoutWinningAndBossPersists()
        {
            using (var combat = new MvpCombat(17, config: config))
            {
                combat.BossAtSeconds = .1f;
                for (var i = 0; i < 2200; i++)
                {
                    combat.DebugSetHealth(100f);
                    combat.Step(.05f, float2.zero, new float2(1f, 0f), DefaultStats());
                }

                Assert.That(combat.Elapsed, Is.GreaterThan(100f));
                Assert.That(combat.BossActive, Is.True);
                Assert.That(combat.Won, Is.False);
                Assert.That(HasEnemyVariant(combat, Boss), Is.True);
            }
        }

        [Test]
        public void HighDamageBallsCanDefeatBossAndSetWon()
        {
            using (var combat = new MvpCombat(18, config: config))
            {
                combat.BossAtSeconds = .1f;
                var stats = DefaultStats();
                stats.DamageMultiplier = 500f;
                for (var i = 0; i < 220 && !combat.Won; i++)
                {
                    combat.DebugSetHealth(100f);
                    combat.Step(.05f, float2.zero, new float2(0f, 1f), stats);
                }

                Assert.That(combat.Won, Is.True);
                Assert.That(combat.BossActive, Is.False);
            }
        }

        [Test]
        public void DefeatAndResetClearTerminalFlagsAndRestoreHealth()
        {
            using (var combat = new MvpCombat(19, config: config))
            {
                combat.DebugSetHealth(0f);
                combat.Step(.05f, float2.zero, new float2(0f, 1f), DefaultStats());
                Assert.That(combat.Lost, Is.True);

                combat.Reset();

                Assert.That(combat.Lost, Is.False);
                Assert.That(combat.Won, Is.False);
                Assert.That(combat.Health, Is.EqualTo(combat.MaxHealth));
                Assert.That(combat.Elapsed, Is.Zero);
            }
        }

        // Arrange a known target so these rule tests do not depend on random wave
        // placement or the time at which a limited-inventory ball becomes available.
        private static void SpawnTestEnemy(MvpCombat combat,float2 position,int variant,float hp)
        {
            typeof(MvpCombat).GetMethod("CreateBody",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(combat,new object[]{position,float2.zero,.3f,Enemy,variant,hp,hp,-1f});
        }

        private static CombatStats DefaultStats()
        {
            return new CombatStats
            {
                BallCount = 1,
                DamageMultiplier = 1f,
                FireRateMultiplier = 1f,
                MoveSpeedMultiplier = 1f,
                PickupRadius = 1f,
            };
        }

        private static float2 AimAtClosestEnemy(MvpCombat combat)
        {
            for (var i = 0; i < combat.Visuals.Count; i++)
            {
                var visual = combat.Visuals[i];
                if (visual.Kind == Enemy && visual.Variant != Boss)
                {
                    return visual.Position - combat.PlayerPosition;
                }
            }

            return new float2(0f, 1f);
        }

        private static CombatVisual FindFirst(MvpCombat combat, int kind)
        {
            for (var i = 0; i < combat.Visuals.Count; i++)
            {
                if (combat.Visuals[i].Kind == kind)
                {
                    return combat.Visuals[i];
                }
            }

            Assert.Fail("Expected visual kind " + kind + ".");
            return default(CombatVisual);
        }

        private static bool HasKind(MvpCombat combat, int kind) => CountKind(combat, kind) > 0;

        private static int CountKind(MvpCombat combat, int kind)
        {
            var count = 0;
            for (var i = 0; i < combat.Visuals.Count; i++)
            {
                if (combat.Visuals[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasEnemyVariant(MvpCombat combat, int variant)
        {
            for (var i = 0; i < combat.Visuals.Count; i++)
            {
                var visual = combat.Visuals[i];
                if (visual.Kind == Enemy && visual.Variant == variant)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
