using CoinRush.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CoinRush.Utilities
{
    /// <summary>
    /// Testing shortcuts, only active in the Editor and development builds:
    /// F1 = +10 coins, F2 = teleport the player in front of the portal, F3 = lose the run (test the defeat screen).
    /// </summary>
    public class DevCheats : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            Keyboard kb = Keyboard.current;
            GameManager gm = GameManager.Instance;
            if (kb == null || gm == null || !gm.IsPlaying) return;

            if (kb.f1Key.wasPressedThisFrame)
            {
                for (int i = 0; i < 10; i++) gm.CollectCoin(1, Vector3.zero, Color.yellow);
            }

            if (kb.f3Key.wasPressedThisFrame)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null && p.TryGetComponent(out CoinRush.Player.PlayerHealth health))
                    health.TakeDamage(9999f, p.transform.position, 0f);
            }

            if (kb.f2Key.wasPressedThisFrame)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                var portal = FindFirstObjectByType<CoinRush.Level.Portal>();
                if (player == null || portal == null) return;
                CharacterController cc = player.GetComponent<CharacterController>();
                cc.enabled = false;
                player.transform.position = portal.transform.position - portal.transform.forward * 6f + Vector3.up * 0.2f;
                cc.enabled = true;
            }
        }
#endif
    }
}
