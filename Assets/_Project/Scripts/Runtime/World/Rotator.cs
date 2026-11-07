using UnityEngine;

namespace CoinRush.Level
{
    /// <summary>Constant rotation (spinning blade hazards, decorative rings, windmills).</summary>
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 90f, 0f);
        [SerializeField] private Space space = Space.Self;

        private void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, space);
    }
}
