using UnityEngine;

namespace CoinRush.Enemies
{
    /// <summary>Drives an enemy model's Animator (walk/run blend by speed, attack trigger).</summary>
    [RequireComponent(typeof(EnemyBehaviour))]
    public class EnemyAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AttackId = Animator.StringToHash("Attack");

        [SerializeField] private Animator animator;
        [Tooltip("Assigned to the model's Animator at runtime if it has none (nested FBX prefabs can drop the override).")]
        [SerializeField] private RuntimeAnimatorController controller;

        private EnemyBehaviour behaviour;
        private float smoothed;

        private void Awake()
        {
            behaviour = GetComponent<EnemyBehaviour>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController == null && controller != null)
                animator.runtimeAnimatorController = controller;
        }

        private void OnEnable() => behaviour.Attacked += OnAttacked;
        private void OnDisable() => behaviour.Attacked -= OnAttacked;

        private void Update()
        {
            if (!IsReady()) return;
            smoothed = Mathf.Lerp(smoothed, behaviour.CurrentSpeed, 8f * Time.deltaTime);
            animator.SetFloat(SpeedId, smoothed);
        }

        private void OnAttacked()
        {
            if (IsReady()) animator.SetTrigger(AttackId);
        }

        private bool warned;

        private bool IsReady()
        {
            if (animator == null || !animator.isActiveAndEnabled) return false;
            if (animator.runtimeAnimatorController != null) return true;
            if (!warned) { warned = true; Debug.LogWarning($"[Coin Rush] Enemy animator on '{name}/{animator.name}' has no controller.", this); }
            return false;
        }
    }
}
