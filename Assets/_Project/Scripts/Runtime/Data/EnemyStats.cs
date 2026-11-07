using UnityEngine;

namespace CoinRush.Data
{
    /// <summary>Behaviour tuning for an enemy archetype. Several assets can exist (e.g. "Slime", "Brute").</summary>
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "Coin Rush/Enemy Stats", order = 2)]
    public class EnemyStats : ScriptableObject
    {
        [Header("Movement")]
        [Min(0f)] public float patrolSpeed = 2.5f;
        [Min(0f)] public float chaseSpeed = 4.6f;
        [Min(0f)] public float turnSpeed = 360f;

        [Header("Senses")]
        [Tooltip("Distance at which the enemy can see the player (inside its field of view).")]
        [Min(0f)] public float sightRange = 9f;
        [Tooltip("Distance at which the enemy notices the player regardless of facing (hearing).")]
        [Min(0f)] public float hearingRange = 3.5f;
        [Range(10f, 360f)] public float fieldOfView = 120f;
        [Tooltip("Enemy gives up the chase beyond this distance.")]
        [Min(0f)] public float loseInterestRange = 16f;
        [Tooltip("Seconds the enemy keeps searching the last known position before patrolling again.")]
        [Min(0f)] public float searchDuration = 2.5f;

        [Header("Attack")]
        [Min(0f)] public float attackRange = 1.4f;
        [Min(0f)] public float damage = 15f;
        [Min(0.1f)] public float attackCooldown = 1.6f;
        [Min(0f)] public float knockbackForce = 12f;
    }
}
