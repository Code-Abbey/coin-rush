using CoinRush.Core;
using TMPro;
using UnityEngine;

namespace CoinRush.UI
{
    /// <summary>
    /// Once the portal opens, an arrow orbits the screen centre pointing toward it,
    /// with the distance in metres. Hidden when the portal is on screen and close.
    /// </summary>
    public class PortalCompass : MonoBehaviour
    {
        [SerializeField] private RectTransform arrow;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float radius = 260f;

        private Vector3 portalPosition;
        private bool active;
        private Camera cam;
        private Transform player;

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
        }

        private void OnEnable() => GameEvents.PortalOpened += Activate;
        private void OnDisable() => GameEvents.PortalOpened -= Activate;

        private void Activate(Vector3 position)
        {
            portalPosition = position;
            active = true;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        private void LateUpdate()
        {
            if (!active || group == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null || player == null) return;

            Vector3 toPortal = portalPosition - player.position;
            toPortal.y = 0f;
            float distance = toPortal.magnitude;

            // Direction relative to where the camera looks (screen "up" = camera forward).
            Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            float angle = Vector3.SignedAngle(camForward, toPortal, Vector3.up);

            float rad = -angle * Mathf.Deg2Rad;
            arrow.anchoredPosition = new Vector2(Mathf.Sin(-rad), Mathf.Cos(rad)) * radius;
            arrow.localRotation = Quaternion.Euler(0f, 0f, -angle);
            if (distanceText != null)
            {
                distanceText.text = $"PORTAL {distance:0}m";
                distanceText.rectTransform.anchoredPosition = arrow.anchoredPosition * 0.78f;
            }

            bool visibleAndClose = distance < 8f;
            group.alpha = Mathf.MoveTowards(group.alpha, visibleAndClose ? 0f : 1f, Time.unscaledDeltaTime * 3f);
        }
    }
}
