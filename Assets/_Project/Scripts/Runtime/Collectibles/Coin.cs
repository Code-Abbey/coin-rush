using CoinRush.Audio;
using CoinRush.Core;
using CoinRush.Data;
using CoinRush.Player;
using UnityEngine;

namespace CoinRush.Collectibles
{
    /// <summary>
    /// Scoring collectible. Value / sound / colour come from a <see cref="CollectibleData"/> asset,
    /// so the same script drives Coin and Gem prefabs. Flies toward the player while the Magnet is active.
    /// Pickup sound pitch rises with the combo for satisfying feedback.
    /// </summary>
    public class Coin : Pickup
    {
        [SerializeField] private CollectibleData data;

        public CollectibleData Data => data;
        protected override SoundId PickupSound => SoundId.None; // played manually with combo pitch
        protected override Color EffectColor => data != null ? data.effectColor : Color.yellow;

        protected override void Update()
        {
            base.Update();
            PullTowardMagnet();
        }

        private void PullTowardMagnet()
        {
            PlayerPowerUps magnet = PlayerPowerUps.Active;
            if (magnet == null || !magnet.IsMagnetActive) return;

            Vector3 target = magnet.transform.position + Vector3.up * 0.2f;
            Vector3 toPlayer = target - transform.position;
            if (toPlayer.sqrMagnitude > magnet.MagnetRadius * magnet.MagnetRadius) return;

            transform.position = Vector3.MoveTowards(transform.position, target, magnet.MagnetPullSpeed * Time.deltaTime);
        }

        protected override bool CanCollect(PlayerController player) =>
            GameManager.Instance == null || GameManager.Instance.IsPlaying;

        protected override void OnCollected(PlayerController player)
        {
            int value = data != null ? data.scoreValue : 1;
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.CollectCoin(value, transform.position + Vector3.up, EffectColor);

            int combo = gm != null ? gm.Combo : 0;
            float pitch = 1f + Mathf.Min(combo, 15) * 0.035f;
            AudioManager.PlayPitched(data != null ? data.pickupSound : SoundId.CoinPickup, pitch);
        }
    }
}
