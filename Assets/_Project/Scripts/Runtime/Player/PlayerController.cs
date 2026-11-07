using CoinRush.Audio;
using CoinRush.Core;
using CoinRush.Data;
using CoinRush.InputHandling;
using CoinRush.Level;
using UnityEngine;

namespace CoinRush.Player
{
    /// <summary>
    /// Camera-relative third-person movement on a CharacterController:
    /// walk / sprint (stamina), jump with coyote time + jump buffer, dash, gravity,
    /// knockback, jump-pad launches, riding moving platforms and slippery (ice) surfaces.
    /// All input comes from <see cref="InputReader"/> (new Input System).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlayerStats stats;

        [Header("References")]
        [Tooltip("Movement is relative to this transform's forward. Defaults to Camera.main.")]
        [SerializeField] private Transform cameraTransform;
        [Tooltip("Particles played while dashing (optional).")]
        [SerializeField] private ParticleSystem dashEffect;

        [Header("Ground Probe")]
        [SerializeField] private LayerMask groundMask = ~0;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private Vector3 knockbackVelocity;
        private float stamina;
        private bool exhausted;
        private float speedMultiplier = 1f;
        private float traction = 1f;

        // Per-hero multipliers (set by PlayerCharacterLoader from the chosen CharacterDefinition).
        private float heroSpeed = 1f, heroJump = 1f, heroStamina = 1f;

        private float lastGroundedTime = -999f;
        private float lastJumpPressedTime = -999f;
        private float dashEndTime = -999f;
        private float nextDashTime;
        private Vector3 dashDirection;
        private Vector3 spawnPoint;
        private bool wasGrounded = true;
        private float airborneMinVelocity;

        // Targeted jump-pad launch: a ballistic arc that lands exactly on the pad's landing spot.
        private bool arcActive;
        private Vector3 arcVelocity;
        private float arcStartTime, arcEndTime;

        /// <summary>Raised on touchdown with the downward speed (for landing sound / dust).</summary>
        public event System.Action<float> Landed;

