using UnityEngine;

namespace CoinRush.Utilities
{
    /// <summary>Keeps an object (e.g. the enemy "!" marker) facing the main camera.</summary>
    public class Billboard : MonoBehaviour
    {
        private Transform cam;

        private void LateUpdate()
        {
            if (cam == null)
            {
                if (Camera.main == null) return;
                cam = Camera.main.transform;
            }
            transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
        }
    }
}
