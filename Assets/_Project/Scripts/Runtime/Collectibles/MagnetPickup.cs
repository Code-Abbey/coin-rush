using CoinRush.Audio;
using CoinRush.Player;
using UnityEngine;

namespace CoinRush.Collectibles
{
    /// <summary>Pulls nearby coins toward the player for a few seconds.</summary>
    public class MagnetPickup : Pickup
    {
        [SerializeField, Min(1f)] private float duration = 8f;

        protected override SoundId PickupSound => SoundId.PowerUp;
        protected override Color EffectColor => new Color(1f, 0.35f, 0.35f);

        protected override void OnCollected(PlayerController player)
        {
            PlayerPowerUps powerUps = player.GetComponent<PlayerPowerUps>();
            if (powerUps != null) powerUps.ApplyMagnet(duration);
        }
    }
}
