using System.Collections;
using CoinRush.Core;
using CoinRush.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>Cinematic "WORLD 2 – DESERT RUINS" title card shown when a world starts.</summary>
    public class WorldIntroBanner : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text worldNumberText;
        [SerializeField] private TMP_Text worldNameText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text goalText;
        [SerializeField] private Image accentBar;
        [SerializeField, Min(0.5f)] private float holdTime = 2.6f;

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
        }

        private void OnEnable() => GameEvents.WorldStarted += Show;
        private void OnDisable() => GameEvents.WorldStarted -= Show;

        private void Show(WorldConfig world, int index, int count)
        {
            worldNumberText.text = $"WORLD {index + 1} OF {count}";
            worldNameText.text = world.worldName.ToUpper();
            worldNameText.color = world.themeColor;
            if (subtitleText != null) subtitleText.text = world.subtitle;
            goalText.text = $"Collect <color=#FFD54A>{world.coinQuota}</color> coins to open the portal";
            if (accentBar != null) accentBar.color = world.themeColor;
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            Vector2 start = content.anchoredPosition;
            content.anchoredPosition = start + Vector2.left * 120f;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.5f;
                float e = UITween.EaseOutCubic(Mathf.Clamp01(t));
                group.alpha = e;
                content.anchoredPosition = Vector2.Lerp(start + Vector2.left * 120f, start, e);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(holdTime);
            yield return UITween.Fade(group, 0f, 0.5f);
            content.anchoredPosition = start;
        }
    }
}
