using UnityEngine;

namespace BounceFit.Mvp
{
    /// <summary>Live tuning for the run progression system. Basic upgrades are counted separately from the starting ball count.</summary>
    [CreateAssetMenu(menuName = "Bounce Fit/MVP/Progression Config", fileName = "MvpProgressionConfig")]
    public sealed class MvpProgressionConfig : ScriptableObject
    {
        [Min(1)] public int maxActiveSlots = 4;
        [Min(0)] public int maxPassiveSlots = 3;
        [Min(1)] public int maxSkillLevel = 5;
        [Tooltip("Number of ball-card upgrades allowed; this does not include ball.startingCount.")]
        [Min(0)] public int maxBasicUpgrades = 5;
        [Range(1, 3)] public int choiceCount = 3;
        [Min(1)] public int initialXp = 8;
        [Min(0)] public int xpGrowth = 5;
        [Min(1)] public int fusionRequiredLevel = 2;

        public float passiveDamageBonus = .15f;
        public float passiveFireRateBonus = .12f;
        public float passivePickupRadiusBonus = .2f;
        public float passiveMoveSpeedBonus = .1f;
        public float masteryDamageBonus = .05f;
        public float precisionFireRateBonus = .05f;
        public float rhythmMoveSpeedBonus = .05f;

        public void Validate()
        {
            maxActiveSlots = Mathf.Clamp(maxActiveSlots, 1, 32);
            maxPassiveSlots = Mathf.Clamp(maxPassiveSlots, 0, 32);
            maxSkillLevel = Mathf.Clamp(maxSkillLevel, 1, 100);
            maxBasicUpgrades = Mathf.Clamp(maxBasicUpgrades, 0, 1000);
            choiceCount = Mathf.Clamp(choiceCount, 1, 3);
            initialXp = Mathf.Clamp(initialXp, 1, 1000000);
            xpGrowth = Mathf.Clamp(xpGrowth, 0, 1000000);
            fusionRequiredLevel = Mathf.Clamp(fusionRequiredLevel, 1, maxSkillLevel);
            passiveDamageBonus = Finite(passiveDamageBonus, .15f);
            passiveFireRateBonus = Finite(passiveFireRateBonus, .12f);
            passivePickupRadiusBonus = Finite(passivePickupRadiusBonus, .2f);
            passiveMoveSpeedBonus = Finite(passiveMoveSpeedBonus, .1f);
            masteryDamageBonus = Finite(masteryDamageBonus, .05f);
            precisionFireRateBonus = Finite(precisionFireRateBonus, .05f);
            rhythmMoveSpeedBonus = Finite(rhythmMoveSpeedBonus, .05f);
        }

        void OnValidate() { Validate(); }

        static float Finite(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, 0f, 1000f);
        }
    }
}
