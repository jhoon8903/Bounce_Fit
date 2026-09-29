using System;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;

namespace BounceFit.Mvp.Editor
{
    public static class MvpLiveConfigChecks
    {
        [MenuItem("Bounce Fit/MVP/Run Live Config Checks")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            var game = UnityEngine.Object.FindFirstObjectByType<MvpGame>();
            var original = game.config;
            var test = MvpConfigSet.CreateTransient();
            try
            {
                game.config = test;
                game.StartRun();
                game.TogglePause();
                var combat = game.Combat;
                test.ball.startingCount = 5;
                test.player.moveSpeed = 8;
                test.presentation.fieldOfView = 55;
                test.stage.bossAtSeconds = 120;
                game.SendMessage("Update");
                Require(ReferenceEquals(combat, game.Combat), "Same combat instance retained");
                Require(game.Progression.Stats.BallCount == 5, "Starting count changes live");
                Require(Mathf.Approximately(game.ViewCamera.fieldOfView, 55), "Camera changes while paused");
                Require(Mathf.Approximately(combat.BossAtSeconds, 120), "Boss timing changes live");
                var before = combat.PlayerPosition;
                combat.Step(.05f, new float2(1, 0), new float2(0, 1), game.Progression.Stats);
                Require(Mathf.Abs(combat.PlayerPosition.x - before.x - .4f) < .001f, "Movement speed changes live");
                test.player.maxHealth = 150;
                game.SendMessage("Update");
                Require(Mathf.Approximately(combat.MaxHealth, 150), "Maximum health changes live");
                Debug.Log("Live Config checks: 6/6 PASS (same run, live SO edits, paused presentation). Test values restored.");
            }
            finally
            {
                game.config = original;
                game.StartRun();
                game.TogglePause();
                UnityEngine.Object.Destroy(test);
            }
        }
        static void Require(bool condition, string label)
        {
            if (!condition) throw new Exception("Live config FAIL: " + label);
        }
    }
}
