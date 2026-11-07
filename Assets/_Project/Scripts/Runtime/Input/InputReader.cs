using UnityEngine;
using UnityEngine.InputSystem;

namespace CoinRush.InputHandling
{
    /// <summary>
    /// Single entry point for player input (new Input System).
    /// Reads the "Gameplay" map of the CoinRushControls.inputactions asset and exposes
    /// simple properties, so gameplay code never touches devices or bindings directly.
    /// Supports keyboard/mouse and gamepad through the asset's control schemes.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public class InputReader : MonoBehaviour
    {
        public static InputReader Instance { get; private set; }

        [Tooltip("Assets/_Project/Settings/Input/CoinRushControls.inputactions")]
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMapName = "Gameplay";

        private InputActionMap map;
        private InputAction move, look, cameraRotate, zoom, jump, sprint, dash, pause;

        public Vector2 Move => Read<Vector2>(move);
        public Vector2 Look => Read<Vector2>(look);
        public float CameraRotate => Read<float>(cameraRotate);
        public float Zoom => Read<float>(zoom);
        public bool SprintHeld => sprint != null && sprint.IsPressed();
        public bool JumpPressed => jump != null && jump.WasPressedThisFrame();
        public bool DashPressed => dash != null && dash.WasPressedThisFrame();
        public bool PausePressed => pause != null && pause.WasPressedThisFrame();

        /// <summary>True when the last look input came from a gamepad stick (needs per-second scaling).</summary>
        public bool LookIsGamepad => look?.activeControl?.device is Gamepad;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (actions == null)
            {
                Debug.LogError("InputReader has no InputActionAsset assigned.", this);
                return;
            }

            map = actions.FindActionMap(actionMapName, throwIfNotFound: true);
            move = map.FindAction("Move");
            look = map.FindAction("Look");
            cameraRotate = map.FindAction("CameraRotate");
            zoom = map.FindAction("Zoom");
            jump = map.FindAction("Jump");
            sprint = map.FindAction("Sprint");
            dash = map.FindAction("Dash");
            pause = map.FindAction("Pause");
        }

        private void OnEnable() => map?.Enable();
        private void OnDisable() => map?.Disable();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static T Read<T>(InputAction action) where T : struct =>
            action != null ? action.ReadValue<T>() : default;
    }
}
