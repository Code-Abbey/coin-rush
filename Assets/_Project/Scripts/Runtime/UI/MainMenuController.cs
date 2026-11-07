using System.Collections;
using CoinRush.Audio;
using CoinRush.Core;
using CoinRush.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>Main menu: start a new run, how-to-play and settings panels, records, quit.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private MenuPanel controlsPanel;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private CharacterSelectScreen characterScreen;
        [SerializeField] private MenuPanel creditsPanel;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button charactersButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [Tooltip("Optional: replays the intro story.")]
        [SerializeField] private Button storyButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text recordsText;
        [SerializeField] private RectTransform titleTransform;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            playButton.onClick.AddListener(StartRun);
            charactersButton.onClick.AddListener(() => characterScreen.Open());
            controlsButton.onClick.AddListener(() => controlsPanel.Open());
            settingsButton.onClick.AddListener(() => settingsPanel.Open());
            if (creditsButton != null) creditsButton.onClick.AddListener(() => creditsPanel.Open());
            quitButton.onClick.AddListener(Quit);
#if UNITY_WEBGL && !UNITY_EDITOR
            quitButton.gameObject.SetActive(false); // a browser game can't quit itself; just close the tab
#endif
            if (storyButton != null) storyButton.onClick.AddListener(() => SceneNames.Load(SceneNames.Story));
        }

        private void Start()
        {
            controlsPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
            characterScreen.gameObject.SetActive(false);
            if (creditsPanel != null) creditsPanel.gameObject.SetActive(false);

            float best = HighScoreService.BestWinTime;
            recordsText.text = $"BEST SCORE  <color=#FFD54A>{HighScoreService.HighScore:N0}</color>" +
                               (best >= 0f ? $"      FASTEST ESCAPE  <color=#6EC8FF>{best:0.0}s</color>" : string.Empty);
            StartCoroutine(TitleBob());
        }

        private IEnumerator TitleBob()
        {
            if (titleTransform == null) yield break;
            Vector2 start = titleTransform.anchoredPosition;
            while (true)
            {
                float t = Time.unscaledTime;
                titleTransform.anchoredPosition = start + Vector2.up * Mathf.Sin(t * 1.6f) * 10f;
                titleTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * 1.5f);
                yield return null;
            }
        }

        private void StartRun()
        {
            AudioManager.Play(SoundId.UIConfirm);
            RunSession.StartNewRun();
            WorldConfig first = gameConfig != null ? gameConfig.GetWorld(0) : null;
            SceneNames.Load(first != null ? first.sceneName : SceneNames.World1);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
