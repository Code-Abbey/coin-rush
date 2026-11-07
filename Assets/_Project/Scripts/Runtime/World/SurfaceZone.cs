using CoinRush.Player;
using UnityEngine;

namespace CoinRush.Level
{
    /// <summary>Changes the player's grip while inside (e.g. ice = slippery, mud = sticky).</summary>
    [RequireComponent(typeof(Collider))]
    public class SurfaceZone : MonoBehaviour
    {
        [Tooltip("1 = normal grip. 0.15 = very slippery ice.")]
        [SerializeField, Range(0.05f, 1f)] private float traction = 0.15f;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null) player.SetTraction(traction);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null) player.SetTraction(1f);
        }
    }
}
