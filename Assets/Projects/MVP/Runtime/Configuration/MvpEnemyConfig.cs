using UnityEngine;

namespace BounceFit.Mvp
{
    [CreateAssetMenu(menuName = "Bounce Fit/MVP/Enemy Config")]
    public sealed class MvpEnemyConfig : ScriptableObject
    {
        [Min(.01f)] public float spawnIntervalStart = 1.55f;
        [Min(.01f)] public float spawnIntervalEnd = .45f;
        [Min(.01f)] public float spawnRampSeconds = 300f;
        [Min(0f)] public float countRampSeconds = 75f;
        [Min(0f)] public float shooterStartSeconds = 10f;
        [Range(0f, 1f)] public float shooterChanceStart = .12f;
        [Range(0f, 1f)] public float shooterChanceRamp = .18f;
        [Min(.01f)] public float regularHealth = 20f;
        [Min(0f)] public float regularHealthPerSecond = .035f;
        [Min(.01f)] public float shooterHealth = 30f;
        [Min(.001f)] public float regularRadius = .30f;
        [Min(.001f)] public float shooterRadius = .34f;
        [Min(0f)] public float spawnY = 13f;
        [Min(0f)] public float spawnRowSpacing = .65f;
        [Min(.01f)] public float shooterInitialCooldownMin = .9f;
        [Min(.01f)] public float shooterInitialCooldownMax = 1.7f;
        [Min(.01f)] public float shooterCooldown = 1.7f;
        [Min(.01f)] public float shooterTelegraphSeconds = .4f;
        [Min(.01f)] public float shooterBulletSpeed = 3.5f;
        [Min(.001f)] public float shooterBulletRadius = .14f;
        [Min(.01f)] public float shooterBulletLifetime = 4f;
        [Min(.01f)] public float bulletDamage = 8f;
        [Min(.01f)] public float shooterContactDamage = 12f;
        [Min(.01f)] public float bossContactDamage = 18f;
        [Min(.001f)] public float bossRadius = .9f;
        [Min(.01f)] public float bossHealth = 4500f;
        [Min(0f)] public float bossSpawnY = 11.5f;
        [Min(0f)] public float bossStopY = 8f;
        [Min(0f)] public float bossMoveSpeed = 1.2f;
        [Min(.01f)] public float bossShotInterval = .9f;
        [Min(.01f)] public float bossFanBulletSpeed = 3.7f;
        [Min(.01f)] public float bossAimBulletSpeed = 4.2f;
        [Min(.001f)] public float bossBulletRadius = .16f;
        [Min(.01f)] public float bossFanBulletLifetime = 5f;
        [Min(.01f)] public float bossAimBulletLifetime = 4f;
        [Min(1)] public int bossFanHalfCount = 4;
        [Min(1)] public int bossPatternCycle = 3;
        [Min(.001f)] public float bossFanAngleStepRadians = .2f;
        [Min(.001f)] public float bossAimAngleStepRadians = .18f;

        public void Validate()
        {
            spawnIntervalStart = Number(spawnIntervalStart, .01f, 3600f, 1.55f);
            spawnIntervalEnd = Number(spawnIntervalEnd, .01f, 3600f, .45f);
            spawnRampSeconds = Number(spawnRampSeconds, .01f, 36000f, 300f);
            countRampSeconds = Number(countRampSeconds, 0f, 36000f, 75f);
            shooterStartSeconds = Number(shooterStartSeconds, 0f, 36000f, 10f);
            shooterChanceStart = Number(shooterChanceStart, 0f, 1f, .12f);
            shooterChanceRamp = Number(shooterChanceRamp, 0f, 1f, .18f);
            regularHealth = Number(regularHealth, .01f, 1000000f, 20f);
            regularHealthPerSecond = Number(regularHealthPerSecond, 0f, 100000f, .035f);
            shooterHealth = Number(shooterHealth, .01f, 1000000f, 30f);
            regularRadius = Number(regularRadius, .001f, 100f, .30f);
            shooterRadius = Number(shooterRadius, .001f, 100f, .34f);
            spawnY = Number(spawnY, -1000f, 1000f, 13f);
            spawnRowSpacing = Number(spawnRowSpacing, 0f, 1000f, .65f);
            shooterInitialCooldownMin = Number(shooterInitialCooldownMin, .01f, 3600f, .9f);
            shooterInitialCooldownMax = Number(shooterInitialCooldownMax, shooterInitialCooldownMin, 3600f, 1.7f);
            shooterCooldown = Number(shooterCooldown, .01f, 3600f, 1.7f);
            shooterTelegraphSeconds = Number(shooterTelegraphSeconds, .01f, 3600f, .4f);
            shooterBulletSpeed = Number(shooterBulletSpeed, .01f, 1000f, 3.5f);
            shooterBulletRadius = Number(shooterBulletRadius, .001f, 100f, .14f);
            shooterBulletLifetime = Number(shooterBulletLifetime, .01f, 3600f, 4f);
            bulletDamage = Number(bulletDamage, .01f, 1000000f, 8f);
            shooterContactDamage = Number(shooterContactDamage, .01f, 1000000f, 12f);
            bossContactDamage = Number(bossContactDamage, .01f, 1000000f, 18f);
            bossRadius = Number(bossRadius, .001f, 100f, .9f);
            bossHealth = Number(bossHealth, .01f, 1000000f, 4500f);
            bossSpawnY = Number(bossSpawnY, -1000f, 1000f, 11.5f);
            bossStopY = Number(bossStopY, -1000f, 1000f, 8f);
            bossMoveSpeed = Number(bossMoveSpeed, 0f, 1000f, 1.2f);
            bossShotInterval = Number(bossShotInterval, .01f, 3600f, .9f);
            bossFanBulletSpeed = Number(bossFanBulletSpeed, .01f, 1000f, 3.7f);
            bossAimBulletSpeed = Number(bossAimBulletSpeed, .01f, 1000f, 4.2f);
            bossBulletRadius = Number(bossBulletRadius, .001f, 100f, .16f);
            bossFanBulletLifetime = Number(bossFanBulletLifetime, .01f, 3600f, 5f);
            bossAimBulletLifetime = Number(bossAimBulletLifetime, .01f, 3600f, 4f);
            bossFanHalfCount = Mathf.Clamp(bossFanHalfCount, 1, 100);
            bossPatternCycle = Mathf.Clamp(bossPatternCycle, 1, 100);
            bossFanAngleStepRadians = Number(bossFanAngleStepRadians, .001f, 3.1415927f, .2f);
            bossAimAngleStepRadians = Number(bossAimAngleStepRadians, .001f, 3.1415927f, .18f);
        }

        static float Number(float value, float min, float max, float fallback)
            => MvpConfigValidation.Number(value, min, max, fallback);

        void OnValidate() => Validate();
    }
}
