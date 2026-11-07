using CoinRush.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>Options panel (used in the main menu and the pause menu). Values persist via SettingsService.</summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text musicValue;
        [SerializeField] private TMP_Text sfxValue;
        [SerializeField] private TMP_Text sensitivityValue;
        [SerializeField] private Button backButton;
        [Tooltip("Panel to return to when Back is pressed.")]
        [SerializeField] private GameObject returnTo;

        private void Awake()
        {
            Setup(musicSlider, 0f, 1f, SettingsService.MusicVolume, v => { SettingsService.MusicVolume = v; Refresh(); });
            Setup(sfxSlider, 0f, 1f, SettingsService.SfxVolume, v => { SettingsService.SfxVolume = v; Refresh(); });
            Setup(sensitivitySlider, 0.2f, 3f, SettingsService.MouseSensitivity, v => { SettingsService.MouseSensitivity = v; Refresh(); });
            if (backButton != null) backButton.onClick.AddListener(Close);
        }

        private void OnEnable() => Refresh();

        private static void Setup(Slider slider, float min, float max, float value, UnityEngine.Events.UnityAction<float> onChange)
        {
            if (slider == null) return;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(onChange);
        }

        private void Refresh()
        {
            if (musicValue != null) musicValue.text = $"{SettingsService.MusicVolume * 100f:0}%";
            if (sfxValue != null) sfxValue.text = $"{SettingsService.SfxVolume * 100f:0}%";
            if (sensitivityValue != null) sensitivityValue.text = $"{SettingsService.MouseSensitivity:0.0}x";
        }

        public void Open()
        {
            if (returnTo != null) returnTo.SetActive(false);
            StartCoroutineSafe(gameObject);
        }

        private static void StartCoroutineSafe(GameObject panel) => MonoBehaviourRunner.Run(UITween.ShowPanel(panel));

        public void Close()
        {
            SettingsService.Save();
            gameObject.SetActive(false);
            if (returnTo != null) MonoBehaviourRunner.Run(UITween.ShowPanel(returnTo));
        }
    }
}
