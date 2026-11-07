using UnityEngine;

namespace CoinRush.Data
{
    /// <summary>
    /// Global rules shared by every world (combo, bonuses, world order).
    /// Per-world tuning lives in <see cref="WorldConfig"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Coin Rush/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("World Order")]
        [Tooltip("Worlds in play order. Entering the portal of world N loads world N+1.")]
        public WorldConfig[] worlds;

        [Header("Combo")]
        [Tooltip("Seconds between pickups to keep the combo alive.")]
        [Range(0.5f, 5f)] public float comboWindow = 2.2f;
        [Tooltip("Pickups needed per +1 multiplier step.")]
        [Min(1)] public int pickupsPerMultiplierStep = 5;
        [Range(1, 10)] public int maxMultiplier = 4;

        [Header("Bonuses")]
        [Tooltip("Points per second left on the clock when entering the portal.")]
        [Min(0f)] public float timeBonusPerSecond = 0.5f;

        [Header("Spawning")]
        [Range(0, 5)] public int countdownSeconds = 3;
        [Tooltip("Enemies never spawn closer than this to the player.")]
        [Min(0f)] public float minEnemySpawnDistance = 18f;

        public int WorldCount => worlds != null ? worlds.Length : 0;

        public WorldConfig GetWorld(int index) =>
            worlds != null && index >= 0 && index < worlds.Length ? worlds[index] : null;
    }
}
