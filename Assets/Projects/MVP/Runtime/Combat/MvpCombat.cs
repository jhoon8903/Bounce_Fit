using System;
using Framework.Poolable;
using Framework.Poolable.Components;
using Unity.Transforms;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;

namespace BounceFit.Mvp
{
    /// <summary>Input values supplied by the meta-game. A zero-valued multiplier is treated as one.</summary>
    public struct CombatStats
    {
        public int BallCount;
        public float DamageMultiplier;
        public float FireRateMultiplier;
        public float MoveSpeedMultiplier;
        public float PickupRadius;
        public int FireLevel;
        public int IceLevel;
        public int StormLevel;
        public int FusionLevel;
    }

    /// <summary>A renderer-facing snapshot. Kind: enemy/player ball/enemy bullet/xp = 0/1/2/3.</summary>
    public struct CombatVisual
    {
        public int Id;
        public float2 Position;
        public float Radius;
        public int Kind;
        public int Variant;
        public float Health01;
        public bool Returning;
        public float Telegraph01;
    }

    internal struct CombatPosition : IComponentData { public float2 Value; }
    internal struct CombatVelocity : IComponentData { public float2 Value; }
    internal struct CombatBody : IComponentData { public float Radius; public int Kind; public int Variant; }
    internal struct CombatHealth : IComponentData { public float Value; public float Maximum; }
    internal struct CombatLifetime : IComponentData { public float Value; }
    internal struct CombatEnemy : IComponentData { }
    internal struct CombatEnemyShot : IComponentData { public float Cooldown; public float Telegraph; }
    internal struct CombatEffect : IComponentData { public float SlowRemaining; }
    internal struct CombatBaseVelocity : IComponentData { public float2 Value; }
    internal struct CombatProjectileMotion : IComponentData { public float2 Direction; public byte Type; }
    internal struct CombatBallHit : IComponentData { public Entity Last; public float Cooldown; public int Pierce; public float Splash; public float Slow; }

    [BurstCompile]
    internal partial struct CombatIntegrateJob : IJobEntity
    {
        public float DeltaTime;
        public void Execute(ref CombatPosition position, ref CombatVelocity velocity, in CombatBaseVelocity baseVelocity, ref CombatEffect effect, ref LocalTransform transform)
        {
            effect.SlowRemaining -= DeltaTime;
            if (effect.SlowRemaining <= 0f) velocity.Value = baseVelocity.Value;
            position.Value += velocity.Value * DeltaTime;
            transform.Position = new float3(position.Value.x, 0, position.Value.y);
        }
    }

    /// <summary>Stepped explicitly by MvpCombat; streamed entities use the default World.</summary>
    [DisableAutoCreation]
    [BurstCompile]
    internal partial struct CombatMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new CombatIntegrateJob { DeltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Deterministic ECS simulation. Gameplay uses the baked default World; tests can own an isolated World.
    /// The caller owns presentation and should read Visuals after Step.
    /// </summary>
    public sealed class MvpCombat : IDisposable
    {
        const int Enemy = 0, Ball = 1, EnemyBullet = 2, Xp = 3;
        const int Regular = 0, Shooter = 1, Boss = 2;

        readonly World world;
        readonly bool ownsWorld;
        readonly Entity poolEntity;
        readonly Entity session;
        bool disposed;
        public bool UsesBakedPool => !ownsWorld;
        readonly EntityManager entities;
        readonly EntityQuery bodyQuery;
        readonly SystemHandle movementSystem;
        readonly MvpConfigSet config;
        NativeArray<CombatCollisionBody> collisionEnemyBuffer;
        NativeArray<CombatCollisionBody> collisionBodyBuffer;
        NativeArray<CombatCollisionResult> collisionResultBuffer;
        NativeArray<CombatSpawnPlan> wavePlanBuffer;
        NativeArray<Unity.Mathematics.Random> waveRandomBuffer;
        NativeArray<CombatShooterPlan> shooterPlanBuffer;
        Unity.Mathematics.Random random;
        Entity player;
        float elapsed, health, maxHealth, scrollDistance;
        float fireTimer, spawnTimer, specialTimer, bossShotTimer, invulnerableUntil;
        float2 lastAim = new float2(0f, 1f);
        int kills, collectedXp;
        bool bossSpawned, won, lost;

        float? bossAtSecondsOverride;
        /// <summary>Time at which the boss enters. Assigning creates a run-local override.</summary>
        public float BossAtSeconds { get => bossAtSecondsOverride ?? config.stage.bossAtSeconds; set => bossAtSecondsOverride = math.max(0f, value); }
        public MvpConfigSet Config => config;
        public float2 PlayerPosition => IsAlive(player) ? entities.GetComponentData<CombatPosition>(player).Value : float2.zero;
        /// <summary>Optional presentation-projected muzzle offset; standalone simulations keep legacy spawn offsets.</summary>
        public float2 PlayerLaunchOffset { get; set; }
        public bool UsePlayerLaunchOffset { get; set; }
        public int ShotsFired { get; private set; }
        public float Health => health;
        public float MaxHealth => maxHealth;
        public float Elapsed => elapsed;
        public int Kills => kills;
        public int ReturnedBalls { get; private set; }
        public int TopWallBounces { get; private set; }
        public int CollectedXp => collectedXp;
        public bool Won => won;
        public bool Lost => lost;
        public bool BossActive => bossSpawned && !won;
        public float BossHealth01 { get; private set; }
        public float ScrollDistance => scrollDistance;
        public List<CombatVisual> Visuals { get; } = new List<CombatVisual>(128);
        public int EntityCount => world.IsCreated ? bodyQuery.CalculateEntityCount() : 0;

