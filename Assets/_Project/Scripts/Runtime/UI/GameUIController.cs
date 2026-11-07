using CoinRush.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>
    /// Switches between HUD, countdown, pause, settings, world-complete and end-of-run panels based on the
    /// game state, and wires every menu button to the GameManager.
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private MenuPanel controlsPanel;
        [SerializeField] private GameObject worldCompletePanel;
        [SerializeField] private GameObject endPanel;

        [Header("Countdown")]
        [SerializeField] private TMP_Text countdownText;

        [Header("World Complete")]
        [SerializeField] private TMP_Text completeTitleText;
        [SerializeField] private TMP_Text completeStatsText;

        [Header("End Panel")]
        [SerializeField] private ResultsScreen results;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button pauseControlsButton;
        [SerializeField] private Button pauseMenuButton;
        [SerializeField] private Button endRetryWorldButton;
        [SerializeField] private Button endNewRunButton;
        [SerializeField] private Button endMenuButton;

        private float countdownHideTime;

        private void Awake()
        {
            Wire(resumeButton, () => GameManager.Instance.TogglePause());
            Wire(pauseRestartButton, () => GameManager.Instance.RestartWorld());
            Wire(pauseSettingsButton, () => settingsPanel.Open());
            Wire(pauseControlsButton, () => controlsPanel.Open());
            Wire(pauseMenuButton, () => GameManager.Instance.GoToMainMenu());
            Wire(endRetryWorldButton, () => GameManager.Instance.RestartWorld());
            Wire(endNewRunButton, () => GameManager.Instance.RestartRun());
            Wire(endMenuButton, () => GameManager.Instance.GoToMainMenu());
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.CountdownTick += OnCountdownTick;
            GameEvents.WorldCompleted += OnWorldCompleted;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.CountdownTick -= OnCountdownTick;
            GameEvents.WorldCompleted -= OnWorldCompleted;
        }

        private void Start()
        {
            SetActive(pausePanel, false);
            SetActive(worldCompletePanel, false);
            SetActive(endPanel, false);
            if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
            if (controlsPanel != null) controlsPanel.gameObject.SetActive(false);
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            SetActive(hudPanel, true);
        }

        private void Update()
        {
            if (countdownText != null && countdownText.gameObject.activeSelf && Time.unscaledTime > countdownHideTime)
                countdownText.gameObject.SetActive(false);
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action); // click sound comes from UIButtonSounds
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private static void Show(GameObject panel)
        {
            if (panel != null && !panel.activeSelf) MonoBehaviourRunner.Run(UITween.ShowPanel(panel));
        }

        private void OnCountdownTick(int secondsLeft)
        {
            if (countdownText == null) return;
            countdownText.gameObject.SetActive(true);
            countdownText.text = secondsLeft > 0 ? secondsLeft.ToString() : "GO!";
            countdownText.color = secondsLeft > 0 ? Color.white : new Color(1f, 0.84f, 0.25f);
            StartCoroutine(UITween.Pop(countdownText.transform, 0.4f, 1.8f));
            countdownHideTime = Time.unscaledTime + (secondsLeft > 0 ? 1.1f : 0.8f);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Paused) Show(pausePanel);
            else
            {
                SetActive(pausePanel, false);
                if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
                if (controlsPanel != null) controlsPanel.gameObject.SetActive(false);
            }

            bool ended = state == GameState.Won || state == GameState.Lost;
            SetActive(hudPanel, state != GameState.WorldComplete && !ended);

            if (state == GameState.Lost) ShowEnd(won: false);
        }

        private void OnWorldCompleted(WorldSummary summary)
        {
            if (summary.IsFinalWorld)
            {
                ShowEnd(won: true);
                return;
            }

            completeTitleText.text = $"{summary.WorldName.ToUpper()} CLEARED!";
            completeStatsText.text =
                $"World points   <color=#FFD54A>{summary.WorldPoints}</color>\n" +
                $"Time bonus     <color=#6EC8FF>+{summary.TimeBonus}</color>\n" +
                $"Run score      <color=#FFFFFF>{RunSession.TotalScore:N0}</color>\n\n" +
                "<size=70%><color=#9AA3B8>Next world loading...</color></size>";
            Show(worldCompletePanel);
        }

        private void ShowEnd(bool won)
        {
            GameManager gm = GameManager.Instance;
            string reason = gm.TimeRemaining <= 0f ? "TIME'S UP!" : "KNOCKED OUT!";
            int cleared = won ? 3 : gm.WorldIndex;
            float runTime = RunSession.TotalTime + (won ? 0f : gm.World.timeLimit - gm.TimeRemaining);
            Show(endPanel);
            results.Show(won, reason, cleared, gm.World.worldName, gm.IsNewHighScore, runTime);
        }
    }
}
