using UnityEngine;

namespace BounceFit.Mvp
{
    [CreateAssetMenu(menuName = "Bounce Fit/MVP/Ball Config")]
    public sealed class MvpBallConfig : ScriptableObject
    {
        [Min(1)] public int startingCount = 1;
        [Tooltip("Shared outbound speed for base and active balls.")]
        [Min(.01f)] public float speed = 9f;
        [Min(.01f)] public float returnSpeed = 14f;
        [Min(.001f)] public float arrivalDistance = .15f;
        [Min(.01f)] public float maxFlightSeconds = 45f;
        [Tooltip("Shared firing interval for base and active balls.")]
        [Min(.01f)] public float fireInterval = .62f;
        [Min(.01f)] public float baseDamage = 10f;
        [Min(.001f)] public float basicRadius = .17f;
        [Min(.001f)] public float activeRadius = .19f;
        [Min(.001f)] public float fusionRadius = .25f;
        [Min(0f)] public float basicSpawnOffset = .42f;
        [Min(0f)] public float activeSpawnOffset = .4f;
        [Range(0f, 89f)] public float spreadAngle = 12f;
        [Range(0f, 89f)] public float activeRandomAngle = 9.74f;
        [Min(.01f)] public float fireDamageMultiplier = 1.5f;
        [Min(.01f)] public float iceDamageMultiplier = 1.15f;
        [Min(.01f)] public float stormDamageMultiplier = 1.3f;
        [Min(.01f)] public float fusionDamageMultiplier = 2.2f;
        [Min(0f)] public float activeDamagePerLevel = .2f;
        [Min(0f)] public float splashRadius = 1.45f;
        [Range(0f, 1f)] public float splashDamageMultiplier = .75f;
        [Min(0f)] public float slowSeconds = 2.2f;
        [Range(0f, 1f)] public float slowSpeedMultiplier = .45f;
        [Min(0)] public int stormPierce = 99;
        [Min(.001f)] public float hitCooldown = .18f;

        public void Validate()
        {
            startingCount = Mathf.Clamp(startingCount, 1, 100);
            speed = Number(speed, .01f, 1000f, 9f);
            returnSpeed = Number(returnSpeed, .01f, 1000f, 14f);
            arrivalDistance = Number(arrivalDistance, .001f, 100f, .15f);
            maxFlightSeconds = Number(maxFlightSeconds, .01f, 36000f, 45f);
            fireInterval = Number(fireInterval, .01f, 3600f, .62f);
            baseDamage = Number(baseDamage, .01f, 1000000f, 10f);
            basicRadius = Number(basicRadius, .001f, 100f, .17f);
            activeRadius = Number(activeRadius, .001f, 100f, .19f);
            fusionRadius = Number(fusionRadius, .001f, 100f, .25f);
            basicSpawnOffset = Number(basicSpawnOffset, 0f, 100f, .42f);
            activeSpawnOffset = Number(activeSpawnOffset, 0f, 100f, .4f);
            spreadAngle = Number(spreadAngle, 0f, 89f, 12f);
            activeRandomAngle = Number(activeRandomAngle, 0f, 89f, 9.74f);
            fireDamageMultiplier = Number(fireDamageMultiplier, .01f, 1000f, 1.5f);
            iceDamageMultiplier = Number(iceDamageMultiplier, .01f, 1000f, 1.15f);
            stormDamageMultiplier = Number(stormDamageMultiplier, .01f, 1000f, 1.3f);
            fusionDamageMultiplier = Number(fusionDamageMultiplier, .01f, 1000f, 2.2f);
            activeDamagePerLevel = Number(activeDamagePerLevel, 0f, 1000f, .2f);
            splashRadius = Number(splashRadius, 0f, 100f, 1.45f);
            splashDamageMultiplier = Number(splashDamageMultiplier, 0f, 1f, .75f);
            slowSeconds = Number(slowSeconds, 0f, 3600f, 2.2f);
            slowSpeedMultiplier = Number(slowSpeedMultiplier, 0f, 1f, .45f);
            stormPierce = Mathf.Clamp(stormPierce, 0, 10000);
            hitCooldown = Number(hitCooldown, .001f, 3600f, .18f);
        }

        static float Number(float value, float min, float max, float fallback)
            => MvpConfigValidation.Number(value, min, max, fallback);

        void OnValidate() => Validate();
    }
}
