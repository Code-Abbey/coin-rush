using UnityEngine;

namespace CoinRush.Data
{
    /// <summary>Movement / health tuning for the player character.</summary>
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Coin Rush/Player Stats", order = 1)]
    public class PlayerStats : ScriptableObject
    {
        [Header("Movement")]
        [Min(0f)] public float walkSpeed = 6f;
        [Range(1f, 3f)] public float sprintMultiplier = 1.6f;
        [Tooltip("Degrees per second the character turns toward its move direction.")]
        [Min(0f)] public float rotationSpeed = 720f;
        [Min(0f)] public float acceleration = 40f;

        [Header("Jumping")]
        [Min(0f)] public float jumpHeight = 1.4f;
        [Tooltip("Negative value. Stronger than real gravity for snappier jumps.")]
        public float gravity = -25f;

        [Tooltip("Grace time after walking off a ledge where a jump still works.")]
        [Range(0f, 0.3f)] public float coyoteTime = 0.12f;
        [Tooltip("A jump pressed this long before landing is still executed.")]
        [Range(0f, 0.3f)] public float jumpBufferTime = 0.12f;

        [Header("Dash")]
        [Min(0f)] public float dashSpeed = 22f;
        [Range(0.05f, 0.5f)] public float dashDuration = 0.18f;
        [Min(0.1f)] public float dashCooldown = 1.6f;
        [Tooltip("Stamina consumed per dash.")]
        [Min(0f)] public float dashStaminaCost = 20f;

        [Header("Stamina")]
        [Min(1f)] public float maxStamina = 100f;
        [Tooltip("Stamina used per second while sprinting.")]
        [Min(0f)] public float staminaDrain = 30f;
        [Tooltip("Stamina recovered per second while not sprinting.")]
        [Min(0f)] public float staminaRegen = 20f;
        [Tooltip("Stamina needed before sprinting is allowed again after running dry.")]
        [Min(0f)] public float staminaRecoverThreshold = 25f;

        [Header("Health")]
        [Min(1f)] public float maxHealth = 100f;
        [Tooltip("Seconds of invulnerability after taking a hit.")]
        [Min(0f)] public float invulnerabilityTime = 1.2f;
        [Tooltip("Speed at which knockback decays.")]
        [Min(0.1f)] public float knockbackDamping = 6f;
    }
}