        public MvpCombat(uint seed = 1, MvpConfigSet config = null, World simulationWorld = null)
        {
            this.config = config ?? MvpConfigSet.Default;
            this.config.ValidateAll();
            ownsWorld = simulationWorld == null;
            world = simulationWorld ?? new World("BounceFit Isolated Test Combat");
            entities = world.EntityManager;
            if (ownsWorld) poolEntity = MvpCombatPools.CreateIsolatedTestPool(entities);
            else
            {
                if (!MvpCombatPools.IsReady(world)) throw new InvalidOperationException("Baked MVP pools are not loaded yet.");
                using var poolQuery = entities.CreateEntityQuery(typeof(EntityPool));
                poolEntity = poolQuery.GetSingletonEntity();
            }
            session = entities.CreateEntity();
            bodyQuery = entities.CreateEntityQuery(typeof(CombatBody));
            movementSystem = world.GetOrCreateSystem<CombatMovementSystem>();
            random = new Unity.Mathematics.Random(seed == 0 ? 1u : seed);
            Reset();
        }

        public void Reset()
        {
            ReturnSession();
            elapsed = scrollDistance = fireTimer = spawnTimer = specialTimer = bossShotTimer = invulnerableUntil = 0f;
            maxHealth = config.player.maxHealth;
            health = maxHealth;
            kills = collectedXp = 0;
            ReturnedBalls = TopWallBounces = 0;
            bossSpawned = won = lost = false;
            BossHealth01 = 0f;
            lastAim = new float2(0f, 1f);
            player = CreateBody(config.player.startPosition, float2.zero, config.player.contactRadius, -1, 0, health, maxHealth, -1f);
            Snapshot();
        }

        public void Dispose()
        {
            if (disposed || !world.IsCreated) return;
            disposed = true;
            ReturnSession();
            entities.DestroyEntity(session);
            bodyQuery.Dispose();
            DisposeBuffer(ref collisionEnemyBuffer);
            DisposeBuffer(ref collisionBodyBuffer);
            DisposeBuffer(ref collisionResultBuffer);
            DisposeBuffer(ref wavePlanBuffer);
            DisposeBuffer(ref waveRandomBuffer);
            DisposeBuffer(ref shooterPlanBuffer);
            if (ownsWorld)
            {
                var pool = entities.GetComponentData<EntityPool>(poolEntity);
                pool.Dispose();
                world.Dispose();
            }
        }

        static NativeArray<T> Slice<T>(ref NativeArray<T> buffer, int length) where T : struct
        {
            if (!buffer.IsCreated || buffer.Length < length)
            {
                int capacity = math.max(1, math.max(length, buffer.IsCreated ? buffer.Length * 2 : 16));
                if (buffer.IsCreated) buffer.Dispose();
                buffer = new NativeArray<T>(capacity, Allocator.Persistent);
            }
            return buffer.GetSubArray(0, length);
        }

        static void DisposeBuffer<T>(ref NativeArray<T> buffer) where T : struct
        {
            if (buffer.IsCreated) buffer.Dispose();
            buffer = default;
        }

        /// <summary>Returns future boss timing to the live stage asset value.</summary>
        public void ClearBossTimeOverride() => bossAtSecondsOverride = null;

        /// <summary>Applies live child-asset tuning while simulation is paused, then updates presentation.</summary>
        public void RefreshConfiguration()
        {
            if (!world.IsCreated) return;
            config.ValidateAll();
            ApplyLiveConfig();
            Snapshot();
        }

