using UnityEngine;

namespace BounceFit.Mvp
{
    [CreateAssetMenu(menuName = "Bounce Fit/MVP/Experience Config")]
    public sealed class MvpExperienceConfig : ScriptableObject
    {
        [Min(1)] public int regularDropCount = 3;
        [Min(1)] public int shooterDropCount = 4;
        [Min(1)] public int smallValue = 1;
        [Min(1)] public int largeValue = 10;
        [Min(1)] public int rareValue = 25;
        [Range(0f, 1f)] public float bonusChance = .035f;
        [Range(0f, 1f)] public float rareChance = .008f;
        [Min(.001f)] public float radius = .16f;
        [Min(0f)] public float scatterRadius = .18f;
        [Min(0f)] public float magnetRadius = 1.3f;
        [Min(.01f)] public float magnetSpeed = 5f;
        [Range(0f, 1f)] public float magnetVelocityBlend = .2f;
        [Min(.001f)] public float collectDistance = .35f;

        public void Validate()
        {
            regularDropCount = Mathf.Clamp(regularDropCount, 1, 1000);
            shooterDropCount = Mathf.Clamp(shooterDropCount, 1, 1000);
            smallValue = Mathf.Clamp(smallValue, 1, 1000000);
            largeValue = Mathf.Clamp(largeValue, 1, 1000000);
            rareValue = Mathf.Clamp(rareValue, 1, 1000000);
            bonusChance = Number(bonusChance, 0f, 1f, .035f);
            rareChance = Number(rareChance, 0f, 1f, .008f);
            radius = Number(radius, .001f, 100f, .16f);
            scatterRadius = Number(scatterRadius, 0f, 100f, .18f);
            magnetRadius = Number(magnetRadius, 0f, 1000f, 1.3f);
            magnetSpeed = Number(magnetSpeed, .01f, 1000f, 5f);
            magnetVelocityBlend = Number(magnetVelocityBlend, 0f, 1f, .2f);
            collectDistance = Number(collectDistance, .001f, 100f, .35f);
        }

        static float Number(float value, float min, float max, float fallback)
            => MvpConfigValidation.Number(value, min, max, fallback);

        void OnValidate() => Validate();
    }
}
