using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CoinRush.Data;
using CoinRush.Level;
using UnityEditor;
using UnityEngine;

namespace CoinRush.EditorTools
{
    /// <summary>
    /// External JSON config (StreamingAssets/Config/coinrush_config.json) and the procedural
    /// fractal-tree forests placed in each world.
    /// </summary>
    public static partial class CoinRushBuilder
    {
        private static string ConfigFileAsset => "Assets/StreamingAssets/" + JsonConfig.RelativePath;

        // ------------------------------------------------------------------ forest styles

        /// <summary>Ramps and bridges of the world being built (start and end), so trees never block them.</summary>
        private static readonly List<(Vector3 a, Vector3 b)> walkways = new List<(Vector3 a, Vector3 b)>();

        /// <summary>Default L-system tree style per world (the JSON "forests" section can override it).</summary>
        private static ForestData ForestStyle(int worldIndex)
        {
            switch (worldIndex)
            {
                case 0: return new ForestData // Sunny Meadow: round green oaks
                {
                    sceneName = "World1_Meadow", treeCount = 8, seed = 11, iterations = 4, branchAngle = 28f,
                    trunkLength = 1.6f, trunkRadius = 0.28f, lengthScale = 0.78f, leafSize = 0.95f,
                    barkColor = "#6B4A2F", leafColor = "#4FB45A",
                };
                case 1: return new ForestData // Desert Ruins: wide, flat acacias
                {
                    sceneName = "World2_Desert", treeCount = 6, seed = 22, iterations = 3, branchAngle = 42f,
                    trunkLength = 1.5f, trunkRadius = 0.24f, lengthScale = 0.86f, leafSize = 1.15f,
                    barkColor = "#8A5A3C", leafColor = "#A5B84A",
                };
                default: return new ForestData // Frozen Night: slender frosted trees
                {
                    sceneName = "World3_Frozen", treeCount = 7, seed = 33, iterations = 4, branchAngle = 24f,
                    trunkLength = 1.5f, trunkRadius = 0.22f, lengthScale = 0.76f, leafSize = 0.8f,
                    barkColor = "#4A5568", leafColor = "#DDEFFF",
                };
            }
        }

        /// <summary>
        /// Picks candidate spots for procedural trees on the outer ring of the map, away from jump pads,
        /// ramps/bridges, the start, the portal and anything solid, then adds the FractalForest that grows
        /// the trees at runtime.
        /// </summary>
        private static void FractalGrove(int worldIndex, System.Func<Vector3, bool> allowed)
        {
            ForestData style = ForestStyle(worldIndex);
            Physics.SyncTransforms();
            List<Vector3> pads = Object.FindObjectsByType<JumpPad>(FindObjectsSortMode.None).Select(p => p.transform.position).ToList();

            var spots = new List<Vector3>();
            Random.State state = Random.state;
            Random.InitState(style.seed * 31);
            for (int attempt = 0; attempt < 800 && spots.Count < style.treeCount * 3; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f), radius = Random.Range(28f, 45f);
                Vector3 p = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (Mathf.Abs(p.x) > 45f || Mathf.Abs(p.z) > 45f || !allowed(p)) continue;
                if (p.z > 33f && Mathf.Abs(p.x) < 12f) continue;  // portal
                if (p.z < -31f && Mathf.Abs(p.x) < 9f) continue;  // start
                if (pads.Any(q => Flat(q - p).magnitude < 7f)) continue;
                if (walkways.Any(w => SegmentDistance(p, w.a, w.b) < 5f)) continue; // ramps and bridges
                if (spots.Any(q => Flat(q - p).magnitude < 7f)) continue;
                if (Physics.CheckSphere(p + Vector3.up * 1.5f, 1.4f, ~0, QueryTriggerInteraction.Collide)) continue; // trees, rocks, blocks, hazards
                spots.Add(p);
            }
            Random.state = state;
            walkways.Clear(); // the next world starts with its own ramps and bridges

            Material bark = Lit("Environment", "M_FractalBark", JsonConfig.ParseColor(style.barkColor, Color.gray), 0f, 0.15f);
            Material leaves = Lit("Environment", "M_FractalLeaves", JsonConfig.ParseColor(style.leafColor, Color.green), 0f, 0.25f);
            Empty("FractalForest", layoutRoot, Vector3.zero).AddComponent<FractalForest>().Configure(spots.ToArray(), bark, leaves, style);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = Flat(b - a), ap = Flat(p - a);
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return (ap - ab * t).magnitude;
        }

        // ------------------------------------------------------------------ JSON export

        [MenuItem("Coin Rush/Export Config to JSON", priority = 20)]
        private static void ExportConfigMenu()
        {
            CreateData();
            ExportConfig(overwrite: true);
            EditorUtility.RevealInFinder(ConfigFileAsset);
        }

        /// <summary>
        /// Writes the current asset values to coinrush_config.json (StreamingAssets, so it ships next to
        /// the built game as a plain text file). Build Everything only creates it when it is missing,
        /// so hand edits to the JSON are never overwritten.
        /// </summary>
        private static void ExportConfig(bool overwrite)
        {
            string path = Path.GetFullPath(ConfigFileAsset);
            if (File.Exists(path) && !overwrite) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var data = new CoinRushConfigData
            {
                rules = new RulesData
                {
                    comboWindow = gameConfig.comboWindow,
                    pickupsPerMultiplierStep = gameConfig.pickupsPerMultiplierStep,
                    maxMultiplier = gameConfig.maxMultiplier,
                    timeBonusPerSecond = gameConfig.timeBonusPerSecond,
                    countdownSeconds = gameConfig.countdownSeconds,
                },
                player = new PlayerData
                {
                    walkSpeed = playerStats.walkSpeed,
                    sprintMultiplier = playerStats.sprintMultiplier,
                    jumpHeight = playerStats.jumpHeight,
                    gravity = playerStats.gravity,
                    dashSpeed = playerStats.dashSpeed,
                    dashCooldown = playerStats.dashCooldown,
                    maxStamina = playerStats.maxStamina,
                    maxHealth = playerStats.maxHealth,
                },
                worlds = worldConfigs.Select(w => new WorldData
                {
                    sceneName = w.sceneName,
                    worldName = w.worldName,
                    coinQuota = w.coinQuota,
                    timeLimit = w.timeLimit,
                    maxActiveCoins = w.maxActiveCoins,
                    gemChance = w.gemChance,
                    startingEnemies = w.startingEnemies,
                    maxEnemies = w.maxEnemies,
                    powerUpSpawnInterval = w.powerUpSpawnInterval,
                    maxPowerUpsOnField = w.maxPowerUpsOnField,
                }).ToArray(),
                forests = Enumerable.Range(0, worldConfigs.Length).Select(ForestStyle).ToArray(),
            };
            // JsonUtility prints floats as doubles (2.200000047683716); round them for a human-friendly file.
            string json = Regex.Replace(JsonUtility.ToJson(data, true), @"-?\d+\.\d{5,}", m =>
                System.Math.Round(double.Parse(m.Value, CultureInfo.InvariantCulture), 3).ToString(CultureInfo.InvariantCulture));
            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(ConfigFileAsset);
            Debug.Log($"[Coin Rush] Wrote {ConfigFileAsset}");
        }
    }
}