        public void Step(float dt, float2 move, float2 aim, CombatStats stats)
        {
            if (!world.IsCreated || won || lost) { Snapshot(); return; }
            config.ValidateAll();
            dt = math.clamp(dt, 0f, config.stage.maxSimulationStep);
            if (dt <= 0f) { Snapshot(); return; }
            ApplyLiveConfig();
            elapsed += dt;
            scrollDistance += config.stage.scrollSpeed * dt;
            stats = Normalize(stats);
            SetPlayerMotion(move, stats.MoveSpeedMultiplier);
            SpawnEnemies(dt);
            Fire(dt, aim, stats);
            world.PushTime(new TimeData(elapsed, dt));
            try { movementSystem.Update(world.Unmanaged); }
            finally { world.PopTime(); }
            ResolveBoundsAndLifetime(dt, stats);
            ResolveCollisions(stats);
            FireShooters(dt);
            if (bossSpawned) FireBoss(dt);
            Snapshot();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugGrantXp(int amount) { collectedXp += math.max(0, amount); }
        public void DebugSetHealth(float amount) { health = math.clamp(amount, 0f, maxHealth); lost = health <= 0f; }
        public void DebugAdvanceToBoss() { elapsed = math.max(elapsed, BossAtSeconds); if (!bossSpawned) SpawnBoss(); Snapshot(); }
        public void DebugDefeatBoss()
        {
            var all = EnemyEntities();
            for (int i = 0; i < all.Length; i++) if (entities.GetComponentData<CombatBody>(all[i]).Variant == Boss) Release(all[i]);
            all.Dispose(); bossSpawned = true; won = true; BossHealth01 = 0f; Snapshot();
        }
#endif

        // ScriptableObjects stay on the managed side. ECS only receives primitive values,
        // refreshed at each simulation step so Inspector changes apply to live bodies.
        void ApplyLiveConfig()
        {
            float missingHealth = math.max(0f, maxHealth - health);
            maxHealth = config.player.maxHealth;
            health = math.clamp(maxHealth - missingHealth, 0f, maxHealth);
            if (health <= 0f) lost = true;
            if (IsAlive(player))
            {
                var playerBody = entities.GetComponentData<CombatBody>(player);
                playerBody.Radius = config.player.contactRadius;
                entities.SetComponentData(player, playerBody);
                entities.SetComponentData(player, new CombatHealth { Value = health, Maximum = maxHealth });
            }

            var all = bodyQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < all.Length; i++)
            {
                Entity entity = all[i]; if (!IsAlive(entity)) continue;
                var body = entities.GetComponentData<CombatBody>(entity);
                if (body.Kind == Ball)
                {
                    var flight = entities.GetComponentData<CombatBallFlight>(entity);
                    body.Radius = body.Variant == 0 ? config.ball.basicRadius : body.Variant == 4 ? config.ball.fusionRadius : config.ball.activeRadius;
                    entities.SetComponentData(entity, body);
                    var hit = entities.GetComponentData<CombatBallHit>(entity);
                    hit.Splash = body.Variant == 1 || body.Variant == 4 ? config.ball.splashDamageMultiplier : 0f;
                    hit.Slow = body.Variant == 2 || body.Variant == 4 ? config.ball.slowSeconds : 0f;
                    entities.SetComponentData(entity, hit);
                    if (flight.Returning == 0)
                    {
                        var velocity = entities.GetComponentData<CombatVelocity>(entity);
                        float speed = config.ball.speed;
                        velocity.Value = math.normalizesafe(velocity.Value, new float2(0f, 1f)) * speed;
                        entities.SetComponentData(entity, velocity);
                        entities.SetComponentData(entity, new CombatBaseVelocity { Value = velocity.Value });
                    }
                    continue;
                }
                if (body.Kind == Enemy)
                {
                    float2 baseVelocity = body.Variant == Boss ? new float2(0f, -config.enemy.bossMoveSpeed) : new float2(0f, -config.stage.scrollSpeed);
                    if (body.Variant == Boss && entities.GetComponentData<CombatPosition>(entity).Value.y <= config.enemy.bossStopY) baseVelocity = float2.zero;
                    SetLiveVelocity(entity, baseVelocity);
                    continue;
                }
                if (body.Kind == Xp) SetLiveVelocity(entity, new float2(0f, -config.stage.scrollSpeed));
                if (body.Kind == EnemyBullet && entities.HasComponent<CombatProjectileMotion>(entity))
                {
                    var projectile = entities.GetComponentData<CombatProjectileMotion>(entity);
                    float speed = projectile.Type == 1 ? config.enemy.shooterBulletSpeed : projectile.Type == 2 ? config.enemy.bossFanBulletSpeed : config.enemy.bossAimBulletSpeed;
                    body.Radius = projectile.Type == 1 ? config.enemy.shooterBulletRadius : config.enemy.bossBulletRadius;
                    entities.SetComponentData(entity, body);
                    SetLiveVelocity(entity, projectile.Direction * speed + new float2(0f, -config.stage.scrollSpeed));
                }
            }
            all.Dispose();
        }

        void SetLiveVelocity(Entity entity, float2 baseVelocity)
        {
            var effect = entities.GetComponentData<CombatEffect>(entity);
            float2 velocity = effect.SlowRemaining > 0f ? baseVelocity * config.ball.slowSpeedMultiplier : baseVelocity;
            entities.SetComponentData(entity, new CombatBaseVelocity { Value = baseVelocity });
            entities.SetComponentData(entity, new CombatVelocity { Value = velocity });
        }

        static CombatStats Normalize(CombatStats s)
        {
            s.BallCount = math.max(1, s.BallCount);
            s.DamageMultiplier = s.DamageMultiplier <= 0 ? 1 : s.DamageMultiplier;
            s.FireRateMultiplier = s.FireRateMultiplier <= 0 ? 1 : s.FireRateMultiplier;
            s.MoveSpeedMultiplier = s.MoveSpeedMultiplier <= 0 ? 1 : s.MoveSpeedMultiplier;
            s.PickupRadius = s.PickupRadius <= 0 ? 1 : s.PickupRadius;
            return s;
        }

