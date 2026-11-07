using UnityEngine;

namespace CoinRush.CameraSystem
{
    /// <summary>
    /// Top-down orthographic camera that follows the player and renders into a RenderTexture
    /// shown by the HUD minimap. It only sees the "Minimap" icon layer plus the level geometry,
    /// so the map reads as a clean, simple overview.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MinimapCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float height = 60f;
        [Tooltip("Rotate the map with the main camera so 'up' on the map is where you're looking.")]
        [SerializeField] private bool rotateWithView = true;
        [SerializeField] private Transform viewTransform;

        public Transform Target { get => target; set => target = value; }

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = new Vector3(target.position.x, target.position.y + height, target.position.z);

            float yaw = rotateWithView && viewTransform != null ? viewTransform.eulerAngles.y : 0f;
            transform.rotation = Quaternion.Euler(90f, yaw, 0f);
        }
    }
}
