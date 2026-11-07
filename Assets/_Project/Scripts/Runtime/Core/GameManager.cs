using System.Collections;
using CoinRush.Audio;
using CoinRush.Data;
using CoinRush.InputHandling;
using UnityEngine;

namespace CoinRush.Core
{
    /// <summary>
    /// Runs one world: countdown, timer, quota/portal, combo scoring, difficulty, pause,
    /// world completion and game over. One per world scene; run-wide data lives in <see cref="RunSession"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private WorldConfig world;
        [Tooltip("Index of this world inside GameConfig.worlds.")]
        [SerializeField, Min(0)] private int worldIndex;

        [Header("Flow")]
        [Tooltip("Seconds the World Complete screen stays before loading the next world.")]
        [SerializeField, Min(0.5f)] private float worldCompleteDelay = 3.5f;

        public GameConfig Config => gameConfig;
        public WorldConfig World => world;
        public int WorldIndex => worldIndex;
        public bool IsFinalWorld => worldIndex >= gameConfig.WorldCount - 1;
        public GameState State { get; private set; } = GameState.Countdown;
        public bool IsPlaying => State == GameState.Playing;
        public float TimeRemaining { get; private set; }
        public int WorldPoints { get; private set; }
        public bool PortalOpen { get; private set; }
        public int DifficultyLevel { get; private set; }
        public int Combo { get; private set; }
        public int Multiplier { get; private set; } = 1;
        public bool IsNewHighScore { get; private set; }

        private float lastPickupTime = -999f;
        private Vector3 portalPosition;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (gameConfig == null) gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            if (world == null)
            {
                Debug.LogError("GameManager has no WorldConfig assigned.", this);
                world = ScriptableObject.CreateInstance<WorldConfig>();
            }