        Entity CreateBody(float2 position, float2 velocity, float radius, int kind, int variant, float hp, float maxHp, float lifetime)
        {
            Entity e = Rent(kind, variant, position);
            if (e == Entity.Null) return Entity.Null;
            entities.SetComponentData(e, new CombatEffect());
            entities.SetComponentData(e, new CombatPosition { Value = position });
            entities.SetComponentData(e, new CombatVelocity { Value = velocity });
            entities.SetComponentData(e, new CombatBaseVelocity { Value = velocity });
            entities.SetComponentData(e, new CombatBody { Radius = radius, Kind = kind, Variant = variant });
            entities.SetComponentData(e, new CombatHealth { Value = hp, Maximum = maxHp });
            entities.SetComponentData(e, new CombatLifetime { Value = lifetime });
            if (kind == Enemy)
            {
                
                if (variant == Shooter) entities.SetComponentData(e, new CombatEnemyShot { Cooldown = random.NextFloat(config.enemy.shooterInitialCooldownMin, config.enemy.shooterInitialCooldownMax) });
            }
            if (kind == Ball) { entities.SetComponentData(e, new CombatBallHit()); entities.SetComponentData(e, new CombatBallFlight()); }
            return e;
        }

        Entity Rent(int kind, int variant, float2 position)
        {
            // A using variable is readonly: mutating its nested pool would lose native container headers.
            var access = EntityPoolAccess.Open(entities, poolEntity);
            try { return access.Pool.Get(entities, MvpCombatPools.Id(kind, variant),
                LocalTransform.FromPosition(position.x, 0, position.y), session); }
            finally { access.Dispose(); }
        }

        void ReturnSession()
        {
            var access = EntityPoolAccess.Open(entities, poolEntity);
            try { access.Pool.ReturnSession(entities, session); }
            finally { access.Dispose(); }
        }

        bool IsAlive(Entity entity) => entities.Exists(entity) && !entities.HasComponent<Disabled>(entity);

        void Release(Entity entity)
        {
            if (!IsAlive(entity)) return;
            var access = EntityPoolAccess.Open(entities, poolEntity);
            try { access.Pool.Return(entities, entity); }
            finally { access.Dispose(); }
        }

        void SetPlayerMotion(float2 move, float speedMultiplier)
        {
            if (!IsAlive(player)) return;
            float2 direction = math.lengthsq(move) > 1 ? math.normalize(move) : move;
            var velocity = new CombatVelocity { Value = direction * (config.player.moveSpeed * speedMultiplier) };
            entities.SetComponentData(player, velocity);
            entities.SetComponentData(player, new CombatBaseVelocity { Value = velocity.Value });
        }

        void SpawnEnemies(float dt)
        {
            if (!bossSpawned && elapsed >= BossAtSeconds) { SpawnBoss(); return; }
            if (bossSpawned) return;
            if (EntityCount >= config.stage.spawnEntityCap) return;
            spawnTimer -= dt;
            if (spawnTimer > 0) return;
            float ramp = math.saturate(elapsed / config.enemy.spawnRampSeconds);
            spawnTimer = math.lerp(config.enemy.spawnIntervalStart, config.enemy.spawnIntervalEnd, ramp);
            int count = 1 + (int)(elapsed / math.max(.01f, config.enemy.countRampSeconds));
            count = math.min(count, math.max(0, config.stage.spawnEntityCap - EntityCount));
            if (count == 0) return;
            var plans = Slice(ref wavePlanBuffer, count);
            var randomAfter = Slice(ref waveRandomBuffer, 1);
            new CombatWavePlanJob
            {
                Random = random,
                Elapsed = elapsed,
                ShooterStart = config.enemy.shooterStartSeconds,
                ShooterChance = config.enemy.shooterChanceStart + ramp * config.enemy.shooterChanceRamp,
                Left = config.stage.left,
                Right = config.stage.right,
                SpawnY = config.enemy.spawnY,
                RowSpacing = config.enemy.spawnRowSpacing,
                Plans = plans,
                RandomAfter = randomAfter
            }.Run();
            random = randomAfter[0];
            for (int i = 0; i < count; i++)
            {
                var plan = plans[i];
                bool shooter = plan.Shooter != 0;
                float hp = shooter ? config.enemy.shooterHealth : config.enemy.regularHealth + elapsed * config.enemy.regularHealthPerSecond;
                CreateBody(plan.Position, new float2(0, -config.stage.scrollSpeed),
                    shooter ? config.enemy.shooterRadius : config.enemy.regularRadius,
                    Enemy, shooter ? Shooter : Regular, hp, hp, -1);
            }
        }

        void SpawnBoss()
        {
            bossSpawned = true;
            CreateBody(new float2(0, config.enemy.bossSpawnY), new float2(0, -config.enemy.bossMoveSpeed), config.enemy.bossRadius, Enemy, Boss, config.enemy.bossHealth, config.enemy.bossHealth, -1);
        }

