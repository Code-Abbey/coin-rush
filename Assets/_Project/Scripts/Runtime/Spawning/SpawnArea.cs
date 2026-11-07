using UnityEngine;

namespace CoinRush.Spawning
{
    /// <summary>
    /// Rectangular area on the ground used to find free random spawn points.
    /// Points overlapping colliders on <see cref="blockingLayers"/> are rejected.
    /// The area is drawn as a gizmo in the Scene view.
    /// </summary>
    public class SpawnArea : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(40f, 40f);
        [Tooltip("Layers that a spawn point must not overlap (walls, obstacles, hazards). Triggers on these layers count too.")]
        [SerializeField] private LayerMask blockingLayers;
        [SerializeField, Min(0.1f)] private float clearanceRadius = 1f;
        [SerializeField, Min(1)] private int maxAttempts = 30;

        public Vector2 Size => size;

        /// <summary>Returns a random free point at ground height (this transform's Y).</summary>
        public bool TryGetRandomPoint(out Vector3 point, Vector3? avoid = null, float minDistanceFromAvoid = 0f)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 local = new Vector3(Random.Range(-size.x, size.x) * 0.5f, 0f, Random.Range(-size.y, size.y) * 0.5f);
                Vector3 candidate = transform.position + local;

                if (avoid.HasValue)
                {
                    Vector3 flat = candidate - avoid.Value;
                    flat.y = 0f;
                    if (flat.magnitude < minDistanceFromAvoid) continue;
                }

                Vector3 checkCenter = candidate + Vector3.up * (clearanceRadius + 0.05f);
                if (Physics.CheckSphere(checkCenter, clearanceRadius, blockingLayers, QueryTriggerInteraction.Collide)) continue;
                // Never under a roof, platform or overhang (the pickup would be unreachable or hidden).
                if (Physics.Raycast(candidate + Vector3.up * 0.2f, Vector3.up, 15f, ~0, QueryTriggerInteraction.Ignore)) continue;

                point = candidate;
                return true;
            }

            point = transform.position;
            return false;
        }

        public bool Contains(Vector3 worldPoint)
        {
            Vector3 local = worldPoint - transform.position;
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.z) <= size.y * 0.5f;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.1f, size.y));
        }
    }
}
