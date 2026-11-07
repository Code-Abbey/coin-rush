using CoinRush.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>Red screen-edge flash when hit, plus a slow pulse while health is low.</summary>
    [RequireComponent(typeof(Image))]
    public class DamageVignette : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float hitAlpha = 0.65f;
        [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.3f;
        [SerializeField] private float fadeSpeed = 2.5f;

        private Image image;
        private float flash;
        private float health01 = 1f;

        private void Awake()
        {
            image = GetComponent<Image>();
            image.raycastTarget = false;
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            GameEvents.PlayerDamaged += OnDamaged;
            GameEvents.HealthChanged += OnHealth;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDamaged -= OnDamaged;
            GameEvents.HealthChanged -= OnHealth;
        }

        private void OnDamaged(float amount) => flash = hitAlpha;
        private void OnHealth(float current, float max) => health01 = max > 0f ? current / max : 0f;

        private void Update()
        {
            flash = Mathf.MoveTowards(flash, 0f, fadeSpeed * Time.unscaledDeltaTime);
            float pulse = health01 < lowHealthThreshold && health01 > 0f
                ? (0.15f + Mathf.PingPong(Time.unscaledTime * 0.8f, 0.2f)) * (1f - health01 / lowHealthThreshold)
                : 0f;
            SetAlpha(Mathf.Max(flash, pulse));
        }

        private void SetAlpha(float a)
        {
            Color c = image.color;
            c.a = a;
            image.color = c;
        }
    }
}
