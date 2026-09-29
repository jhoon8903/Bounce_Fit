using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BounceFit.Mvp.Tests
{
    public sealed class MvpProgressionTests
    {
        private MvpConfigSet config;
        [SetUp] public void SetUp() => config = MvpConfigSet.CreateTransient();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(config);

        [Test]
        public void StartsWithOneBasicActiveAndDefaultCombatStats()
        {
            var progression = new MvpProgression(config: config);

            Assert.That(progression.Level, Is.EqualTo(1));
            Assert.That(progression.NextXp, Is.EqualTo(8));
            Assert.That(progression.ActiveCount, Is.EqualTo(1));
            Assert.That(progression.Stats.BallCount, Is.EqualTo(1));
            Assert.That(progression.ActiveLabels, Does.Contain("바운스 볼"));
        }

        [Test]
        public void XpOverflowCreatesEachPendingChoiceAndKeepsRemainder()
        {
            var progression = new MvpProgression(seed: 10, config: config);

            progression.AddXp(22); // 8 + 13, then one XP remains toward 18.

            Assert.That(progression.Level, Is.EqualTo(3));
            Assert.That(progression.Xp, Is.EqualTo(1));
            Assert.That(progression.NextXp, Is.EqualTo(18));
            Assert.That(progression.PendingChoices, Is.EqualTo(2));
            Assert.That(progression.Choices.Count, Is.EqualTo(3));
            Assert.That(progression.Choose(0), Is.True);
            Assert.That(progression.PendingChoices, Is.EqualTo(1));
            Assert.That(progression.Choices.Count, Is.EqualTo(3));
        }

        [Test]
        public void ChoicePoolsHaveThreeUniqueCardsAcrossSeeds()
        {
            for (var seed = 1; seed <= 40; seed++)
            {
                var progression = new MvpProgression(seed, config: config);
                progression.AddXp(8);
                var ids = new HashSet<string>();
                foreach (var card in progression.Choices)
                {
                    ids.Add(card.Id);
                }

                Assert.That(progression.Choices.Count, Is.EqualTo(3), "seed " + seed);
                Assert.That(ids.Count, Is.EqualTo(3), "seed " + seed);
            }
        }

        [Test]
        public void FallbackCardsDoNotDiluteAHealthyUpgradePool()
        {
            var progression = new MvpProgression(config: config);

            Assert.That(ContainsId(progression.EligibleCards(), "mastery"), Is.False);
            Assert.That(ContainsId(progression.EligibleCards(), "precision"), Is.False);
            Assert.That(ContainsId(progression.EligibleCards(), "rhythm"), Is.False);
        }

        [Test]
        public void ExistingActiveCanLevelAfterActiveSlotsFill()
        {
            var progression = new MvpProgression(config: config);
            Assert.That(progression.TryApplyCard("fire"), Is.True);
            Assert.That(progression.TryApplyCard("ice"), Is.True);
            Assert.That(progression.TryApplyCard("storm"), Is.True);

            Assert.That(progression.ActiveCount, Is.EqualTo(4));
            Assert.That(progression.TryApplyCard("fire"), Is.True);
            Assert.That(progression.Stats.FireLevel, Is.EqualTo(2));
            Assert.That(progression.ActiveCount, Is.EqualTo(4));
        }

        [Test]
        public void BasicBallUpgradesDoNotConsumeAnAdditionalActiveSlot()
        {
            var progression = new MvpProgression(config: config);
            progression.TryApplyCard("fire");
            progression.TryApplyCard("ice");
            progression.TryApplyCard("storm");

            Assert.That(progression.TryApplyCard("ball"), Is.True);
            Assert.That(progression.ActiveCount, Is.EqualTo(4));
            Assert.That(progression.Stats.BallCount, Is.EqualTo(2));
        }

        [Test]
        public void PassiveSlotsCapAtThreeTypesWhileOwnedPassiveCanLevel()
        {
            var progression = new MvpProgression(config: config);
            Assert.That(progression.TryApplyCard("power"), Is.True);
            Assert.That(progression.TryApplyCard("haste"), Is.True);
            Assert.That(progression.TryApplyCard("magnet"), Is.True);

            Assert.That(progression.PassiveCount, Is.EqualTo(3));
            Assert.That(progression.TryApplyCard("boots"), Is.False);
            Assert.That(progression.TryApplyCard("power"), Is.True);
            Assert.That(progression.Stats.DamageMultiplier, Is.EqualTo(1.3f).Within(.0001f));
        }

        [Test]
        public void FusionConsumesTwoSkillsAndBlocksTheirReacquisition()
        {
            var progression = new MvpProgression(config: config);
            Assert.That(ContainsId(progression.EligibleCards(), "fusion"), Is.False);
            progression.TryApplyCard("fire");
            progression.TryApplyCard("fire");
            progression.TryApplyCard("ice");
            progression.TryApplyCard("ice");

            Assert.That(progression.ActiveCount, Is.EqualTo(3));
            Assert.That(ContainsId(progression.EligibleCards(), "fusion"), Is.True);
            Assert.That(progression.TryApplyCard("fusion"), Is.True);

            Assert.That(progression.ActiveCount, Is.EqualTo(2));
            Assert.That(progression.Stats.FireLevel, Is.Zero);
            Assert.That(progression.Stats.IceLevel, Is.Zero);
            Assert.That(progression.Stats.FusionLevel, Is.EqualTo(1));
            Assert.That(progression.TryApplyCard("fire"), Is.False);
            Assert.That(progression.TryApplyCard("ice"), Is.False);
            Assert.That(progression.ActiveLabels, Does.Contain("빙염 폭발"));
        }

        [Test]
        public void ClampAimCapsDirectionAtPlusOrMinusEightyDegrees()
        {
            var upper = MvpGame.ClampAim(new Vector2(100f, -100f));
            var lower = MvpGame.ClampAim(new Vector2(-100f, -100f));
            var sine = Mathf.Sin(80f * Mathf.Deg2Rad);
            var cosine = Mathf.Cos(80f * Mathf.Deg2Rad);

            Assert.That(upper.x, Is.EqualTo(sine).Within(.0001f));
            Assert.That(upper.y, Is.EqualTo(cosine).Within(.0001f));
            Assert.That(lower.x, Is.EqualTo(-sine).Within(.0001f));
            Assert.That(lower.y, Is.EqualTo(cosine).Within(.0001f));
        }

        private static bool ContainsId(IReadOnlyList<UpgradeCard> cards, string id)
        {
            foreach (var card in cards)
            {
                if (card.Id == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
