using CoinRush.Audio;
using CoinRush.Player;
using UnityEngine;

namespace CoinRush.Collectibles
{
    /// <summary>Gives a bubble shield that absorbs the next hit (or expires after a while).</summary>
    public class ShieldPickup : Pickup
    {
        [SerializeField, Min(1f)] private float duration = 15f;

        protected override SoundId PickupSound => SoundId.PowerUp;
        protected override Color EffectColor => new Color(0.4f, 1f, 0.85f);

        protected override void OnCollected(PlayerController player)
        {
            PlayerPowerUps powerUps = player.GetComponent<PlayerPowerUps>();
            if (powerUps != null) powerUps.ApplyShield(duration);
        }
    }
}