        void Fire(float dt, float2 suppliedAim, CombatStats stats)
        {
            fireTimer = math.max(0, fireTimer - dt);
            specialTimer = math.max(0, specialTimer - dt);
            if (math.lengthsq(suppliedAim) >= .001f) lastAim = math.normalize(suppliedAim);
            float2 aim = lastAim;
            aim = ClampForward(aim);
            int available = math.max(0, stats.BallCount - InFlight(0));
            if (fireTimer <= 0 && available > 0)
            {
                fireTimer = config.ball.fireInterval / stats.FireRateMultiplier;
                int n = available;
                for (int i = 0; i < n; i++)
                {
                    float angle = n == 1 ? 0f : math.radians(math.lerp(-config.ball.spreadAngle, config.ball.spreadAngle, (float)i / (n - 1)));
                    float2 direction = ClampForward(Rotate(aim, angle));
                    float2 origin = PlayerPosition + (UsePlayerLaunchOffset ? PlayerLaunchOffset : direction * config.ball.basicSpawnOffset);
                    if (CreateBody(origin, direction * config.ball.speed, config.ball.basicRadius, Ball, 0, config.ball.baseDamage * stats.DamageMultiplier, config.ball.baseDamage * stats.DamageMultiplier, -1f) != Entity.Null) ShotsFired++;
                }
            }
            if (specialTimer <= 0 && (stats.FireLevel + stats.IceLevel + stats.StormLevel + stats.FusionLevel) > 0)
            {
                specialTimer = config.ball.fireInterval / stats.FireRateMultiplier;
                SpawnActive(aim, stats.FireLevel, 1, config.ball.fireDamageMultiplier, stats);
                SpawnActive(aim, stats.IceLevel, 2, config.ball.iceDamageMultiplier, stats);
                SpawnActive(aim, stats.StormLevel, 3, config.ball.stormDamageMultiplier, stats);
                SpawnActive(aim, stats.FusionLevel, 4, config.ball.fusionDamageMultiplier, stats);
            }
        }

        int InFlight(int variant)
        {
            using var query = entities.CreateEntityQuery(typeof(CombatBallFlight), typeof(CombatBody));
            using var all = query.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in all) if (entities.GetComponentData<CombatBody>(entity).Variant == variant) count++;
            return count;
        }

        void SpawnActive(float2 aim, int level, int variant, float damage, CombatStats stats)
        {
            if (level <= 0 || InFlight(variant) >= 1) return;
            float2 direction = ClampForward(Rotate(aim, math.radians(random.NextFloat(-config.ball.activeRandomAngle, config.ball.activeRandomAngle))));
            float value = config.ball.baseDamage * damage * (1 + config.ball.activeDamagePerLevel * level) * stats.DamageMultiplier;
            float2 origin = PlayerPosition + (UsePlayerLaunchOffset ? PlayerLaunchOffset : direction * config.ball.activeSpawnOffset);
            Entity ball = CreateBody(origin, direction * config.ball.speed, variant == 4 ? config.ball.fusionRadius : config.ball.activeRadius, Ball, variant, value, value, -1f);
            if (ball == Entity.Null) return;
            ShotsFired++;
            entities.SetComponentData(ball, new CombatBallFlight { Level = level });
            entities.SetComponentData(ball, new CombatBallHit
            {
                Pierce = variant == 3 ? config.ball.stormPierce : 0,
                Splash = variant == 1 || variant == 4 ? config.ball.splashDamageMultiplier : 0f,
                Slow = variant == 2 || variant == 4 ? config.ball.slowSeconds : 0f
            });
        }

        void ResolveBoundsAndLifetime(float dt, CombatStats stats)
        {
            using var query = entities.CreateEntityQuery(typeof(CombatPosition), typeof(CombatVelocity), typeof(CombatBody), typeof(CombatLifetime));
            using var all = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < all.Length; i++)
            {
                Entity e = all[i]; var p = entities.GetComponentData<CombatPosition>(e); var v = entities.GetComponentData<CombatVelocity>(e);
                var b = entities.GetComponentData<CombatBody>(e); var life = entities.GetComponentData<CombatLifetime>(e);
                if (b.Kind == -1) { p.Value = math.clamp(p.Value, new float2(config.stage.playerMin.x, config.stage.playerMin.y), new float2(config.stage.playerMax.x, config.stage.playerMax.y)); entities.SetComponentData(e, p); continue; }
                if (b.Kind == Enemy && b.Variant == Boss && p.Value.y <= config.enemy.bossStopY)
                {
                    p.Value.y = config.enemy.bossStopY; v.Value.y = 0f; entities.SetComponentData(e, p); entities.SetComponentData(e, v); entities.SetComponentData(e, new CombatBaseVelocity { Value = v.Value });
                }
                if (b.Kind == Ball)
                {
                    var flight = entities.GetComponentData<CombatBallFlight>(e);
                    int result = MvpBallFlight.Resolve(ref p.Value, ref v.Value, ref flight, b.Radius, PlayerPosition, dt,
                        config.stage.top, config.stage.rear, config.stage.left, config.stage.right, config.ball.returnSpeed,
                        config.ball.arrivalDistance, config.ball.maxFlightSeconds);
                    if ((result & MvpBallFlight.TopBounce) != 0) TopWallBounces++;
                    if ((result & MvpBallFlight.Collected) != 0)
                    { ReturnedBalls++; Release(e); continue; }
                    entities.SetComponentData(e, p); entities.SetComponentData(e, v);
                    entities.SetComponentData(e, new CombatBaseVelocity { Value = v.Value });
                    entities.SetComponentData(e, flight);
                    var hit = entities.GetComponentData<CombatBallHit>(e);
                    hit.Cooldown -= dt;
                    if (hit.Cooldown <= 0) hit.Last = Entity.Null;
                    entities.SetComponentData(e, hit);
                    continue; // Balls leave the arena only after arriving at the player.
                }
                if (life.Value > 0) { life.Value -= dt; entities.SetComponentData(e, life); }
                if (life.Value <= 0 && life.Value != -1 || p.Value.y < config.stage.cleanupMinY || p.Value.y > config.stage.cleanupMaxY || math.abs(p.Value.x) > config.stage.cleanupAbsX) if (IsAlive(e)) Release(e);
            }
        }

