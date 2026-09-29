using NUnit.Framework;
using UnityEngine;

namespace BounceFit.Mvp.Tests
{
    public sealed class MvpProgressionConfigTests
    {
        MvpConfigSet config;

        [SetUp]
        public void SetUp()
        {
            config = MvpConfigSet.CreateTransient();
        }

        [TearDown]
        public void TearDown()
        {
            if (config)
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void StartingCountLiveEditPreservesEarnedBallUpgrades()
        {
            var progression = new MvpProgression(config: config);
            Assert.That(progression.TryApplyCard("ball"), Is.True);
            Assert.That(progression.TryApplyCard("ball"), Is.True);
            config.ball.startingCount = 5;
            progression.RefreshConfiguration();
            Assert.That(progression.Stats.BallCount, Is.EqualTo(7));
        }

        [Test]
        public void PassiveBonusLiveEditRebuildsExistingPassiveStats()
        {
            var progression = new MvpProgression(config: config);
            progression.TryApplyCard("power");
            config.progression.passiveDamageBonus = .4f;
            progression.RefreshConfiguration();
            Assert.That(progression.Stats.DamageMultiplier, Is.EqualTo(1.4f).Within(.0001f));
        }

        [Test]
        public void LoweredXpThresholdProcessesRetainedXpAndInvalidChoiceIsRebuilt()
        {
            var progression = new MvpProgression(seed: 4, config: config);
            progression.AddXp(9);
            Assert.That(progression.Choices.Count, Is.EqualTo(3));
            config.progression.maxBasicUpgrades = 0;
            config.progression.maxActiveSlots = 1;
            config.progression.maxPassiveSlots = 0;
            config.progression.choiceCount = 1;
            config.progression.initialXp = 1;
            config.progression.xpGrowth = 0;
            progression.RefreshConfiguration();
            Assert.That(progression.Level, Is.EqualTo(3));
            Assert.That(progression.Choices.Count, Is.EqualTo(1));
            Assert.That(progression.Choose(0), Is.True);
        }

        [Test]
        public void ValidateClampsInvalidProgressionValues()
        {
            config.progression.choiceCount = 99;
            config.progression.initialXp = 0;
            config.progression.xpGrowth = -1;
            config.progression.passiveDamageBonus = float.NaN;
            config.progression.Validate();
            Assert.That(config.progression.choiceCount, Is.EqualTo(3));
            Assert.That(config.progression.initialXp, Is.EqualTo(1));
            Assert.That(config.progression.xpGrowth, Is.Zero);
            Assert.That(float.IsNaN(config.progression.passiveDamageBonus), Is.False);

            config.progression.passiveDamageBonus = -1f;
            config.progression.Validate();
            Assert.That(config.progression.passiveDamageBonus, Is.Zero);
        }
    }
}