        public PlayerStats Stats => stats;
        public bool IsSprinting { get; private set; }
        public bool IsDashing => Time.time < dashEndTime;
        public bool IsGrounded => controller.isGrounded;
        public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;
        public float HorizontalSpeed => (IsDashing ? stats.dashSpeed : horizontalVelocity.magnitude);
        /// <summary>Grounded with a short grace period (CharacterController.isGrounded flickers on slopes).</summary>
        public bool IsGroundedBuffered => Time.time - lastGroundedTime < 0.15f;
        /// <summary>True while flying along a jump-pad arc.</summary>
        public bool IsLaunched => arcActive;
        private float MaxStamina => stats.maxStamina * heroStamina;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (stats == null)
            {
                Debug.LogError("PlayerController needs a PlayerStats asset.", this);
                stats = ScriptableObject.CreateInstance<PlayerStats>();
            }
            stats = JsonConfig.Apply(stats, JsonConfig.Load()); // external JSON tuning (runtime copy)
            stamina = stats.maxStamina;
            spawnPoint = transform.position;
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            stamina = MaxStamina;
            GameEvents.RaiseStaminaChanged(stamina, MaxStamina);
            GameEvents.RaiseDashCharge(1f);
        }

        private void Update()
        {
            bool canControl = GameManager.Instance == null || GameManager.Instance.IsPlaying;
            InputReader input = InputReader.Instance;

            Vector3 moveDir = canControl ? ReadMoveDirection(input) : Vector3.zero;

            if (canControl && input != null && input.JumpPressed) lastJumpPressedTime = Time.time;
            if (canControl && input != null && input.DashPressed && !arcActive) TryDash(moveDir);

            HandleSprintAndStamina(canControl && input != null && input.SprintHeld && moveDir.sqrMagnitude > 0.01f);
            HandleHorizontalMovement(moveDir);
            HandleJumpAndGravity();
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, stats.knockbackDamping * Time.deltaTime);

            Vector3 planar = arcActive ? arcVelocity : (IsDashing ? dashDirection * stats.dashSpeed : horizontalVelocity);
            Vector3 motion = (planar + knockbackVelocity + Vector3.up * verticalVelocity) * Time.deltaTime;
            motion += GetPlatformDelta();
            controller.Move(motion);

            if (controller.isGrounded) lastGroundedTime = Time.time;
            if (arcActive && (Time.time > arcEndTime || (controller.isGrounded && verticalVelocity <= 0f && Time.time > arcStartTime + 0.2f)))
            {
                arcActive = false;
                horizontalVelocity = arcVelocity * 0.25f; // soft landing, keep a little momentum
            }
            DetectLanding();
            UpdateDashCharge();
            if (transform.position.y < -15f) RespawnAfterFall();
        }

        // ------------------------------------------------------------------ input

        private Vector3 ReadMoveDirection(InputReader input)
        {
            if (input == null) return Vector3.zero;
            Vector2 raw = Vector2.ClampMagnitude(input.Move, 1f);
            if (cameraTransform == null) return new Vector3(raw.x, 0f, raw.y);

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            return forward * raw.y + right * raw.x;
        }

        // ------------------------------------------------------------------ stamina / sprint

        private void HandleSprintAndStamina(bool wantsSprint)
        {
            if (exhausted && stamina >= stats.staminaRecoverThreshold) exhausted = false;
            IsSprinting = wantsSprint && !exhausted && stamina > 0f;

            float previous = stamina;
            if (IsSprinting)
            {
                stamina -= stats.staminaDrain * Time.deltaTime;
                if (stamina <= 0f) { stamina = 0f; exhausted = true; }
            }
            else if (!IsDashing)
            {
                stamina = Mathf.Min(MaxStamina, stamina + stats.staminaRegen * Time.deltaTime);
            }

            if (!Mathf.Approximately(previous, stamina)) GameEvents.RaiseStaminaChanged(stamina, MaxStamina);
        }

        // ------------------------------------------------------------------ movement

        private void HandleHorizontalMovement(Vector3 direction)
        {
            float targetSpeed = stats.walkSpeed * heroSpeed * speedMultiplier * (IsSprinting ? stats.sprintMultiplier : 1f);
            Vector3 targetVelocity = direction * targetSpeed;

            // Low traction (ice) = slow to accelerate and slow to stop.
            float accel = stats.acceleration * traction * (controller.isGrounded ? 1f : 0.6f);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, accel * Time.deltaTime);

            Vector3 faceDir = IsDashing ? dashDirection : direction;
            if (faceDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(faceDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, stats.rotationSpeed * Time.deltaTime);
            }
        }

        private void HandleJumpAndGravity()
        {
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;

            bool jumpBuffered = Time.time - lastJumpPressedTime <= stats.jumpBufferTime;
            bool coyote = Time.time - lastGroundedTime <= stats.coyoteTime;
            if (jumpBuffered && coyote && verticalVelocity <= 0.1f)
            {
                verticalVelocity = Mathf.Sqrt(stats.jumpHeight * heroJump * -2f * stats.gravity);
                lastJumpPressedTime = -999f;
                lastGroundedTime = -999f;
                AudioManager.Play(SoundId.Jump);
            }

            if (!IsDashing) verticalVelocity += stats.gravity * Time.deltaTime;
            else verticalVelocity = Mathf.Max(verticalVelocity, 0f); // dashes stay level
        }

        private void TryDash(Vector3 moveDir)
        {
            if (Time.time < nextDashTime || stamina < stats.dashStaminaCost) return;

            dashDirection = moveDir.sqrMagnitude > 0.01f ? moveDir.normalized : transform.forward;
            dashEndTime = Time.time + stats.dashDuration;
            nextDashTime = Time.time + stats.dashCooldown;
            stamina -= stats.dashStaminaCost;
            GameEvents.RaiseStaminaChanged(stamina, MaxStamina);
            horizontalVelocity = dashDirection * stats.walkSpeed * speedMultiplier; // keep momentum after dash
            AudioManager.Play(SoundId.Dash);
            if (dashEffect != null) dashEffect.Play();
        }

        private void UpdateDashCharge()
        {
            float remaining = nextDashTime - Time.time;
            float charge = remaining <= 0f ? 1f : 1f - remaining / stats.dashCooldown;
            GameEvents.RaiseDashCharge(charge);
        }

        private void DetectLanding()
        {
            bool grounded = controller.isGrounded;
            if (!grounded) airborneMinVelocity = Mathf.Min(airborneMinVelocity, verticalVelocity);
            else if (!wasGrounded && airborneMinVelocity < -7f)
            {
                Landed?.Invoke(-airborneMinVelocity);
                AudioManager.Play(SoundId.Land);
            }
            if (grounded) airborneMinVelocity = 0f;
            wasGrounded = grounded;
        }

        // ------------------------------------------------------------------ platforms

        /// <summary>If standing on a MovingPlatform, move along with it this frame.</summary>
        private Vector3 GetPlatformDelta()
        {
            if (!controller.isGrounded && verticalVelocity > 0f) return Vector3.zero;

            Vector3 origin = transform.position + Vector3.up * 0.2f;
            if (Physics.SphereCast(origin, controller.radius * 0.8f, Vector3.down, out RaycastHit hit, 0.5f,
                    groundMask, QueryTriggerInteraction.Ignore))
            {
                MovingPlatform platform = hit.collider.GetComponentInParent<MovingPlatform>();
                if (platform != null) return platform.FrameDelta;
            }
            return Vector3.zero;
        }

        /// <summary>Safety net: falling out of the world puts the player back at the start.</summary>
        private void RespawnAfterFall()
        {
            controller.enabled = false;
            transform.position = spawnPoint + Vector3.up;
            controller.enabled = true;
            horizontalVelocity = knockbackVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        // ------------------------------------------------------------------ external forces

        public void ApplyKnockback(Vector3 direction, float force)
        {
            direction.y = 0f;
            knockbackVelocity = direction.normalized * force;
            verticalVelocity = Mathf.Max(verticalVelocity, 4f);
        }

        /// <summary>Jump pads: sets an upward velocity (and optional forward push).</summary>
        public void Launch(float upwardVelocity, Vector3 push)
        {
            verticalVelocity = upwardVelocity;
            knockbackVelocity += push;
            lastGroundedTime = -999f;
        }

        /// <summary>
        /// Jump pads with a landing spot: computes the ballistic arc (apex <paramref name="arcHeight"/> above the
        /// higher end) from this hero's gravity and flies along it, so the player lands right on the target.
        /// </summary>
        public void LaunchTo(Vector3 landing, float arcHeight)
        {
            float g = -stats.gravity;
            Vector3 start = transform.position;
            float apex = Mathf.Max(start.y, landing.y) + Mathf.Max(0.5f, arcHeight);
            float vy = Mathf.Sqrt(2f * g * (apex - start.y));
            float flightTime = vy / g + Mathf.Sqrt(2f * (apex - landing.y) / g);
            Vector3 flat = new Vector3(landing.x - start.x, 0f, landing.z - start.z);

            arcVelocity = flat / Mathf.Max(0.1f, flightTime);
            verticalVelocity = vy;
            horizontalVelocity = knockbackVelocity = Vector3.zero;
            dashEndTime = -999f;
            lastGroundedTime = -999f;
            arcActive = true;
            arcStartTime = Time.time;
            arcEndTime = Time.time + flightTime + 0.4f;
            if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
        }

        /// <summary>Applies the chosen hero's speed / jump / stamina multipliers.</summary>
        public void ApplyCharacterModifiers(float speed, float jump, float staminaMul)
        {
            heroSpeed = speed;
            heroJump = jump;
            heroStamina = staminaMul;
            stamina = MaxStamina;
        }

        public void SetSpeedMultiplier(float multiplier) => speedMultiplier = Mathf.Max(0.1f, multiplier);

        /// <summary>1 = normal grip, lower = slippery. Set by SurfaceZone triggers.</summary>
        public void SetTraction(float value) => traction = Mathf.Clamp(value, 0.05f, 1f);
    }
}
