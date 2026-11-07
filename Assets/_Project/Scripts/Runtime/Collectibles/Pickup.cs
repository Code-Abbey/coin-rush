using System;
using CoinRush.Audio;
using CoinRush.Player;
using UnityEngine;

namespace CoinRush.Collectibles
{
    /// <summary>
    /// Base class for everything the player can pick up. Handles the idle animation (spin + bob),
    /// trigger detection, effects and despawning (back to an object pool, or Destroy).
    /// Subclasses only implement <see cref="OnCollected"/> (template-method pattern).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class Pickup : MonoBehaviour
    {
        [Header("Idle Animation")]
        [SerializeField] private Transform visual;
        [SerializeField] private float spinSpeed = 120f;
        [SerializeField] private float bobHeight = 0.25f;
        [SerializeField] private float bobSpeed = 2f;

        [Header("Feedback")]
        [Tooltip("Particle effect spawned on pickup (should auto-destroy).")]
        [SerializeField] private ParticleSystem collectEffectPrefab;

        private Vector3 visualStartLocalPos;
        private float bobPhase;
        private bool collected;

        /// <summary>Set by a spawner that pools this object. If null the pickup is destroyed instead.</summary>
        public Action<Pickup> ReleaseToPool { get; set; }

        protected virtual SoundId PickupSound => SoundId.PowerUp;
        protected virtual Color EffectColor => Color.white;

        protected virtual void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual != null) visualStartLocalPos = visual.localPosition;
        }

        protected virtual void OnEnable()
        {
            collected = false;
            bobPhase = UnityEngine.Random.value * Mathf.PI * 2f; // de-sync neighbouring pickups
        }

        protected virtual void Update()
        {
            if (visual == null) return;
            visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            float offset = Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobHeight;
            visual.localPosition = visualStartLocalPos + Vector3.up * offset;
        }

        private void OnTriggerEnter(Collider other) => TryCollect(other);

        // Stay as well, so e.g. a health pack is taken as soon as the player becomes damaged while standing on it.
        private void OnTriggerStay(Collider other) => TryCollect(other);

        private void TryCollect(Collider other)
        {
            if (collected) return;
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !CanCollect(player)) return;

            collected = true;
            OnCollected(player);
            PlayFeedback();
            Despawn();
        }

        /// <summary>Override to refuse pickup (e.g. health pack when health is already full).</summary>
        protected virtual bool CanCollect(PlayerController player) => true;

        /// <summary>Apply the pickup's effect to the player.</summary>
        protected abstract void OnCollected(PlayerController player);

        private void PlayFeedback()
        {
            AudioManager.Play(PickupSound);
            if (collectEffectPrefab == null) return;

            ParticleSystem fx = Instantiate(collectEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            ParticleSystem.MainModule main = fx.main;
            main.startColor = EffectColor;
            fx.Play();
        }

        public void Despawn()
        {
            if (ReleaseToPool != null) ReleaseToPool(this);
            else Destroy(gameObject);
        }
    }
}
