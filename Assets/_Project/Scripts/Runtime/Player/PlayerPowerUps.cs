using CoinRush.Core;
using UnityEngine;

namespace CoinRush.Player
{
    /// <summary>
    /// Timed power-ups on the player: Speed Boost, Coin Magnet and Shield.
    /// Re-collecting a power-up refreshes its timer. Visuals (trail, magnet ring, shield bubble)
    /// are toggled here; the shield bubble uses the SG_ForceField Shader Graph material.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [DisallowMultipleComponent]
    public class PlayerPowerUps : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private TrailRenderer boostTrail;
        [SerializeField] private GameObject magnetVisual;
        [SerializeField] private GameObject shieldVisual;

        [Header("Magnet")]
        [SerializeField, Min(1f)] private float magnetRadius = 9f;
        [SerializeField, Min(1f)] private float magnetPullSpeed = 16f;

        private PlayerController controller;
        private float boostLeft, boostDuration;
        private float magnetLeft, magnetDuration;
        private float shieldLeft, shieldDuration;

        public bool IsBoosted => boostLeft > 0f;
        public bool IsMagnetActive => magnetLeft > 0f;
        public bool IsShielded => shieldLeft > 0f;
        public float MagnetRadius => magnetRadius;
        public float MagnetPullSpeed => magnetPullSpeed;

        /// <summary>The magnet is queried by coins every frame, so expose the active player statically.</summary>
        public static PlayerPowerUps Active { get; private set; }

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            Active = this;
            SetVisual(boostTrail, false);
            SetVisual(magnetVisual, false);
            SetVisual(shieldVisual, false);
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        private void Update()
        {
            Tick(ref boostLeft, boostDuration, PowerUpType.SpeedBoost, () =>
            {
                controller.SetSpeedMultiplier(1f);
                SetVisual(boostTrail, false);
            });
            Tick(ref magnetLeft, magnetDuration, PowerUpType.Magnet, () => SetVisual(magnetVisual, false));
            Tick(ref shieldLeft, shieldDuration, PowerUpType.Shield, () => SetVisual(shieldVisual, false));
        }

        private static void Tick(ref float left, float duration, PowerUpType type, System.Action onEnd)
        {
            if (left <= 0f) return;
            left -= Time.deltaTime;
            if (left <= 0f)
            {
                left = 0f;
                onEnd();
            }
            GameEvents.RaisePowerUpChanged(type, left, duration);
        }

        public void ApplySpeedBoost(float multiplier, float duration)
        {
            boostLeft = boostDuration = duration;
            controller.SetSpeedMultiplier(multiplier);
            SetVisual(boostTrail, true);
            GameEvents.RaiseNotification("SPEED BOOST!", new Color(0.4f, 0.8f, 1f));
        }

        public void ApplyMagnet(float duration)
        {
            magnetLeft = magnetDuration = duration;
            SetVisual(magnetVisual, true);
            GameEvents.RaiseNotification("COIN MAGNET!", new Color(1f, 0.45f, 0.45f));
        }

        public void ApplyShield(float duration)
        {
            shieldLeft = shieldDuration = duration;
            SetVisual(shieldVisual, true);
            GameEvents.RaiseNotification("SHIELD UP!", new Color(0.45f, 1f, 0.85f));
        }

        /// <summary>Called by PlayerHealth when hit. Returns true if the shield absorbed the hit.</summary>
        public bool TryConsumeShield()
        {
            if (!IsShielded) return false;
            shieldLeft = 0f;
            SetVisual(shieldVisual, false);
            GameEvents.RaisePowerUpChanged(PowerUpType.Shield, 0f, shieldDuration);
            GameEvents.RaiseNotification("SHIELD BLOCKED!", new Color(0.45f, 1f, 0.85f));
            return true;
        }

        private static void SetVisual(Component c, bool on)
        {
            if (c == null) return;
            if (c is TrailRenderer trail) trail.emitting = on;
            else c.gameObject.SetActive(on);
        }

        private static void SetVisual(GameObject go, bool on)
        {
            if (go != null) go.SetActive(on);
        }
    }
}