        void ResolveCollisions(CombatStats stats)
        {
            var enemies = EnemyEntities();
            using var movers = entities.CreateEntityQuery(typeof(CombatPosition), typeof(CombatVelocity), typeof(CombatBody), typeof(CombatHealth));
            using var all = movers.ToEntityArray(Allocator.Temp);
            float2 pp = PlayerPosition;
            var enemiesForJob = Slice(ref collisionEnemyBuffer, enemies.Length);
            var bodiesForJob = Slice(ref collisionBodyBuffer, all.Length);
            var collisionResults = Slice(ref collisionResultBuffer, all.Length);
            for (int j = 0; j < enemies.Length; j++)
            {
                Entity entity = enemies[j];
                var body = entities.GetComponentData<CombatBody>(entity);
                enemiesForJob[j] = new CombatCollisionBody
                {
                    Entity = entity,
                    Position = entities.GetComponentData<CombatPosition>(entity).Value,
                    Radius = body.Radius,
                    Kind = body.Kind
                };
            }
            for (int i = 0; i < all.Length; i++)
            {
                Entity entity = all[i];
                var body = entities.GetComponentData<CombatBody>(entity);
                var candidate = new CombatCollisionBody
                {
                    Entity = entity,
                    Position = entities.GetComponentData<CombatPosition>(entity).Value,
                    Radius = body.Radius,
                    Kind = body.Kind
                };
                if (body.Kind == Ball)
                {
                    candidate.Returning = entities.GetComponentData<CombatBallFlight>(entity).Returning;
                    var hit = entities.GetComponentData<CombatBallHit>(entity);
                    candidate.LastHit = hit.Last;
                    candidate.HitCooldown = hit.Cooldown;
                }
                bodiesForJob[i] = candidate;
            }
            var collisionJob = new CombatCollisionJob
            {
                Bodies = bodiesForJob,
                Enemies = enemiesForJob,
                Results = collisionResults,
                Player = pp,
                PlayerHitRadius = config.player.hitRadius,
                PlayerContactRadius = config.player.contactRadius
            };
            if (all.Length >= 32) collisionJob.Schedule(all.Length, 32).Complete();
            else collisionJob.Run(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                Entity e = all[i]; if (!IsAlive(e)) continue;
                var body = entities.GetComponentData<CombatBody>(e); var pos = entities.GetComponentData<CombatPosition>(e);
                if (body.Kind == Ball)
                {
                    if (entities.GetComponentData<CombatBallFlight>(e).Returning != 0) continue;
                    var hit = entities.GetComponentData<CombatBallHit>(e);
                    for (int j = collisionResults[i].FirstEnemy; j >= 0 && j < enemies.Length; j++)
                    {
                        Entity target = enemies[j]; if (!IsAlive(target)) continue;
                        var targetPos = entities.GetComponentData<CombatPosition>(target); var targetBody = entities.GetComponentData<CombatBody>(target);
                        if (targetBody.Kind != Enemy) continue;
                        if (hit.Last == target && hit.Cooldown > 0f) continue;
                        if (math.distancesq(pos.Value, targetPos.Value) > math.pow(body.Radius + targetBody.Radius, 2)) continue;
                        float damage = entities.GetComponentData<CombatHealth>(e).Value;
                        var targetHealth = entities.GetComponentData<CombatHealth>(target); targetHealth.Value -= damage;
                        hit.Last = target; hit.Cooldown = config.ball.hitCooldown; entities.SetComponentData(e, hit);
                        if (hit.Slow > 0f) SlowEnemy(target, hit.Slow);
                        if (hit.Splash > 0f) Splash(targetPos.Value, target, damage * hit.Splash, hit.Slow);
                        if (hit.Pierce <= 0)
                        {
                            var velocity = entities.GetComponentData<CombatVelocity>(e);
                            float2 normal = math.normalizesafe(pos.Value - targetPos.Value, new float2(0f, 1f));
                            velocity.Value = math.reflect(velocity.Value, normal);
                            entities.SetComponentData(e, velocity); entities.SetComponentData(e, new CombatBaseVelocity { Value = velocity.Value });
                        }
                        if (targetHealth.Value <= 0) KillEnemy(target); else entities.SetComponentData(target, targetHealth);
                        break;
                    }
                }
                else if (body.Kind == EnemyBullet && collisionResults[i].TouchesPlayer != 0) DamagePlayer(config.enemy.bulletDamage);
                else if (body.Kind == Xp)
                {
                    float distance = math.distance(pos.Value, pp);
                    if (distance < config.experience.magnetRadius * stats.PickupRadius)
                    {
                        var velocity = entities.GetComponentData<CombatVelocity>(e);
                        velocity.Value = math.lerp(velocity.Value, math.normalizesafe(pp - pos.Value) * config.experience.magnetSpeed, config.experience.magnetVelocityBlend);
                        entities.SetComponentData(e, velocity);
                        entities.SetComponentData(e, new CombatBaseVelocity { Value = velocity.Value });
                    }
                    if (distance < config.experience.collectDistance) { collectedXp += body.Variant; Release(e); }
                }
                else if (body.Kind == Enemy && collisionResults[i].TouchesPlayer != 0) DamagePlayer(body.Variant == Boss ? config.enemy.bossContactDamage : config.enemy.shooterContactDamage);
            }
            enemies.Dispose();
            UpdateBossHealth();
        }

