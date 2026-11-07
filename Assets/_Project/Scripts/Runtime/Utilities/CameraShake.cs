using UnityEngine;

namespace CoinRush.Utilities
{
    /// <summary>
    /// Provides a decaying random offset that the camera rig adds to its position.
    /// Call <see cref="Shake"/> from anywhere (e.g. when the player is hit).
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        private static CameraShake instance;

        private float duration;
        private float timeLeft;
        private float magnitude;

        /// <summary>Current offset to add to the camera position this frame.</summary>
        public static Vector3 CurrentOffset { get; private set; }

        private void Awake() => instance = this;

        private void OnDestroy()
        {
            if (instance == this) { instance = null; CurrentOffset = Vector3.zero; }
        }

        public static void Shake(float shakeDuration, float shakeMagnitude)
        {
            if (instance == null) return;
            instance.duration = Mathf.Max(0.01f, shakeDuration);
            instance.timeLeft = instance.duration;
            instance.magnitude = shakeMagnitude;
        }

        private void Update()
        {
            if (timeLeft <= 0f) { CurrentOffset = Vector3.zero; return; }

            timeLeft -= Time.unscaledDeltaTime;
            float strength = magnitude * Mathf.Clamp01(timeLeft / duration);
            CurrentOffset = Random.insideUnitSphere * strength;
        }
    }
}
