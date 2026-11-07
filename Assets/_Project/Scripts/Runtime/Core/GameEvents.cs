using System;
using CoinRush.Data;
using UnityEngine;

namespace CoinRush.Core
{
    public enum PowerUpType { SpeedBoost, Magnet, Shield }

    /// <summary>Results shown on the "World Complete" screen.</summary>
    public readonly struct WorldSummary
    {
        public readonly string WorldName;
        public readonly int WorldPoints;
        public readonly int TimeBonus;
        public readonly float TimeSpent;
        public readonly bool IsFinalWorld;

        public WorldSummary(string worldName, int worldPoints, int timeBonus, float timeSpent, bool isFinalWorld)
        {
            WorldName = worldName;
            WorldPoints = worldPoints;
            TimeBonus = timeBonus;
            TimeSpent = timeSpent;
            IsFinalWorld = isFinalWorld;
        }
    }

    /// <summary>
    /// Static event bus. Gameplay raises events, UI / audio / VFX listen.
    /// No system needs a direct reference to another (loose coupling).
    /// Listeners subscribe in OnEnable and unsubscribe in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        // ---- Round flow ----
        public static event Action<GameState> StateChanged;
        public static event Action<int> CountdownTick;
        public static event Action<float> TimeChanged;
        /// <summary>(world, index, worldCount)</summary>
        public static event Action<WorldConfig, int, int> WorldStarted;
        public static event Action<WorldSummary> WorldCompleted;
        public static event Action<Vector3> PortalOpened;
        public static event Action<int> DifficultyIncreased;

        // ---- Score ----
        /// <summary>Total run score.</summary>
        public static event Action<int> ScoreChanged;
        /// <summary>(points in this world, quota)</summary>
        public static event Action<int, int> QuotaProgress;
        /// <summary>(combo count, multiplier)</summary>
        public static event Action<int, int> ComboChanged;
        /// <summary>(world position, points, colour) – floating "+N" popups.</summary>
        public static event Action<Vector3, int, Color> PointsAwarded;

        // ---- Player ----
        public static event Action<float, float> HealthChanged;
        public static event Action<float> PlayerDamaged;
        public static event Action<float, float> StaminaChanged;
        /// <summary>0 = just used, 1 = ready.</summary>
        public static event Action<float> DashCharge;
        /// <summary>(type, seconds remaining, total duration). Remaining 0 = ended.</summary>
        public static event Action<PowerUpType, float, float> PowerUpChanged;
        public static event Action PlayerDied;

        // ---- UI ----
        public static event Action<string, Color> Notification;

        public static void RaiseStateChanged(GameState s) => StateChanged?.Invoke(s);
        public static void RaiseCountdownTick(int s) => CountdownTick?.Invoke(s);
        public static void RaiseTimeChanged(float t) => TimeChanged?.Invoke(t);
        public static void RaiseWorldStarted(WorldConfig w, int i, int count) => WorldStarted?.Invoke(w, i, count);
        public static void RaiseWorldCompleted(WorldSummary s) => WorldCompleted?.Invoke(s);
        public static void RaisePortalOpened(Vector3 p) => PortalOpened?.Invoke(p);
        public static void RaiseDifficultyIncreased(int l) => DifficultyIncreased?.Invoke(l);
        public static void RaiseScoreChanged(int s) => ScoreChanged?.Invoke(s);
        public static void RaiseQuotaProgress(int c, int q) => QuotaProgress?.Invoke(c, q);
        public static void RaiseComboChanged(int c, int m) => ComboChanged?.Invoke(c, m);
        public static void RaisePointsAwarded(Vector3 p, int pts, Color c) => PointsAwarded?.Invoke(p, pts, c);
        public static void RaiseHealthChanged(float c, float m) => HealthChanged?.Invoke(c, m);
        public static void RaisePlayerDamaged(float a) => PlayerDamaged?.Invoke(a);
        public static void RaiseStaminaChanged(float c, float m) => StaminaChanged?.Invoke(c, m);
        public static void RaiseDashCharge(float t) => DashCharge?.Invoke(t);
        public static void RaisePowerUpChanged(PowerUpType t, float r, float d) => PowerUpChanged?.Invoke(t, r, d);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseNotification(string m, Color c) => Notification?.Invoke(m, c);
    }
}