        NativeArray<Entity> EnemyEntities()
        {
            using var query = entities.CreateEntityQuery(typeof(CombatEnemy), typeof(CombatPosition), typeof(CombatBody), typeof(CombatHealth));
            return query.ToEntityArray(Allocator.Temp);
        }

        void SlowEnemy(Entity target, float seconds)
        {
            if (!IsAlive(target)) return;
            var baseVelocity = entities.GetComponentData<CombatBaseVelocity>(target);
            entities.SetComponentData(target, new CombatVelocity { Value = baseVelocity.Value * config.ball.slowSpeedMultiplier });
            entities.SetComponentData(target, new CombatEffect { SlowRemaining = seconds });
        }

        void Splash(float2 center, Entity primary, float damage, float slow)
        {
            var enemies = EnemyEntities();
            for (int i = 0; i < enemies.Length; i++)
            {
                Entity e = enemies[i]; if (e == primary || !IsAlive(e)) continue;
                if (math.distancesq(center, entities.GetComponentData<CombatPosition>(e).Value) > config.ball.splashRadius * config.ball.splashRadius) continue;
                var h = entities.GetComponentData<CombatHealth>(e); h.Value -= damage;
                if (slow > 0f) SlowEnemy(e, slow);
                if (h.Value <= 0f) KillEnemy(e); else entities.SetComponentData(e, h);
            }
            enemies.Dispose();
        }

        void KillEnemy(Entity enemy)
        {
            if (!IsAlive(enemy)) return;
            var p = entities.GetComponentData<CombatPosition>(enemy).Value; var b = entities.GetComponentData<CombatBody>(enemy);
            Release(enemy); kills++;
            if (b.Variant == Boss) { won = true; BossHealth01 = 0; return; }
            int drops = b.Variant == Shooter ? config.experience.shooterDropCount : config.experience.regularDropCount;
            for (int i = 0; i < drops; i++)
            {
                if (EntityCount >= config.stage.xpEntityCap) break;
                int amount = b.Variant == Shooter && i == 0 ? config.experience.largeValue : (random.NextFloat() < config.experience.rareChance ? config.experience.rareValue : (random.NextFloat() < config.experience.bonusChance ? config.experience.largeValue : config.experience.smallValue));
                CreateBody(p + random.NextFloat2Direction() * config.experience.scatterRadius, new float2(0, -config.stage.scrollSpeed), config.experience.radius, Xp, amount, 1, 1, -1);
            }
        }

        void DamagePlayer(float amount)
        {
            if (elapsed < invulnerableUntil || lost) return;
            invulnerableUntil = elapsed + config.player.invulnerabilitySeconds; health = math.max(0, health - amount); lost = health <= 0;
        }

        void FireShooters(float dt)
        {
            if (elapsed < config.enemy.shooterStartSeconds) return;
            using var query = entities.CreateEntityQuery(typeof(CombatEnemy), typeof(CombatEnemyShot), typeof(CombatPosition));
            using var shooters = query.ToEntityArray(Allocator.TempJob);
            if (shooters.Length == 0) return;
            var plans = Slice(ref shooterPlanBuffer, shooters.Length);
            for (int i = 0; i < shooters.Length; i++)
            {
                var shot = entities.GetComponentData<CombatEnemyShot>(shooters[i]);
                plans[i] = new CombatShooterPlan
                {
                    Entity = shooters[i],
                    Position = entities.GetComponentData<CombatPosition>(shooters[i]).Value,
                    Cooldown = shot.Cooldown,
                    Telegraph = shot.Telegraph
                };
            }
            var job = new CombatShooterPlanJob
            {
                Plans = plans,
                Player = PlayerPosition,
                DeltaTime = dt,
                Cooldown = config.enemy.shooterCooldown,
                TelegraphSeconds = config.enemy.shooterTelegraphSeconds
            };
            if (shooters.Length >= 32) job.Schedule(shooters.Length, 32).Complete();
            else job.Run(shooters.Length);
            for (int i = 0; i < plans.Length; i++)
            {
                var plan = plans[i];
                if (!IsAlive(plan.Entity)) continue;
                if (plan.Fire != 0)
                {
                    Entity bullet = CreateBody(plan.Position,
                        plan.Direction * config.enemy.shooterBulletSpeed + new float2(0, -config.stage.scrollSpeed),
                        config.enemy.shooterBulletRadius, EnemyBullet, 0, 1, 1, config.enemy.shooterBulletLifetime);
                    if (bullet != Entity.Null) entities.SetComponentData(bullet, new CombatProjectileMotion { Direction = plan.Direction, Type = 1 });
                }
                entities.SetComponentData(plan.Entity, new CombatEnemyShot { Cooldown = plan.Cooldown, Telegraph = plan.Telegraph });
            }
        }