            // External tuning: StreamingAssets/Config/coinrush_config.json overrides the asset values
            // (applied to runtime copies, so the assets themselves never change).
            CoinRushConfigData json = JsonConfig.Load();
            gameConfig = JsonConfig.Apply(gameConfig, json);
            world = JsonConfig.Apply(world, json);
        }

        private void OnEnable() => GameEvents.PlayerDied += HandlePlayerDied;
        private void OnDisable() => GameEvents.PlayerDied -= HandlePlayerDied;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            RunSession.BeginWorld(worldIndex);
            TimeRemaining = world.timeLimit;

            GameEvents.RaiseWorldStarted(world, worldIndex, Mathf.Max(1, gameConfig.WorldCount));
            GameEvents.RaiseScoreChanged(RunSession.TotalScore);
            GameEvents.RaiseQuotaProgress(0, world.coinQuota);
            GameEvents.RaiseTimeChanged(TimeRemaining);
            GameEvents.RaiseComboChanged(0, 1);
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            SetState(GameState.Countdown);
            yield return new WaitForSeconds(0.6f); // let the fade-in finish
            for (int i = gameConfig.countdownSeconds; i > 0; i--)
            {
                GameEvents.RaiseCountdownTick(i);
                AudioManager.Play(SoundId.CountdownBeep);
                yield return new WaitForSeconds(1f);
            }
            GameEvents.RaiseCountdownTick(0);
            AudioManager.Play(SoundId.CountdownGo);
            SetState(GameState.Playing);
        }

        private void Update()
        {
            InputReader input = InputReader.Instance;
            if (input != null && input.PausePressed) TogglePause();

            if (State != GameState.Playing) return;

            UpdateCombo();

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                GameEvents.RaiseTimeChanged(0f);
                Lose();
                return;
            }
            GameEvents.RaiseTimeChanged(TimeRemaining);
        }

        // ------------------------------------------------------------------ scoring

        /// <summary>Called by coins. Applies the combo multiplier and returns the points awarded.</summary>
        public int CollectCoin(int baseValue, Vector3 position, Color color)
        {
            if (!IsPlaying || baseValue <= 0) return 0;

            Combo++;
            lastPickupTime = Time.time;
            Multiplier = Mathf.Min(gameConfig.maxMultiplier, 1 + (Combo - 1) / gameConfig.pickupsPerMultiplierStep);
            RunSession.RegisterCombo(Combo);
            GameEvents.RaiseComboChanged(Combo, Multiplier);

            int points = baseValue * Multiplier;
            WorldPoints += points;
            RunSession.AddScore(points, 1);

            GameEvents.RaisePointsAwarded(position, points, color);
            GameEvents.RaiseScoreChanged(RunSession.TotalScore);
            GameEvents.RaiseQuotaProgress(WorldPoints, world.coinQuota);

            int newLevel = WorldPoints / world.scorePerNewEnemy;
            while (DifficultyLevel < newLevel)
            {
                DifficultyLevel++;
                GameEvents.RaiseDifficultyIncreased(DifficultyLevel);
            }

            if (!PortalOpen && WorldPoints >= world.coinQuota) OpenPortal();
            return points;
        }

        private void UpdateCombo()
        {
            if (Combo > 0 && Time.time - lastPickupTime > gameConfig.comboWindow)
            {
                Combo = 0;
                Multiplier = 1;
                GameEvents.RaiseComboChanged(0, 1);
            }
        }

        /// <summary>Remaining combo time 0..1 (for the UI timer ring).</summary>
        public float ComboTimeLeft01 =>
            Combo == 0 ? 0f : Mathf.Clamp01(1f - (Time.time - lastPickupTime) / gameConfig.comboWindow);

        // ------------------------------------------------------------------ portal

        /// <summary>Portals register themselves so the HUD compass knows where to point.</summary>
        public void RegisterPortal(Vector3 position) => portalPosition = position;

        private void OpenPortal()
        {
            PortalOpen = true;
            AudioManager.Play(SoundId.PortalOpen);
            string message = IsFinalWorld ? "THE FINAL PORTAL IS OPEN!" : "PORTAL OPEN! FIND IT!";
            GameEvents.RaiseNotification(message, new Color(0.75f, 0.45f, 1f));
            GameEvents.RaisePortalOpened(portalPosition);
        }

        /// <summary>Called by the Portal when the player steps in.</summary>
        public void EnterPortal(float playerHealth)
        {
            if (!IsPlaying || !PortalOpen) return;

            int bonus = Mathf.RoundToInt(TimeRemaining * gameConfig.timeBonusPerSecond);
            float timeSpent = world.timeLimit - TimeRemaining;
            RunSession.CompleteWorld(timeSpent, playerHealth, bonus);
            GameEvents.RaiseScoreChanged(RunSession.TotalScore);

            var summary = new WorldSummary(world.worldName, WorldPoints, bonus, timeSpent, IsFinalWorld);
            AudioManager.Play(IsFinalWorld ? SoundId.Win : SoundId.PortalEnter);

            if (IsFinalWorld)
            {
                IsNewHighScore = HighScoreService.TrySubmitScore(RunSession.TotalScore);
                HighScoreService.TrySubmitWinTime(RunSession.TotalTime);
                HighScoreService.RecordRun(true, worldIndex + 1, RunSession.TotalTime, HeroName());
                RunSession.EndRun();
                SetState(GameState.Won);
                GameEvents.RaiseWorldCompleted(summary);
            }
            else
            {
                SetState(GameState.WorldComplete);
                GameEvents.RaiseWorldCompleted(summary);
                StartCoroutine(LoadNextWorldRoutine());
            }
        }

        private IEnumerator LoadNextWorldRoutine()
        {
            yield return new WaitForSecondsRealtime(worldCompleteDelay);
            WorldConfig next = gameConfig.GetWorld(worldIndex + 1);
            SceneNames.Load(next != null ? next.sceneName : SceneNames.MainMenu);
        }

        // ------------------------------------------------------------------ flow

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void RestartWorld()
        {
            RunSession.RollbackWorld();
            SceneNames.ReloadCurrent();
        }

        public void RestartRun()
        {
            RunSession.StartNewRun();
            WorldConfig first = gameConfig.GetWorld(0);
            SceneNames.Load(first != null ? first.sceneName : SceneNames.World1);
        }

        public void GoToMainMenu()
        {
            RunSession.EndRun();
            SceneNames.Load(SceneNames.MainMenu);
        }

        private void HandlePlayerDied()
        {
            if (State == GameState.Playing) Lose();
        }

        private void Lose()
        {
            IsNewHighScore = HighScoreService.TrySubmitScore(RunSession.TotalScore);
            HighScoreService.RecordRun(false, worldIndex, RunSession.TotalTime + world.timeLimit - TimeRemaining, HeroName());
            AudioManager.Play(SoundId.Lose);
            SetState(GameState.Lost);
        }

        private static string HeroName()
        {
            var loader = FindFirstObjectByType<CoinRush.Player.PlayerCharacterLoader>();
            return loader != null && loader.Current != null ? loader.Current.displayName : "Hero";
        }

        private void SetState(GameState newState)
        {
            State = newState;
            Time.timeScale = newState == GameState.Paused ? 0f : 1f;

            bool lockCursor = newState == GameState.Playing || newState == GameState.Countdown;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;

            GameEvents.RaiseStateChanged(newState);
        }
    }
}
