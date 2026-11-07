using System.Collections;
using CoinRush.Core;
using CoinRush.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>
    /// In-game heads-up display. Purely reactive: listens to <see cref="GameEvents"/>, never polls gameplay
    /// (except the combo timer ring, read from GameManager each frame for smoothness).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [System.Serializable]
        public class PowerUpSlot
        {
            public PowerUpType type;
            public GameObject root;
            public Image radialFill;
        }

        [Header("World Card (top-left)")]
        [SerializeField] private TMP_Text worldNameText;
        [SerializeField] private TMP_Text quotaText;
        [SerializeField] private Image quotaFill;
        [SerializeField] private TMP_Text levelText;

        [Header("Score / Timer (top-centre)")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Image timerPill;

        [Header("Combo")]
        [SerializeField] private GameObject comboRoot;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private Image comboTimerFill;

        [Header("Vitals (bottom-left)")]
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Image staminaFill;

        [Header("Abilities (bottom-centre)")]
        [SerializeField] private Image dashFill;
        [SerializeField] private TMP_Text dashLabel;
        [SerializeField] private PowerUpSlot[] powerUpSlots;

        [Header("Hero (bottom-left)")]
        [SerializeField] private CharacterDatabase heroDatabase;
        [SerializeField] private Image heroPortrait;
        [SerializeField] private TMP_Text heroNameText;

        [Header("Colours")]
        [SerializeField] private Color healthFull = new Color(0.35f, 0.95f, 0.45f);
        [SerializeField] private Color healthLow = new Color(1f, 0.25f, 0.3f);
        [SerializeField] private Color timerNormal = new Color(0.1f, 0.1f, 0.16f, 0.85f);
        [SerializeField] private Color timerWarning = new Color(0.85f, 0.15f, 0.2f, 0.9f);
        [SerializeField, Min(0f)] private float warningThreshold = 20f;

        private Coroutine scorePop, comboPop;
        private int displayedScore;
        private int targetScore;

        private void OnEnable()
        {
            GameEvents.WorldStarted += OnWorldStarted;
            GameEvents.ScoreChanged += OnScoreChanged;
            GameEvents.QuotaProgress += OnQuotaProgress;
            GameEvents.TimeChanged += OnTimeChanged;
            GameEvents.HealthChanged += OnHealthChanged;
            GameEvents.StaminaChanged += OnStaminaChanged;
            GameEvents.DashCharge += OnDashCharge;
            GameEvents.PowerUpChanged += OnPowerUpChanged;
            GameEvents.ComboChanged += OnComboChanged;
            GameEvents.DifficultyIncreased += OnDifficultyIncreased;
            GameEvents.PortalOpened += OnPortalOpened;
        }

        private void OnDisable()
        {
            GameEvents.WorldStarted -= OnWorldStarted;
            GameEvents.ScoreChanged -= OnScoreChanged;
            GameEvents.QuotaProgress -= OnQuotaProgress;
            GameEvents.TimeChanged -= OnTimeChanged;
            GameEvents.HealthChanged -= OnHealthChanged;
            GameEvents.StaminaChanged -= OnStaminaChanged;
            GameEvents.DashCharge -= OnDashCharge;
            GameEvents.PowerUpChanged -= OnPowerUpChanged;
            GameEvents.ComboChanged -= OnComboChanged;
            GameEvents.DifficultyIncreased -= OnDifficultyIncreased;
            GameEvents.PortalOpened -= OnPortalOpened;
        }

        private void Start()
        {
            if (comboRoot != null) comboRoot.SetActive(false);
            CharacterDefinition hero = heroDatabase != null ? heroDatabase.Get(SelectedCharacter.Index) : null;
            if (hero != null)
            {
                if (heroPortrait != null && hero.portrait != null) heroPortrait.sprite = hero.portrait;
                if (heroNameText != null) { heroNameText.text = hero.displayName.ToUpper(); heroNameText.color = hero.accentColor; }
            }
            if (powerUpSlots != null)
                foreach (PowerUpSlot slot in powerUpSlots)
                    if (slot.root != null) slot.root.SetActive(false);
        }

        private void Update()
        {
            // Score counts up smoothly instead of jumping.
            if (displayedScore != targetScore && scoreText != null)
            {
                int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(targetScore - displayedScore) * 10f * Time.unscaledDeltaTime));
                displayedScore = (int)Mathf.MoveTowards(displayedScore, targetScore, step);
                scoreText.text = displayedScore.ToString("N0");
            }

            if (comboTimerFill != null && GameManager.Instance != null)
                comboTimerFill.fillAmount = GameManager.Instance.ComboTimeLeft01;
        }

        // ------------------------------------------------------------------ handlers

        private void OnWorldStarted(WorldConfig world, int index, int count)
        {
            if (worldNameText != null) worldNameText.text = $"WORLD {index + 1}/{count}  <color=#{ColorUtility.ToHtmlStringRGB(world.themeColor)}>{world.worldName.ToUpper()}</color>";
            if (quotaFill != null) quotaFill.color = world.themeColor;
        }

        private void OnScoreChanged(int score)
        {
            targetScore = score;
            if (score == 0) { displayedScore = 0; if (scoreText != null) scoreText.text = "0"; }
            else Pop(scoreText != null ? scoreText.transform : null, ref scorePop);
        }

        private void OnQuotaProgress(int current, int quota)
        {
            bool done = current >= quota;
            if (quotaText != null)
                quotaText.text = done ? "<color=#43D9BD>PORTAL OPEN</color>" : $"COINS  {current} / {quota}";
            if (quotaFill != null) quotaFill.fillAmount = Mathf.Clamp01(quota > 0 ? (float)current / quota : 0f);
        }

        private void OnPortalOpened(Vector3 _)
        {
            if (quotaFill != null) quotaFill.color = new Color(0.76f, 0.55f, 1f);
        }

        private void OnTimeChanged(float seconds)
        {
            if (timerText == null) return;
            int s = Mathf.CeilToInt(seconds);
            timerText.text = $"{s / 60}:{s % 60:00}";
            bool warning = seconds <= warningThreshold;
            if (timerPill != null) timerPill.color = warning ? Color.Lerp(timerNormal, timerWarning, Mathf.PingPong(Time.time * 2f, 1f)) : timerNormal;
        }

        private void OnHealthChanged(float current, float max)
        {
            float t = max > 0f ? current / max : 0f;
            if (healthFill != null)
            {
                healthFill.fillAmount = t;
                healthFill.color = Color.Lerp(healthLow, healthFull, t);
            }
            if (healthText != null) healthText.text = Mathf.CeilToInt(current).ToString();
        }

        private void OnStaminaChanged(float current, float max)
        {
            if (staminaFill != null) staminaFill.fillAmount = max > 0f ? current / max : 0f;
        }

        private void OnDashCharge(float charge)
        {
            if (dashFill != null) dashFill.fillAmount = charge;
            if (dashLabel != null) dashLabel.color = charge >= 1f ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }

        private void OnPowerUpChanged(PowerUpType type, float remaining, float duration)
        {
            if (powerUpSlots == null) return;
            foreach (PowerUpSlot slot in powerUpSlots)
            {
                if (slot.type != type || slot.root == null) continue;
                bool active = remaining > 0f;
                if (active && !slot.root.activeSelf) { slot.root.SetActive(true); StartCoroutine(UITween.Pop(slot.root.transform)); }
                else if (!active) slot.root.SetActive(false);
                if (slot.radialFill != null) slot.radialFill.fillAmount = duration > 0f ? remaining / duration : 0f;
            }
        }

        private void OnComboChanged(int combo, int multiplier)
        {
            if (comboRoot == null) return;
            bool show = combo >= 2;
            comboRoot.SetActive(show);
            if (!show) return;
            if (comboText != null) comboText.text = multiplier > 1 ? $"x{multiplier}  <size=60%>COMBO {combo}</size>" : $"COMBO {combo}";
            Pop(comboRoot.transform, ref comboPop);
        }

        private void OnDifficultyIncreased(int level)
        {
            if (levelText != null) levelText.text = $"THREAT LV {level + 1}";
        }

        private void Pop(Transform t, ref Coroutine handle)
        {
            if (t == null || !isActiveAndEnabled) return;
            if (handle != null) StopCoroutine(handle);
            handle = StartCoroutine(UITween.Pop(t, 0.25f, 1.25f));
        }
    }
}