        void FireBoss(float dt)
        {
            bossShotTimer -= dt; if (bossShotTimer > 0) return;
            Entity boss = Entity.Null; var list = EnemyEntities();
            for (int i = 0; i < list.Length; i++) if (entities.GetComponentData<CombatBody>(list[i]).Variant == Boss) { boss = list[i]; break; }
            list.Dispose(); if (boss == Entity.Null) return;
            bossShotTimer = config.enemy.bossShotInterval;
            float2 origin = entities.GetComponentData<CombatPosition>(boss).Value;
            float2 target = PlayerPosition;
            if (((int)((elapsed - BossAtSeconds) / config.enemy.bossShotInterval)) % config.enemy.bossPatternCycle == 0)
            {
                for (int i = -config.enemy.bossFanHalfCount; i <= config.enemy.bossFanHalfCount; i++)
                {
                    if (i == 0) continue; // A readable central gap in the wide fan.
                    float2 direction = Rotate(new float2(0, -1), i * config.enemy.bossFanAngleStepRadians);
                    Entity bullet = CreateBody(origin, direction * config.enemy.bossFanBulletSpeed + new float2(0, -config.stage.scrollSpeed), config.enemy.bossBulletRadius, EnemyBullet, 0, 1, 1, config.enemy.bossFanBulletLifetime);
                    if (bullet != Entity.Null) entities.SetComponentData(bullet, new CombatProjectileMotion { Direction = direction, Type = 2 });
                }
            }
            else
            {
                for (int i = -1; i <= 1; i++) { float2 d = Rotate(math.normalizesafe(target - origin, new float2(0, -1)), i * config.enemy.bossAimAngleStepRadians); Entity bullet = CreateBody(origin, d * config.enemy.bossAimBulletSpeed + new float2(0, -config.stage.scrollSpeed), config.enemy.bossBulletRadius, EnemyBullet, 0, 1, 1, config.enemy.bossAimBulletLifetime); if (bullet != Entity.Null) entities.SetComponentData(bullet, new CombatProjectileMotion { Direction = d, Type = 3 }); }
            }
        }

        void UpdateBossHealth()
        {
            if (!bossSpawned || won) return;
            var list = EnemyEntities(); bool found = false;
            for (int i = 0; i < list.Length; i++) { var b = entities.GetComponentData<CombatBody>(list[i]); if (b.Variant == Boss) { var h = entities.GetComponentData<CombatHealth>(list[i]); BossHealth01 = math.saturate(h.Value / h.Maximum); found = true; break; } }
            list.Dispose(); if (!found) BossHealth01 = 0;
        }

        void Snapshot()
        {
            Visuals.Clear(); if (!world.IsCreated) return;
            using var query = entities.CreateEntityQuery(typeof(CombatPosition), typeof(CombatBody), typeof(CombatHealth));
            using var all = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < all.Length; i++) { var e = all[i]; var b = entities.GetComponentData<CombatBody>(e); if (b.Kind < 0) continue; var h = entities.GetComponentData<CombatHealth>(e); float telegraph = entities.HasComponent<CombatEnemyShot>(e) ? entities.GetComponentData<CombatEnemyShot>(e).Telegraph : 0f; Visuals.Add(new CombatVisual { Id = e.Index, Returning = b.Kind == Ball && entities.GetComponentData<CombatBallFlight>(e).Returning != 0, Position = entities.GetComponentData<CombatPosition>(e).Value, Radius = b.Radius, Kind = b.Kind, Variant = b.Variant, Health01 = h.Maximum > 0 ? math.saturate(h.Value / h.Maximum) : 1, Telegraph01 = telegraph }); }
        }

        float2 ClampForward(float2 v) { return Rotate(new float2(0, 1), math.clamp(math.atan2(-v.x, v.y), math.radians(-config.stage.aimHalfAngle), math.radians(config.stage.aimHalfAngle))); }
        static float2 Rotate(float2 v, float radians) { float s = math.sin(radians), c = math.cos(radians); return new float2(v.x * c - v.y * s, v.x * s + v.y * c); }
    }
}
