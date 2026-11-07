using CoinRush.Audio;
using CoinRush.Player;
using UnityEngine;

namespace CoinRush.Level
{
    /// <summary>Bounces the player high into the air, or along a guaranteed arc onto a raised platform (landingTarget).</summary>
    [RequireComponent(typeof(Collider))]
    public class JumpPad : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float launchVelocity = 17f;
        [Tooltip("Extra push along the pad's forward axis (0 = straight up).")]
        [SerializeField, Min(0f)] private float forwardPush = 0f;
        [Tooltip("Optional: the spot this pad sends the player to (a ballistic arc that always lands there).")]
        [SerializeField] private Transform landingTarget;
        [Tooltip("How far above the higher end of the jump the arc peaks.")]
        [SerializeField, Min(0.5f)] private float arcHeight = 2.5f;

        /// <summary>Where this pad sends the player (null = straight up).</summary>
        public Transform LandingTarget => landingTarget;
        [SerializeField] private Transform padTop;
        [SerializeField] private float squashAmount = 0.35f;

        private float squash;
        private Vector3 baseScale;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (padTop != null) baseScale = padTop.localScale;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            if (landingTarget != null) player.LaunchTo(landingTarget.position, arcHeight);
            else player.Launch(launchVelocity, transform.forward * forwardPush);
            AudioManager.Play(SoundId.JumpPad);
            squash = 1f;
        }

        private void Update()
        {
            if (padTop == null || squash <= 0f) return;
            squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 3f);
            float s = Mathf.Sin(squash * Mathf.PI) * squashAmount;
            padTop.localScale = new Vector3(baseScale.x + s, baseScale.y * (1f - s), baseScale.z + s);
        }
    }
}
