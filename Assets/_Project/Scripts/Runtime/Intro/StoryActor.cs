using UnityEngine;

namespace CoinRush.Intro
{
    /// <summary>
    /// Makes a model in the story scene "act": sets a locomotion speed, fires a trigger (cheer / attack)
    /// every few seconds, and can walk back and forth along its forward axis.
    /// </summary>
    public class StoryActor : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");

        [SerializeField] private Animator animator;
        [SerializeField] private RuntimeAnimatorController controller;
        [Tooltip("Value written to the Animator 'Speed' parameter (0 idle, ~2 walk, ~7 sprint).")]
        [SerializeField] private float animSpeed;
        [SerializeField] private string trigger = "";
        [SerializeField, Min(0.5f)] private float triggerInterval = 3f;
        [Tooltip("Units per second the actor actually moves forward (0 = stays in place).")]
        [SerializeField] private float moveSpeed;
        [Tooltip("Distance after which the walker loops back to its start.")]
        [SerializeField, Min(0.1f)] private float loopDistance = 6f;

        private Vector3 start;
        private float nextTrigger;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null && controller != null) animator.runtimeAnimatorController = controller;
            start = transform.localPosition;
            nextTrigger = Time.time + Random.Range(0.3f, triggerInterval);
        }

        private void Update()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(SpeedId, animSpeed);
                if (!string.IsNullOrEmpty(trigger) && Time.time >= nextTrigger)
                {
                    animator.SetTrigger(trigger);
                    nextTrigger = Time.time + triggerInterval + Random.Range(-0.4f, 0.4f);
                }
            }

            if (moveSpeed <= 0f) return;
            transform.localPosition += transform.localRotation * Vector3.forward * (moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.localPosition, start) > loopDistance) transform.localPosition = start;
        }
    }
}
