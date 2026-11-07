using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CoinRush.Core
{
    /// <summary>One finished run (won or lost), stored in the JSON save file.</summary>
    [Serializable]
    public class RunRecord
    {
        public string date;       // ISO 8601, local time
        public string hero;
        public bool won;
        public int worldsCleared;
        public int score;
        public int coins;
        public int bestCombo;
        public float time;        // seconds
    }

    /// <summary>Everything the game saves between sessions.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int highScore;
        public float bestWinTime = -1f;
        public List<RunRecord> runs = new List<RunRecord>();
    }

    /// <summary>
    /// Persists the best score, fastest win and a history of runs as JSON
    /// (<c>Application.persistentDataPath/coinrush_save.json</c>). Older PlayerPrefs records are
    /// migrated automatically the first time. Statistics are computed from the history with LINQ.
    /// </summary>
    public static class HighScoreService
    {
        private const string FileName = "coinrush_save.json";
        private const int MaxStoredRuns = 50;
        private const string LegacyHighScoreKey = "CoinRush.HighScore";
        private const string LegacyBestTimeKey = "CoinRush.BestWinTime";

        private static SaveData data;

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        private static SaveData Data => data ??= LoadOrMigrate();

        public static int HighScore => Data.highScore;

        /// <summary>Fastest win in seconds, or -1 if the player never won.</summary>
        public static float BestWinTime => Data.bestWinTime;

        public static IReadOnlyList<RunRecord> Runs => Data.runs;

        /// <returns>True if this is a new high score.</returns>
        public static bool TrySubmitScore(int score)
        {
            if (score <= HighScore) return false;
            Data.highScore = score;
            Save();
            return true;
        }

        /// <returns>True if this is a new best (fastest) win time.</returns>
        public static bool TrySubmitWinTime(float seconds)
        {
            float best = BestWinTime;
            if (best >= 0f && seconds >= best) return false;
            Data.bestWinTime = seconds;
            Save();
            return true;
        }

        /// <summary>Adds the finished run (score, coins and combo come from <see cref="RunSession"/>).</summary>
        public static void RecordRun(bool won, int worldsCleared, float seconds, string hero)
        {
            Data.runs.Add(new RunRecord
            {
                date = DateTime.Now.ToString("s"),
                hero = hero,
                won = won,
                worldsCleared = worldsCleared,
                score = RunSession.TotalScore,
                coins = RunSession.TotalCoins,
                bestCombo = RunSession.BestCombo,
                time = Mathf.Round(seconds * 10f) / 10f,
            });
            // Keep the file small: the best runs plus the most recent ones.
            if (Data.runs.Count > MaxStoredRuns)
            {
                var keep = new HashSet<RunRecord>(TopRuns(MaxStoredRuns / 2).Union(Data.runs.Skip(Data.runs.Count - MaxStoredRuns / 2)));
                Data.runs = Data.runs.Where(r => keep.Contains(r)).ToList(); // keeps chronological order
            }
            Save();
            Debug.Log($"[Coin Rush] Run saved: {RunSession.TotalScore} pts, rank #{RankOf(RunSession.TotalScore)} of {Data.runs.Count}, " +
                      $"{Wins} wins, average {AverageScore:F0} pts, favourite hero {FavouriteHero ?? "-"}  ({SavePath})");
        }

        // ---------------------------------------------------------------- LINQ statistics

        /// <summary>Best runs first (ties: the faster run wins).</summary>
        public static IEnumerable<RunRecord> TopRuns(int count) =>
            Data.runs.OrderByDescending(r => r.score).ThenBy(r => r.time).Take(count);

        /// <summary>1-based leaderboard position a score would have.</summary>
        public static int RankOf(int score) => Data.runs.Count(r => r.score > score) + 1;

        public static int Wins => Data.runs.Count(r => r.won);

        public static float AverageScore => Data.runs.Any() ? (float)Data.runs.Average(r => r.score) : 0f;

        /// <summary>The hero with the most wins (null if nobody has won yet).</summary>
        public static string FavouriteHero =>
            Data.runs.Where(r => r.won)
                     .GroupBy(r => r.hero)
                     .OrderByDescending(g => g.Count())
                     .Select(g => g.Key)
                     .FirstOrDefault();

        // ---------------------------------------------------------------- file I/O

        private static SaveData LoadOrMigrate()
        {
            try
            {
                string json = ReadSaveText();
                if (!string.IsNullOrEmpty(json))
                {
                    SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                    if (loaded != null)
                    {
                        loaded.runs ??= new List<RunRecord>();
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Coin Rush] Save file unreadable ({e.Message}); starting fresh.");
            }

            // First launch with the JSON save: bring over the old PlayerPrefs records.
            return new SaveData
            {
                highScore = PlayerPrefs.GetInt(LegacyHighScoreKey, 0),
                bestWinTime = PlayerPrefs.GetFloat(LegacyBestTimeKey, -1f),
            };
        }

        // In a browser the save JSON lives in the browser's own storage (via PlayerPrefs, which Unity keeps
        // in IndexedDB); everywhere else it is a normal file.
#if UNITY_WEBGL && !UNITY_EDITOR
        private const string WebSaveKey = "CoinRush.SaveJson";
        private static string ReadSaveText() => PlayerPrefs.GetString(WebSaveKey, string.Empty);
        private static void WriteSaveText(string json) { PlayerPrefs.SetString(WebSaveKey, json); PlayerPrefs.Save(); }
#else
        private static string ReadSaveText() => File.Exists(SavePath) ? File.ReadAllText(SavePath) : null;
        private static void WriteSaveText(string json) => File.WriteAllText(SavePath, json);
#endif

        private static void Save()
        {
            try { WriteSaveText(JsonUtility.ToJson(Data, true)); }
            catch (Exception e) { Debug.LogWarning($"[Coin Rush] Could not write save file: {e.Message}"); }
        }
    }
}
