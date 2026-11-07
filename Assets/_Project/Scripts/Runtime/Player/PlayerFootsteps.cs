using CoinRush.Audio;
using UnityEngine;

namespace CoinRush.Player
{
    /// <summary>
    /// Footstep sounds and dust puffs timed to the player's ground speed,
    /// plus a bigger dust burst when landing from a jump.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] private ParticleSystem footDust;
        [SerializeField] private ParticleSystem landDust;
        [Tooltip("Metres travelled between two steps.")]
        [SerializeField, Min(0.2f)] private float strideLength = 1.6f;
        [SerializeField, Min(0f)] private float minSpeed = 1.5f;

        private PlayerController controller;
        private float distanceSinceStep;

        private void Awake() => controller = GetComponent<PlayerController>();
        private void OnEnable() => controller.Landed += OnLanded;
        private void OnDisable() => controller.Landed -= OnLanded;

        private void Update()
        {
            float speed = controller.HorizontalSpeed;
            if (!controller.IsGroundedBuffered || speed < minSpeed || controller.IsDashing)
            {
                distanceSinceStep = strideLength * 0.6f; // first step comes quickly
                return;
            }

            distanceSinceStep += speed * Time.deltaTime;
            if (distanceSinceStep < strideLength) return;
            distanceSinceStep = 0f;

            AudioManager.PlayPitched(SoundId.Footstep, controller.IsSprinting ? 1.1f : 1f);
            if (footDust != null) footDust.Emit(controller.IsSprinting ? 4 : 2);
        }

        private void OnLanded(float impactSpeed)
        {
            if (landDust != null) landDust.Emit(Mathf.RoundToInt(Mathf.Clamp(impactSpeed, 8f, 22f)));
        }
    }
}
