using System;
using CoinRush.Audio;
using CoinRush.Core;
using CoinRush.Data;
using CoinRush.Spawning;
using UnityEngine;

namespace CoinRush.Enemies
{
    /// <summary>
    /// Rule-based enemy behaviour (no machine learning), written as a small finite state machine:
    /// Idle -> Patrol -> (sees/hears player) -> Chase -> (loses player) -> Search -> Patrol.
    /// Uses a CharacterController for movement and a forward probe for simple obstacle avoidance.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EnemyBehaviour : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private EnemyStats stats;

        [Header("Scene References (set by spawner at runtime)")]
        [SerializeField] private Transform player;
        [SerializeField] private SpawnArea patrolArea;

        [Header("Perception")]
        [Tooltip("Layers that block line of sight.")]
        [SerializeField] private LayerMask sightBlockers;
        [SerializeField] private float eyeHeight = 1f;

        [Header("Steering")]
        [SerializeField] private float avoidProbeDistance = 1.8f;
        [SerializeField] private float stuckTimeout = 1.5f;

        [Header("Attack")]
        [Tooltip("Seconds the enemy stands still after landing a hit, giving the player a window to escape.")]
        [SerializeField, Min(0f)] private float postAttackPause = 1.2f;

        private CharacterController controller;
        private IDamageable playerDamageable;
        private Vector3 destination;
        private Vector3 lastKnownPlayerPos;
        private float verticalVelocity;
        private float stateTimer;
        private float nextAttackTime;
        private float stuckTimer;
        private float recoverUntil;

        public EnemyState State { get; private set; } = EnemyState.Idle;
        /// <summary>Horizontal speed this frame (for animation).</summary>
        public float CurrentSpeed { get; private set; }
        /// <summary>Raised when the enemy lands an attack (for the attack animation).</summary>
        public event Action Attacked;
        public float SpeedMultiplier { get; set; } = 1f;
        public EnemyStats Stats => stats;

