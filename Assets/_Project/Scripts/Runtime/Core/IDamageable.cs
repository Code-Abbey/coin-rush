using UnityEngine;

namespace CoinRush.Core
{
    /// <summary>Anything that can receive damage. Enemies talk to this interface, not to PlayerHealth directly.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        /// <param name="amount">Damage to apply.</param>
        /// <param name="sourcePosition">World position of the attacker (used for knockback direction).</param>
        /// <param name="knockbackForce">Strength of the push away from the source.</param>
        void TakeDamage(float amount, Vector3 sourcePosition, float knockbackForce);
    }
}
