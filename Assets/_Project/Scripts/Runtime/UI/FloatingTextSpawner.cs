using System.Collections;
using System.Collections.Generic;
using CoinRush.Core;
using TMPro;
using UnityEngine;

namespace CoinRush.UI
{
    /// <summary>
    /// Shows "+N" popups where points are earned. World position is converted to screen space,
    /// and texts are pooled (re-used) instead of being created and destroyed every pickup.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private TMP_Text template;
        [SerializeField, Min(0.1f)] private float lifetime = 0.9f;
        [SerializeField] private float riseDistance = 90f;

        private readonly Queue<TMP_Text> pool = new Queue<TMP_Text>();
        private Camera cam;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            if (template != null) template.gameObject.SetActive(false);
        }

        private void OnEnable() => GameEvents.PointsAwarded += Spawn;
        private void OnDisable() => GameEvents.PointsAwarded -= Spawn;

        private void Spawn(Vector3 worldPosition, int points, Color color)
        {
            if (template == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f) return; // behind the camera

            TMP_Text text = pool.Count > 0 ? pool.Dequeue() : Instantiate(template, transform);
            text.gameObject.SetActive(true);
            text.text = $"+{points}";
            text.color = color;
            text.fontSize = points >= 5 ? template.fontSize * 1.35f : template.fontSize;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, null, out Vector2 local);
            StartCoroutine(Animate(text, local));
        }

        private IEnumerator Animate(TMP_Text text, Vector2 start)
        {
            RectTransform rt = text.rectTransform;
            Color baseColor = text.color;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / lifetime;
                float eased = UITween.EaseOutCubic(t);
                rt.anchoredPosition = start + Vector2.up * riseDistance * eased;
                rt.localScale = Vector3.one * (t < 0.15f ? Mathf.Lerp(0.5f, 1.2f, t / 0.15f) : Mathf.Lerp(1.2f, 0.9f, t));
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - Mathf.Clamp01((t - 0.6f) / 0.4f));
                yield return null;
            }
            text.gameObject.SetActive(false);
            pool.Enqueue(text);
        }
    }
}
