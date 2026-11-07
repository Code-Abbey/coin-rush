using CoinRush.Core;
using UnityEngine;

namespace CoinRush.Level
{
    /// <summary>
    /// Hurts anything damageable standing inside it (lava pools, spike beds, frost vents).
    /// Damage is applied in ticks; the player's invulnerability frames stop it from stacking.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DamageZone : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damagePerTick = 10f;
        [SerializeField, Min(0f)] private float knockback = 6f;
        [Tooltip("Push the victim out from the zone centre (true) or up (false).")]
        [SerializeField] private bool pushOutward = true;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerStay(Collider other)
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) return;

            // Source slightly below/inside so knockback pushes the victim out of the hazard.
            Vector3 source = pushOutward
                ? new Vector3(transform.position.x, other.transform.position.y, transform.position.z)
                : other.transform.position + Vector3.down;
            if (pushOutward && (source - other.transform.position).sqrMagnitude < 0.01f) source += Vector3.forward * 0.1f;
            target.TakeDamage(damagePerTick, source, knockback);
        }
    }
}
