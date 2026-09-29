using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace BounceFit.Mvp
{
    internal struct CombatCollisionBody
    {
        public Entity Entity;
        public float2 Position;
        public float Radius;
        public int Kind;
        public byte Returning;
        public Entity LastHit;
        public float HitCooldown;
    }

    internal struct CombatCollisionResult
    {
        public int FirstEnemy;
        public byte TouchesPlayer;
    }

    // Pure narrow phase. Resolve health, splash and pooled despawns in entity order afterwards.
    [BurstCompile]
    internal struct CombatCollisionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<CombatCollisionBody> Bodies;
        [ReadOnly] public NativeArray<CombatCollisionBody> Enemies;
        [WriteOnly] public NativeArray<CombatCollisionResult> Results;
        public float2 Player;
        public float PlayerHitRadius;
        public float PlayerContactRadius;

        public void Execute(int index)
        {
            var body = Bodies[index];
            var result = new CombatCollisionResult { FirstEnemy = -1 };
            if (body.Kind == 1 && body.Returning == 0)
            {
                for (int j = 0; j < Enemies.Length; j++)
                {
                    var enemy = Enemies[j];
                    if (body.LastHit == enemy.Entity && body.HitCooldown > 0f) continue;
                    float radius = body.Radius + enemy.Radius;
                    if (math.distancesq(body.Position, enemy.Position) > radius * radius) continue;
                    result.FirstEnemy = j;
                    break;
                }
            }
            else if (body.Kind == 2 || body.Kind == 0)
            {
                float radius = body.Radius + (body.Kind == 2 ? PlayerHitRadius : PlayerContactRadius);
                if (math.distancesq(body.Position, Player) < radius * radius) result.TouchesPlayer = 1;
            }
            Results[index] = result;
        }
    }

    internal struct CombatSpawnPlan
    {
        public float2 Position;
        public byte Shooter;
    }

    // One Burst invocation generates a complete wave so the random sequence stays deterministic.
    [BurstCompile]
    internal struct CombatWavePlanJob : IJob
    {
        public Unity.Mathematics.Random Random;
        public float Elapsed;
        public float ShooterStart;
        public float ShooterChance;
        public float Left;
        public float Right;
        public float SpawnY;
        public float RowSpacing;
        public NativeArray<CombatSpawnPlan> Plans;
        public NativeArray<Unity.Mathematics.Random> RandomAfter;

        public void Execute()
        {
            for (int i = 0; i < Plans.Length; i++)
            {
                bool shooter = Elapsed > ShooterStart && Random.NextFloat() < ShooterChance;
                Plans[i] = new CombatSpawnPlan
                {
                    Shooter = shooter ? (byte)1 : (byte)0,
                    Position = new float2(Random.NextFloat(Left + .6f, Right - .6f), SpawnY + i * RowSpacing)
                };
            }
            RandomAfter[0] = Random;
        }
    }

    internal struct CombatShooterPlan
    {
        public Entity Entity;
        public float2 Position;
        public float Cooldown;
        public float Telegraph;
        public float2 Direction;
        public byte Fire;
    }

    [BurstCompile]
    internal struct CombatShooterPlanJob : IJobParallelFor
    {
        public NativeArray<CombatShooterPlan> Plans;
        public float2 Player;
        public float DeltaTime;
        public float Cooldown;
        public float TelegraphSeconds;

        public void Execute(int index)
        {
            var plan = Plans[index];
            plan.Cooldown -= DeltaTime;
            plan.Telegraph = math.saturate((TelegraphSeconds - math.max(plan.Cooldown, 0f)) / TelegraphSeconds);
            if (plan.Cooldown <= 0f)
            {
                plan.Direction = math.normalizesafe(Player - plan.Position, new float2(0f, -1f));
                plan.Fire = 1;
                plan.Cooldown = Cooldown;
                plan.Telegraph = 0f;
            }
            Plans[index] = plan;
        }
    }
}
