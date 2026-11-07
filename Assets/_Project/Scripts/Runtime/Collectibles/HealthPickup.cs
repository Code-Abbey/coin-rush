using CoinRush.Audio;
using CoinRush.Player;
using UnityEngine;

namespace CoinRush.Collectibles
{
    /// <summary>Restores health. Ignored while the player is at full health so it isn't wasted.</summary>
    public class HealthPickup : Pickup
    {
        [SerializeField, Min(1f)] private float healAmount = 35f;

        protected override SoundId PickupSound => SoundId.Heal;
        protected override Color EffectColor => new Color(0.3f, 1f, 0.4f);

        protected override bool CanCollect(PlayerController player)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            return health != null && health.IsAlive && !health.IsFull;
        }

        protected override void OnCollected(PlayerController player)
        {
            player.GetComponent<PlayerHealth>().Heal(healAmount);
        }
    }
}
