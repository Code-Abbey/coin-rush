using UnityEngine;

namespace CoinRush.Data
{
    /// <summary>
    /// Everything that makes one world different: goal, timer, difficulty, spawning and look.
    /// One asset per world (Meadow / Desert / Frozen). Assigned to the GameManager in each world scene.
    /// </summary>
    [CreateAssetMenu(fileName = "World_", menuName = "Coin Rush/World Config", order = 1)]
    public class WorldConfig : ScriptableObject
    {
        [Header("Identity")]
        public string worldName = "Meadow";
        public string subtitle = "Where it all begins";
        [Tooltip("Exact scene name (must be in Build Settings).")]
        public string sceneName = "World1_Meadow";

        [Header("Goal")]
        [Tooltip("Coins (points) collected in this world needed to open the portal.")]
        [Min(1)] public int coinQuota = 30;
        [Tooltip("Seconds available to reach the portal.")]
        [Min(10f)] public float timeLimit = 150f;

        [Header("Coins")]
        [Range(1, 60)] public int maxActiveCoins = 22;
        [Range(0f, 1f)] public float gemChance = 0.15f;

        [Header("Enemies")]
        [Range(0, 15)] public int startingEnemies = 3;
        [Range(1, 25)] public int maxEnemies = 8;
        [Tooltip("A new enemy joins each time world score passes a multiple of this.")]
        [Min(1)] public int scorePerNewEnemy = 10;
        [Range(0f, 0.5f)] public float speedIncreasePerLevel = 0.06f;
        [Tooltip("Chance a spawned enemy is the heavy 'Brute' variant (if the scene has one).")]
        [Range(0f, 1f)] public float bruteChance = 0f;

        [Header("Power-ups")]
        [Min(1f)] public float powerUpSpawnInterval = 10f;
        [Range(1, 6)] public int maxPowerUpsOnField = 3;

        [Header("Look")]
        public Color themeColor = new Color(0.4f, 0.8f, 0.4f);
        public Color skyTop = new Color(0.25f, 0.5f, 0.9f);
        public Color skyHorizon = new Color(0.75f, 0.88f, 1f);
        public Color fogColor = new Color(0.72f, 0.82f, 0.92f);
        [Tooltip("Music playback pitch, gives each world a slightly different feel.")]
        [Range(0.5f, 1.5f)] public float musicPitch = 1f;
    }
}
