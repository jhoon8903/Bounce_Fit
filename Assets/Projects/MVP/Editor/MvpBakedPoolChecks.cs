using System;
using Framework.Poolable.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace BounceFit.Mvp.Editor
{
    public static class MvpBakedPoolChecks
    {
        [MenuItem("Bounce Fit/MVP/Run Baked Pool Checks")]
        public static void Run()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<MvpGame>();
            if (!Application.isPlaying || !game || game.Combat == null)
                throw new InvalidOperationException("Wait for the ForestCombat SubScene to load in Play mode.");
            Require(game.Combat.UsesBakedPool, "Game uses the baked default World");
            var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using var catalog = manager.CreateEntityQuery(typeof(EntityPoolConfig), typeof(PoolPrefabElement));
            var entries = catalog.GetSingletonBuffer<PoolPrefabElement>();
            Require(entries.Length >= MvpCombatPools.Names.Length, "Baked catalog contains all MVP prefabs");
            for (int i = 0; i < entries.Length; i++) Require(manager.Exists(entries[i].Prefab), "Baked prefab exists");
            using var query = manager.CreateEntityQuery(typeof(EntityPool));
            game.StartRun();
            game.TogglePause();
            var pool = query.GetSingleton<EntityPool>();
            var id = MvpCombatPools.Id(1, 0);
            pool.TryGetStats(id, out _, out _, out int before);
            game.Combat.Step(.01f, float2.zero, new float2(0, 1), game.Progression.Stats);
            pool = query.GetSingleton<EntityPool>();
            pool.TryGetStats(id, out _, out int active, out int total);
            Require(active > 0, "Base Ball rented from the catalog pool");
            Require(total == before, "Prewarmed entity reused without growth");
            game.StartRun();
            game.TogglePause();
            pool = query.GetSingleton<EntityPool>();
            pool.TryGetStats(id, out _, out active, out total);
            Require(active == 0 && total == before, "Restart returns the previous session to the same pool");
            Debug.Log("Baked pool checks PASS: streamed catalog, 11 prefab references, prewarmed rent, session return, persistent pool reuse.");
        }
        static void Require(bool condition, string label)
        {
            if (!condition) throw new Exception("Baked pool FAIL: " + label);
        }
    }
}
