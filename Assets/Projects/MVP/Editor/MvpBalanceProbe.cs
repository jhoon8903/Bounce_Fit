using System;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.IO;
using BounceFit.Mvp;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;

namespace BounceFit.Mvp.Editor
{
    /// <summary>
    /// Repeatable economy/survival smoke probe. This bot is deliberately simple and is not a human-play balance verdict.
    /// </summary>
    public static class MvpBalanceProbe
    {
        const float StepSeconds = 1f / 30f;
        const float MaxSeconds = 600f;
        static readonly uint[] Seeds = { 101u, 202u, 303u };
        static ProbeRun run;
        static readonly ProbeReport report = new ProbeReport();

        [MenuItem("Bounce Fit/MVP/Run Seeded Balance Probe")]
        public static void RunSeededBalanceProbe()
        {
            if (run != null) throw new InvalidOperationException("Balance probe is already running.");
            report.results = new ProbeResult[Seeds.Length];
            report.note = "Deterministic 30 Hz bot probe. It aims at the nearest forward enemy, pursues nearby XP, and avoids nearby bullets. It is an economy/survival signal, not a human-play result.";
            run = new ProbeRun(Seeds[0]);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("MVP seeded balance probe started (3 seeds, editor update batches).");
        }

        static void Tick()
        {
            try
            {
                // A bounded batch keeps the editor responsive while still running the exact fixed simulation timestep.
                for (int i = 0; i < 600 && run != null; i++)
                {
                    run.Step();
                    if (!run.Finished) continue;
                    report.results[run.Index] = run.ToResult();
                    run.Dispose();
                    int next = run.Index + 1;
                    run = next < Seeds.Length ? new ProbeRun(Seeds[next], next) : null;
                }
                if (run == null) Finish();
            }
            catch (Exception exception)
            {
                run?.Dispose(); run = null;
                EditorApplication.update -= Tick;
                UnityEngine.Debug.LogException(exception);
            }
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            string root = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(root, "Captures", "MVP");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "balance-probe.json"), JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(directory, "balance-probe.md"), Markdown());
            UnityEngine.Debug.Log("MVP balance probe complete: " + directory);
        }

        static string Markdown()
        {
            string text = "# MVP seeded balance probe\n\n" + report.note + "\n\n";
            text += "| Seed | Elapsed | Won | Lost | Kills | Level | Damage taken | HP | Peak entities | Wall time |\n";
            text += "|---:|---:|:---:|:---:|---:|---:|---:|---:|---:|---:|\n";
            foreach (ProbeResult result in report.results)
                text += string.Format("| {0} | {1:F1}s | {2} | {3} | {4} | {5} | {6:F1} | {7:F1} | {8} | {9:F3}s |\n", result.seed, result.elapsed, result.won, result.lost, result.kills, result.level, result.damage, result.hp, result.peakEntityCount, result.walltime);
            return text;
        }

        static void ApplyPriorityChoice(MvpProgression progression)
        {
            while (progression.IsChoosing)
            {
                int selected = 0, best = int.MaxValue;
                for (int i = 0; i < progression.Choices.Count; i++)
                {
                    int priority = Priority(progression.Choices[i].Id);
                    if (priority < best) { best = priority; selected = i; }
                }
                if (!progression.Choose(selected)) throw new InvalidOperationException("Production progression rejected its offered card.");
            }
        }

        static int Priority(string id)
        {
            switch (id)
            {
                case "ball": return 0;
                case "fire": return 1;
                case "ice": return 2;
                case "fusion": return 3;
                case "power": return 4;
                case "haste": return 5;
                case "storm": return 6;
                case "magnet": return 7;
                case "boots": return 8;
                case "mastery": return 9;
                case "precision": return 10;
                default: return 11;
            }
        }

        sealed class ProbeRun : IDisposable
        {
            public readonly int Index;
            readonly uint seed;
            readonly MvpCombat combat;
            readonly MvpProgression progression;
            readonly Stopwatch stopwatch = Stopwatch.StartNew();
            int seenXp, peakEntities;

            public bool Finished => combat.Won || combat.Lost || combat.Elapsed >= MaxSeconds;

            public ProbeRun(uint value, int index = 0)
            {
                seed = value; Index = index;
                combat = new MvpCombat(seed) { BossAtSeconds = 360f };
                progression = new MvpProgression((int)seed);
            }

            public void Step()
            {
                int gained = combat.CollectedXp - seenXp;
                if (gained > 0) { progression.AddXp(gained); seenXp = combat.CollectedXp; }
                ApplyPriorityChoice(progression);
                BotInput(combat, out float2 move, out float2 aim);
                combat.Step(StepSeconds, move, aim, progression.Stats);
                peakEntities = math.max(peakEntities, combat.EntityCount);
            }

            public ProbeResult ToResult()
            {
                int gained = combat.CollectedXp - seenXp;
                if (gained > 0) { progression.AddXp(gained); seenXp = combat.CollectedXp; }
                ApplyPriorityChoice(progression);
                stopwatch.Stop();
                return new ProbeResult
                {
                    seed = seed, elapsed = combat.Elapsed, won = combat.Won, lost = combat.Lost,
                    kills = combat.Kills, level = progression.Level, damage = combat.MaxHealth - combat.Health,
                    hp = combat.Health, peakEntityCount = peakEntities, walltime = (float)stopwatch.Elapsed.TotalSeconds
                };
            }

            public void Dispose() { combat.Dispose(); }
        }

        static void BotInput(MvpCombat combat, out float2 move, out float2 aim)
        {
            float2 player = combat.PlayerPosition;
            float2 nearestEnemy = new float2(0f, 1f), nearestXp = float2.zero, nearestBullet = float2.zero;
            float enemyDistance = float.MaxValue, xpDistance = 4.5f, bulletDistance = 2.25f;
            foreach (CombatVisual visual in combat.Visuals)
            {
                float2 delta = visual.Position - player;
                float distance = math.length(delta);
                if (visual.Kind == 0 && delta.y > -1f && distance < enemyDistance) { nearestEnemy = delta; enemyDistance = distance; }
                else if (visual.Kind == 3 && distance < xpDistance) { nearestXp = delta; xpDistance = distance; }
                else if (visual.Kind == 2 && distance < bulletDistance) { nearestBullet = delta; bulletDistance = distance; }
            }
            aim = math.normalizesafe(nearestEnemy, new float2(0f, 1f));
            move = xpDistance < 4.5f ? math.normalizesafe(nearestXp) : float2.zero;
            if (bulletDistance < 2.25f)
            {
                float2 away = math.normalizesafe(-nearestBullet);
                move += away * 1.5f + new float2(-away.y, away.x) * .35f;
            }
            if (math.lengthsq(move) > 1f) move = math.normalize(move);
        }

        [Serializable] sealed class ProbeReport { public string note; public ProbeResult[] results; }
        [Serializable] sealed class ProbeResult
        {
            public uint seed; public float elapsed; public bool won; public bool lost; public int kills; public int level;
            public float damage; public float hp; public int peakEntityCount; public float walltime;
        }
    }
}
