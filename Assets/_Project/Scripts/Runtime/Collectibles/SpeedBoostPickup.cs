using CoinRush.Audio;
using CoinRush.Player;
using UnityEngine;

namespace CoinRush.Collectibles
{
    /// <summary>Temporarily multiplies the player's movement speed.</summary>
    public class SpeedBoostPickup : Pickup
    {
        [SerializeField, Range(1f, 3f)] private float speedMultiplier = 1.5f;
        [SerializeField, Min(0.5f)] private float duration = 6f;

        protected override SoundId PickupSound => SoundId.PowerUp;
        protected override Color EffectColor => new Color(0.3f, 0.8f, 1f);

        protected override void OnCollected(PlayerController player)
        {
            PlayerPowerUps powerUps = player.GetComponent<PlayerPowerUps>();
            if (powerUps != null) powerUps.ApplySpeedBoost(speedMultiplier, duration);
        }
    }
}
