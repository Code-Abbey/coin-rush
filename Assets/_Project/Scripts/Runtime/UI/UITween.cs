using System.Collections;
using UnityEngine;

namespace CoinRush.UI
{
    /// <summary>
    /// Tiny coroutine tween helpers for UI (no external tween library needed).
    /// All use unscaled time so they also animate while the game is paused.
    /// </summary>
    public static class UITween
    {
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        /// <summary>Scales from 'from' to 1 with an overshoot "pop".</summary>
        public static IEnumerator Pop(Transform target, float duration = 0.35f, float from = 0.6f)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                float s = Mathf.LerpUnclamped(from, 1f, EaseOutBack(Mathf.Clamp01(t)));
                target.localScale = Vector3.one * s;
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        public static IEnumerator Fade(CanvasGroup group, float to, float duration = 0.25f)
        {
            float from = group.alpha;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
                group.alpha = Mathf.Lerp(from, to, EaseOutCubic(Mathf.Clamp01(t)));
                yield return null;
            }
            group.alpha = to;
        }

        /// <summary>Fades a panel in (activating it) with a pop.</summary>
        public static IEnumerator ShowPanel(GameObject panel, float duration = 0.3f)
        {
            panel.SetActive(true);
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null) yield break;
            group.alpha = 0f;
            Transform content = panel.transform.childCount > 0 ? panel.transform.GetChild(0) : panel.transform;
            MonoBehaviourRunner.Run(Pop(content, duration, 0.85f));
            yield return Fade(group, 1f, duration);
        }
    }
}
