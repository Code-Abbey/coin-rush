using UnityEngine;

namespace CoinRush.Core
{
    /// <summary>
    /// Data that survives scene loads during one playthrough (World 1 -> 2 -> 3).
    /// Static on purpose: it holds plain data only (no scene references), so it can't leak
    /// destroyed objects. Reset with <see cref="StartNewRun"/> from the main menu.
    /// </summary>
    public static class RunSession
    {
        public static bool IsActive { get; private set; }
        public static int WorldIndex { get; private set; }
        public static int TotalScore { get; private set; }
        public static int TotalCoins { get; private set; }
        public static int BestCombo { get; private set; }
        public static float TotalTime { get; private set; }
        /// <summary>Health carried into the next world (-1 = start at full).</summary>
        public static float CarriedHealth { get; private set; } = -1f;

        // Score at the moment the current world started (used to restart a world fairly).
        private static int scoreAtWorldStart;
        private static int coinsAtWorldStart;

        public static void StartNewRun()
        {
            IsActive = true;
            WorldIndex = 0;
            TotalScore = TotalCoins = BestCombo = 0;
            TotalTime = 0f;
            CarriedHealth = -1f;
            scoreAtWorldStart = coinsAtWorldStart = 0;
        }

        /// <summary>Called by GameManager when a world scene starts.</summary>
        public static void BeginWorld(int worldIndex)
        {
            if (!IsActive) StartNewRun(); // allows pressing Play directly in any world scene
            WorldIndex = worldIndex;
            scoreAtWorldStart = TotalScore;
            coinsAtWorldStart = TotalCoins;
        }

        /// <summary>Restarting a world rolls back what was earned inside it.</summary>
        public static void RollbackWorld()
        {
            TotalScore = scoreAtWorldStart;
            TotalCoins = coinsAtWorldStart;
        }

        public static void AddScore(int points, int coins)
        {
            TotalScore += points;
            TotalCoins += coins;
        }

        public static void RegisterCombo(int combo) => BestCombo = Mathf.Max(BestCombo, combo);

        public static void CompleteWorld(float timeSpent, float healthLeft, int bonus)
        {
            TotalTime += timeSpent;
            TotalScore += bonus;
            CarriedHealth = healthLeft;
            scoreAtWorldStart = TotalScore;
            coinsAtWorldStart = TotalCoins;
        }

        public static void EndRun() => IsActive = false;
    }
}
