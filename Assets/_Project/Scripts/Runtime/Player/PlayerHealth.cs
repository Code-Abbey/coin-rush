using System.Collections;
using CoinRush.Audio;
using CoinRush.Core;
using CoinRush.Utilities;
using UnityEngine;

namespace CoinRush.Player
{
    /// <summary>
    /// Player hit points: invulnerability frames (blinking), knockback, shield power-up absorption,
    /// dash i-frames, and health carried between worlds via <see cref="RunSession"/>.
    /// Implements <see cref="IDamageable"/> so enemies and hazards never depend on this class.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Tooltip("Renderers that blink while invulnerable.")]
        [SerializeField] private Renderer[] blinkRenderers;
        [SerializeField, Min(0.01f)] private float blinkInterval = 0.1f;

        private PlayerController controller;
        private PlayerPowerUps powerUps;
        private float health;
        private float invulnerableUntil;

        public float Health => health;
        public float MaxHealth => controller.Stats.maxHealth;
        public bool IsAlive => health > 0f;
        public bool IsFull => health >= MaxHealth;
        public bool IsInvulnerable => Time.time < invulnerableUntil || controller.IsDashing;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            powerUps = GetComponent<PlayerPowerUps>();
            if (blinkRenderers == null || blinkRenderers.Length == 0)
                blinkRenderers = GetComponentsInChildren<Renderer>();
        }

        private void Start()
        {
            // Carry health over from the previous world (at least 40% so a new world is fair).
            float carried = RunSession.CarriedHealth;
            health = carried > 0f ? Mathf.Max(carried, MaxHealth * 0.4f) : MaxHealth;
            GameEvents.RaiseHealthChanged(health, MaxHealth);
        }

        public void TakeDamage(float amount, Vector3 sourcePosition, float knockbackForce)
        {
            if (!IsAlive || IsInvulnerable || amount <= 0f) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            if (knockbackForce > 0f) controller.ApplyKnockback(transform.position - sourcePosition, knockbackForce);

            // A shield eats the whole hit.
            if (powerUps != null && powerUps.TryConsumeShield())
            {
                AudioManager.Play(SoundId.ShieldBlock);
                invulnerableUntil = Time.time + 0.6f;
                return;
            }

            health = Mathf.Max(0f, health - amount);
            GameEvents.RaiseHealthChanged(health, MaxHealth);
            GameEvents.RaisePlayerDamaged(amount);
            AudioManager.Play(SoundId.PlayerHurt);
            CameraShake.Shake(0.25f, 0.35f);

            if (!IsAlive)
            {
                GameEvents.RaisePlayerDied();
                return;
            }

            invulnerableUntil = Time.time + controller.Stats.invulnerabilityTime;
            StartCoroutine(BlinkRoutine());
        }

        /// <summary>Called by PlayerCharacterLoader once the chosen hero model exists.</summary>
        public void SetBlinkRenderers(Renderer[] renderers) => blinkRenderers = renderers;

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            health = Mathf.Min(MaxHealth, health + amount);
            GameEvents.RaiseHealthChanged(health, MaxHealth);
        }

        private IEnumerator BlinkRoutine()
        {
            bool visible = true;
            while (Time.time < invulnerableUntil)
            {
                visible = !visible;
                SetRenderersVisible(visible);
                yield return new WaitForSeconds(blinkInterval);
            }
            SetRenderersVisible(true);
        }

        private void SetRenderersVisible(bool visible)
        {
            foreach (Renderer r in blinkRenderers)
                if (r != null) r.enabled = visible;
        }
    }
}
