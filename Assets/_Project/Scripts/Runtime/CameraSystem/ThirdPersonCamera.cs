using CoinRush.Core;
using CoinRush.InputHandling;
using CoinRush.Utilities;
using UnityEngine;

namespace CoinRush.CameraSystem
{
    /// <summary>
    /// Orbiting third-person follow camera.
    /// Mouse X / Q-E rotate around the player, mouse wheel zooms,
    /// and a sphere cast stops the camera from clipping through walls.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Orbit")]
        [SerializeField, Range(1f, 25f)] private float distance = 9f;
        [SerializeField] private Vector2 distanceLimits = new Vector2(4f, 16f);
        [SerializeField, Range(-10f, 80f)] private float pitch = 35f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(10f, 70f);
        [SerializeField] private float mouseSensitivity = 0.12f;
        [Tooltip("Degrees per second at full gamepad stick deflection.")]
        [SerializeField] private float gamepadLookSpeed = 160f;
        [SerializeField] private float keyRotateSpeed = 120f;
        [SerializeField] private float zoomSpeed = 4f;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float followSmoothTime = 0.08f;

        [Header("Speed Feel")]
        [Tooltip("Extra field of view while sprinting / dashing.")]
        [SerializeField] private float sprintFovKick = 6f;
        [SerializeField] private float dashFovKick = 14f;
        [SerializeField] private float fovLerpSpeed = 6f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField, Min(0f)] private float collisionRadius = 0.3f;

        private float yaw;
        private Camera cam;
        private float baseFov;
        private Player.PlayerController playerController;
        private Vector3 smoothedTarget;
        private Vector3 followVelocity;

        public Transform Target { get => target; set => target = value; }

        private void Start()
        {
            yaw = transform.eulerAngles.y;
            cam = GetComponent<Camera>();
            if (cam != null) baseFov = cam.fieldOfView;
            if (target != null) playerController = target.GetComponent<Player.PlayerController>();
            if (target != null) smoothedTarget = target.position + targetOffset;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            bool allowInput = GameManager.Instance == null || GameManager.Instance.IsPlaying;
            if (allowInput) ReadInput();

            Vector3 desiredTarget = target.position + targetOffset;
            smoothedTarget = Vector3.SmoothDamp(smoothedTarget, desiredTarget, ref followVelocity, followSmoothTime);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = rotation * Vector3.back;

            float finalDistance = distance;
            if (Physics.SphereCast(smoothedTarget, collisionRadius, direction, out RaycastHit hit, distance,
                    collisionMask, QueryTriggerInteraction.Ignore))
            {
                finalDistance = Mathf.Max(1f, hit.distance - 0.1f);
            }

            transform.SetPositionAndRotation(smoothedTarget + direction * finalDistance + CameraShake.CurrentOffset, rotation);
            UpdateFov();
        }

        private void UpdateFov()
        {
            if (cam == null || playerController == null) return;
            float kick = playerController.IsDashing ? dashFovKick : (playerController.IsSprinting ? sprintFovKick : 0f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + kick, fovLerpSpeed * Time.deltaTime);
        }

        private void ReadInput()
        {
            InputReader input = InputReader.Instance;
            if (input == null) return;

            Vector2 look = input.Look;
            if (input.LookIsGamepad)
            {
                yaw += look.x * gamepadLookSpeed * Time.deltaTime;
                pitch -= look.y * gamepadLookSpeed * 0.5f * Time.deltaTime;
            }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                float sensitivity = mouseSensitivity * SettingsService.MouseSensitivity;
                yaw += look.x * sensitivity;
                pitch -= look.y * sensitivity * 0.5f;
            }

            yaw += input.CameraRotate * keyRotateSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

            float zoom = input.Zoom;
            if (Mathf.Abs(zoom) > 0.01f)
                distance = Mathf.Clamp(distance - Mathf.Sign(zoom) * zoomSpeed * 0.25f, distanceLimits.x, distanceLimits.y);
        }
    }
}
