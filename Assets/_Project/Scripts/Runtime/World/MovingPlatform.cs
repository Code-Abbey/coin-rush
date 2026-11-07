using UnityEngine;

namespace CoinRush.Level
{
    /// <summary>
    /// Moves smoothly back and forth between its start position and an offset.
    /// Exposes <see cref="FrameDelta"/> so the player (CharacterController) can ride it.
    /// Runs before the player (execution order) so the delta is fresh each frame.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 travel = new Vector3(0f, 0f, 8f);
        [SerializeField, Min(0.1f)] private float cycleSeconds = 6f;
        [Range(0f, 1f)] [SerializeField] private float phaseOffset;

        private Vector3 start;
        private Vector3 lastPosition;

        public Vector3 FrameDelta { get; private set; }

        private void Awake()
        {
            start = transform.position;
            lastPosition = start;
        }

        private void Update()
        {
            // Smooth ping-pong 0..1..0 using a cosine curve (eases in/out at the ends).
            float t = (1f - Mathf.Cos((Time.time / cycleSeconds + phaseOffset) * Mathf.PI * 2f)) * 0.5f;
            Vector3 target = start + travel * t;
            FrameDelta = target - lastPosition;
            transform.position = target;
            lastPosition = target;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 from = Application.isPlaying ? start : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(from, from + travel);
            Gizmos.DrawWireCube(from + travel, transform.lossyScale);
        }
    }
}