        /// <summary>(previous, current) — used by EnemyVisual for colour / alert icon.</summary>
        public event Action<EnemyState, EnemyState> StateChanged;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (stats == null) stats = ScriptableObject.CreateInstance<EnemyStats>();
        }

        /// <summary>Called by EnemySpawner right after Instantiate (scene objects cannot be referenced from prefabs).</summary>
        public void Initialize(Transform target, SpawnArea area)
        {
            player = target;
            patrolArea = area;
            playerDamageable = target != null ? target.GetComponent<IDamageable>() : null;
        }

        private void Start()
        {
            if (playerDamageable == null && player != null) playerDamageable = player.GetComponent<IDamageable>();
            PickPatrolDestination();
        }

        private void Update()
        {
            bool roundActive = GameManager.Instance == null || GameManager.Instance.IsPlaying;
            if (!roundActive)
            {
                if (State != EnemyState.Idle && GameManager.Instance != null && GameManager.Instance.State != GameState.Paused)
                    ChangeState(EnemyState.Idle);
                ApplyGravityOnly();
                return;
            }
            if (State == EnemyState.Idle) ChangeState(EnemyState.Patrol);

            stateTimer += Time.deltaTime;
            switch (State)
            {
                case EnemyState.Patrol: UpdatePatrol(); break;
                case EnemyState.Chase: UpdateChase(); break;
                case EnemyState.Search: UpdateSearch(); break;
            }
        }

        // ---------------- States ----------------

        private void UpdatePatrol()
        {
            if (CanDetectPlayer())
            {
                AudioManager.Play(SoundId.EnemyAlert);
                ChangeState(EnemyState.Chase);
                return;
            }

            if (MoveTowards(destination, stats.patrolSpeed) < 0.6f) PickPatrolDestination();
        }

        private void UpdateChase()
        {
            if (player == null || playerDamageable == null || !playerDamageable.IsAlive)
            {
                ChangeState(EnemyState.Patrol);
                return;
            }

            float distance = FlatDistance(transform.position, player.position);
            if (HasLineOfSight()) lastKnownPlayerPos = player.position;

            if (distance > stats.loseInterestRange || (!HasLineOfSight() && stateTimer > 1f && distance > stats.hearingRange))
            {
                ChangeState(EnemyState.Search);
                return;
            }

            if (Time.time < recoverUntil)
            {
                ApplyGravityOnly(); // catching its breath after a hit
                return;
            }

            MoveTowards(player.position, stats.chaseSpeed);

            if (distance <= stats.attackRange && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + stats.attackCooldown;
                recoverUntil = Time.time + postAttackPause;
                playerDamageable.TakeDamage(stats.damage, transform.position, stats.knockbackForce);
                Attacked?.Invoke();
                AudioManager.Play(SoundId.EnemyAttack);
            }
        }

        private void UpdateSearch()
        {
            if (CanDetectPlayer())
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            float remaining = MoveTowards(lastKnownPlayerPos, stats.patrolSpeed);
            if (remaining < 0.8f || stateTimer > stats.searchDuration + 3f)
            {
                // Look around briefly at the last known position, then resume patrolling.
                transform.Rotate(0f, 120f * Time.deltaTime, 0f);
                if (stateTimer > stats.searchDuration) ChangeState(EnemyState.Patrol);
            }
        }

        private void ChangeState(EnemyState newState)
        {
            if (newState == State) return;
            EnemyState previous = State;
            State = newState;
            stateTimer = 0f;
            stuckTimer = 0f;
            if (newState == EnemyState.Patrol) PickPatrolDestination();
            StateChanged?.Invoke(previous, newState);
        }

        // ---------------- Perception ----------------

        private bool CanDetectPlayer()
        {
            if (player == null || playerDamageable == null || !playerDamageable.IsAlive) return false;

            float distance = FlatDistance(transform.position, player.position);
            if (distance <= stats.hearingRange) return true;
            if (distance > stats.sightRange) return false;

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (Vector3.Angle(transform.forward, toPlayer) > stats.fieldOfView * 0.5f) return false;

            return HasLineOfSight();
        }

        private bool HasLineOfSight()
        {
            if (player == null) return false;
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 target = player.position + Vector3.up * eyeHeight;
            return !Physics.Linecast(eye, target, sightBlockers, QueryTriggerInteraction.Ignore);
        }

        // ---------------- Movement ----------------

        /// <summary>Moves toward a point with simple obstacle avoidance. Returns remaining flat distance.</summary>
        private float MoveTowards(Vector3 target, float baseSpeed)
        {
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance < 0.05f) { ApplyGravityOnly(); return distance; }

            Vector3 direction = AvoidObstacles(toTarget / distance);

            Quaternion look = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, stats.turnSpeed * Time.deltaTime);

            float speed = baseSpeed * SpeedMultiplier;
            UpdateGravity();
            Vector3 before = transform.position;
            controller.Move((transform.forward * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

            DetectStuck(before, speed);
            CurrentSpeed = FlatDistance(before, transform.position) / Mathf.Max(Time.deltaTime, 0.0001f);
            return distance;
        }

        private Vector3 AvoidObstacles(Vector3 desired)
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (!Physics.SphereCast(origin, 0.4f, desired, out RaycastHit hit, avoidProbeDistance, sightBlockers, QueryTriggerInteraction.Ignore))
                return desired;

            // Slide along the obstacle: pick whichever tangent is closer to where we want to go.
            Vector3 tangent = Vector3.Cross(hit.normal, Vector3.up).normalized;
            if (Vector3.Dot(tangent, desired) < 0f) tangent = -tangent;
            return (tangent + desired * 0.3f).normalized;
        }

        private void DetectStuck(Vector3 previousPosition, float intendedSpeed)
        {
            float moved = FlatDistance(previousPosition, transform.position);
            if (moved < intendedSpeed * Time.deltaTime * 0.2f) stuckTimer += Time.deltaTime;
            else stuckTimer = 0f;

            if (stuckTimer > stuckTimeout)
            {
                stuckTimer = 0f;
                if (State == EnemyState.Search) ChangeState(EnemyState.Patrol);
                else if (State == EnemyState.Patrol) PickPatrolDestination();
            }
        }

        private void UpdateGravity()
        {
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        private void ApplyGravityOnly()
        {
            CurrentSpeed = 0f;
            UpdateGravity();
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void PickPatrolDestination()
        {
            if (patrolArea != null && patrolArea.TryGetRandomPoint(out Vector3 point)) destination = point;
            else destination = transform.position + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // ---------------- Debug ----------------

        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;
            Vector3 p = transform.position + Vector3.up * 0.1f;

            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
            DrawCircle(p, stats.sightRange);
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            DrawCircle(p, stats.hearingRange);
            Gizmos.color = Color.red;
            DrawCircle(p, stats.attackRange);

            // Field of view edges
            Gizmos.color = Color.yellow;
            Vector3 left = Quaternion.Euler(0f, -stats.fieldOfView * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, stats.fieldOfView * 0.5f, 0f) * transform.forward;
            Gizmos.DrawLine(p, p + left * stats.sightRange);
            Gizmos.DrawLine(p, p + right * stats.sightRange);

            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(p, State == EnemyState.Patrol ? destination : lastKnownPlayerPos);
            }
        }

        private static void DrawCircle(Vector3 center, float radius, int segments = 32)
        {
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
