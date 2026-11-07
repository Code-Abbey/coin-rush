using CoinRush.Audio;
using UnityEngine;

namespace CoinRush.Data
{
    /// <summary>Data for a scoring collectible (coin, gem...). Shared by all instances of a prefab.</summary>
    [CreateAssetMenu(fileName = "CollectibleData", menuName = "Coin Rush/Collectible Data", order = 3)]
    public class CollectibleData : ScriptableObject
    {
        public string displayName = "Coin";
        [Min(0)] public int scoreValue = 1;
        public SoundId pickupSound = SoundId.CoinPickup;
        [Tooltip("Tint applied to the pickup burst particles.")]
        public Color effectColor = new Color(1f, 0.85f, 0.2f);
    }
}
