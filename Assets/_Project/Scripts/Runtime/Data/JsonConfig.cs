using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CoinRush.Data
{
    // ------------------------------------------------------------------------------------------
    // Shape of StreamingAssets/Config/coinrush_config.json (plain [Serializable] classes so that
    // Unity's JsonUtility can read and write them).
    // ------------------------------------------------------------------------------------------

    [Serializable]
    public class CoinRushConfigData
    {
        public int version = 1;
        public RulesData rules = new RulesData();
        public PlayerData player = new PlayerData();
        public WorldData[] worlds = new WorldData[0];
        public ForestData[] forests = new ForestData[0];
    }

    [Serializable]
    public class RulesData
    {
        public float comboWindow;
        public int pickupsPerMultiplierStep;
        public int maxMultiplier;
        public float timeBonusPerSecond;
        public int countdownSeconds;
    }

    [Serializable]
    public class PlayerData
    {
        public float walkSpeed;
        public float sprintMultiplier;
        public float jumpHeight;
        public float gravity;
        public float dashSpeed;
        public float dashCooldown;
        public float maxStamina;
        public float maxHealth;
    }

    [Serializable]
    public class WorldData
    {
        public string sceneName;
        public string worldName;
        public int coinQuota;
        public float timeLimit;
        public int maxActiveCoins;
        public float gemChance;
        public int startingEnemies;
        public int maxEnemies;
        public float powerUpSpawnInterval;
        public int maxPowerUpsOnField;
    }

    /// <summary>Settings for the procedural L-system trees in one world (see FractalForest).</summary>
    [Serializable]
    public class ForestData
    {
        public string sceneName;
        public int treeCount = 8;
        public int seed = 1;
        public int iterations = 4;
        public float branchAngle = 28f;
        public float trunkLength = 1.6f;
        public float trunkRadius = 0.28f;
        public float lengthScale = 0.78f;
        public float leafSize = 0.9f;
        public string barkColor = "#6B4A2F";
        public string leafColor = "#4FB45A";
    }

    /// <summary>
    /// External JSON configuration. Values in the JSON file override the ScriptableObject defaults at
    /// runtime, so the game can be re-tuned (quotas, timers, movement, procedural trees) by editing a
    /// text file, without opening Unity or rebuilding. The file is re-read every time a world loads.
    /// <para>Overrides are applied to runtime copies, never to the assets themselves. A missing file,
    /// section, entry or zero/empty value simply keeps the asset's value.</para>
    /// </summary>
    public static class JsonConfig
    {
        public const string RelativePath = "Config/coinrush_config.json";
        public static string FilePath => Path.Combine(Application.streamingAssetsPath, RelativePath);

#if UNITY_WEBGL && !UNITY_EDITOR
        // In a browser, StreamingAssets is a web address, not a folder: the file is downloaded once
        // when the game starts (long before the first world loads) and kept in memory.
        private static string webText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PreloadForWeb()
        {
            var request = UnityEngine.Networking.UnityWebRequest.Get(FilePath);
            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success) webText = request.downloadHandler.text;
                else Debug.LogWarning($"[Coin Rush] Could not download {RelativePath}: {request.error}. Using asset defaults.");
                request.Dispose();
            };
        }
#endif

        /// <summary>Reads and parses the config file (null if it is missing or invalid).</summary>
        public static CoinRushConfigData Load()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return string.IsNullOrEmpty(webText) ? null : JsonUtility.FromJson<CoinRushConfigData>(webText);
#else
                if (!File.Exists(FilePath)) return null;
                return JsonUtility.FromJson<CoinRushConfigData>(File.ReadAllText(FilePath));
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Coin Rush] Could not read {RelativePath}: {e.Message}. Using asset defaults.");
                return null;
            }
        }

        public static WorldData FindWorld(CoinRushConfigData data, string sceneName) =>
            data?.worlds?.FirstOrDefault(w => w != null && w.sceneName == sceneName);

        public static ForestData FindForest(CoinRushConfigData data, string sceneName) =>
            data?.forests?.FirstOrDefault(f => f != null && f.sceneName == sceneName);

        /// <summary>Runtime copy of <paramref name="asset"/> with the JSON rules applied.</summary>
        public static GameConfig Apply(GameConfig asset, CoinRushConfigData data)
        {
            if (asset == null || data?.rules == null) return asset;
            GameConfig c = UnityEngine.Object.Instantiate(asset);
            RulesData r = data.rules;
            if (r.comboWindow > 0f) c.comboWindow = r.comboWindow;
            if (r.pickupsPerMultiplierStep > 0) c.pickupsPerMultiplierStep = r.pickupsPerMultiplierStep;
            if (r.maxMultiplier > 0) c.maxMultiplier = r.maxMultiplier;
            if (r.timeBonusPerSecond > 0f) c.timeBonusPerSecond = r.timeBonusPerSecond;
            if (r.countdownSeconds > 0) c.countdownSeconds = r.countdownSeconds;
            return c;
        }

        /// <summary>Runtime copy of <paramref name="asset"/> with this world's JSON entry applied.</summary>
        public static WorldConfig Apply(WorldConfig asset, CoinRushConfigData data)
        {
            WorldData w = asset != null ? FindWorld(data, asset.sceneName) : null;
            if (w == null) return asset;
            WorldConfig c = UnityEngine.Object.Instantiate(asset);
            if (!string.IsNullOrEmpty(w.worldName)) c.worldName = w.worldName;
            if (w.coinQuota > 0) c.coinQuota = w.coinQuota;
            if (w.timeLimit > 0f) c.timeLimit = w.timeLimit;
            if (w.maxActiveCoins > 0) c.maxActiveCoins = w.maxActiveCoins;
            if (w.gemChance > 0f) c.gemChance = Mathf.Clamp01(w.gemChance);
            if (w.startingEnemies > 0) c.startingEnemies = w.startingEnemies;
            if (w.maxEnemies > 0) c.maxEnemies = w.maxEnemies;
            if (w.powerUpSpawnInterval > 0f) c.powerUpSpawnInterval = w.powerUpSpawnInterval;
            if (w.maxPowerUpsOnField > 0) c.maxPowerUpsOnField = w.maxPowerUpsOnField;
            return c;
        }

        /// <summary>Runtime copy of <paramref name="asset"/> with the JSON player section applied.</summary>
        public static PlayerStats Apply(PlayerStats asset, CoinRushConfigData data)
        {
            if (asset == null || data?.player == null) return asset;
            PlayerStats c = UnityEngine.Object.Instantiate(asset);
            PlayerData p = data.player;
            if (p.walkSpeed > 0f) c.walkSpeed = p.walkSpeed;
            if (p.sprintMultiplier > 0f) c.sprintMultiplier = p.sprintMultiplier;
            if (p.jumpHeight > 0f) c.jumpHeight = p.jumpHeight;
            if (p.gravity < 0f) c.gravity = p.gravity; // gravity is negative
            if (p.dashSpeed > 0f) c.dashSpeed = p.dashSpeed;
            if (p.dashCooldown > 0f) c.dashCooldown = p.dashCooldown;
            if (p.maxStamina > 0f) c.maxStamina = p.maxStamina;
            if (p.maxHealth > 0f) c.maxHealth = p.maxHealth;
            return c;
        }

        /// <summary>Hex colour from the JSON (e.g. "#4FB45A"), or <paramref name="fallback"/>.</summary>
        public static Color ParseColor(string hex, Color fallback) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color c) ? c : fallback;
    }
}
