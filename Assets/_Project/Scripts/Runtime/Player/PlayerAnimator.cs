using CoinRush.Core;
using UnityEngine;

namespace CoinRush.Player
{
    /// <summary>
    /// Feeds the hero's Animator from gameplay: run speed, grounded state, death and a victory cheer.
    /// Pure presentation – it never changes movement.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int DieId = Animator.StringToHash("Die");
        private static readonly int CheerId = Animator.StringToHash("Cheer");

        [SerializeField, Min(1f)] private float speedDamping = 10f;

        private PlayerController controller;
        private Animator animator;
        private float smoothedSpeed;

        private void Awake() => controller = GetComponent<PlayerController>();

        private void Start()
        {
            PlayerCharacterLoader loader = GetComponent<PlayerCharacterLoader>();
            animator = loader != null && loader.ModelAnimator != null ? loader.ModelAnimator : GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            GameEvents.PlayerDied += OnDied;
            GameEvents.WorldCompleted += OnWorldCompleted;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDied -= OnDied;
            GameEvents.WorldCompleted -= OnWorldCompleted;
        }

        private void Update()
        {
            if (!IsReady()) return;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, controller.HorizontalSpeed, speedDamping * Time.deltaTime);
            animator.SetFloat(SpeedId, smoothedSpeed);
            animator.SetBool(GroundedId, controller.IsGroundedBuffered);
        }

        private void OnDied()
        {
            if (IsReady()) animator.SetTrigger(DieId);
        }

        private void OnWorldCompleted(WorldSummary _)
        {
            if (IsReady()) animator.SetTrigger(CheerId);
        }

        private bool warned;

        private bool IsReady()
        {
            if (animator == null || !animator.isActiveAndEnabled) return false;
            if (animator.runtimeAnimatorController != null) return true;
            if (!warned) { warned = true; Debug.LogWarning($"[Coin Rush] Hero animator on '{animator.name}' has no controller.", animator); }
            return false;
        }
    }
}
