using System.Collections;
using CoinRush.Audio;
using CoinRush.Core;
using CoinRush.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.Intro
{
    /// <summary>
    /// Splash screen sequence: spinning coin → logo pop → loading bar → "press any key".
    /// Continues to the story automatically after a while, or immediately on any key/click.
    /// </summary>
    public class SplashController : MonoBehaviour
    {
        [Header("Elements")]
        [SerializeField] private CanvasGroup logoGroup;
        [SerializeField] private RectTransform logo;
        [SerializeField] private CanvasGroup taglineGroup;
        [SerializeField] private Image loadingFill;
        [SerializeField] private CanvasGroup pressKeyGroup;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float loadingTime = 2.2f;
        [Tooltip("Auto-continue this many seconds after 'press any key' appears.")]
        [SerializeField, Min(0f)] private float autoContinueDelay = 4f;
        [SerializeField] private string nextScene = SceneNames.Story;

        private bool ready;
        private bool leaving;
        private float readyTime;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            logoGroup.alpha = taglineGroup.alpha = pressKeyGroup.alpha = 0f;
            loadingFill.fillAmount = 0f;
            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            yield return new WaitForSecondsRealtime(0.6f); // let the coin spin into view first

            AudioManager.Play(SoundId.PortalOpen);
            StartCoroutine(UITween.Pop(logo, 0.6f, 0.4f));
            yield return UITween.Fade(logoGroup, 1f, 0.35f);
            yield return UITween.Fade(taglineGroup, 1f, 0.5f);

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / loadingTime)
            {
                loadingFill.fillAmount = UITween.EaseOutCubic(t);
                yield return null;
            }
            loadingFill.fillAmount = 1f;

            MarkReady();
        }

        private void MarkReady()
        {
            ready = true;
            readyTime = Time.unscaledTime;
            loadingFill.transform.parent.gameObject.SetActive(false); // "press any key" takes its place
        }

        private void Update()
        {
            if (leaving) return;

            // Any key skips straight to the end of the intro animation, then continues.
            if (IntroInput.AnyPressed() && Time.timeSinceLevelLoad > 0.4f)
            {
                if (ready) { Continue(); return; }
                StopAllCoroutines();
                logoGroup.alpha = taglineGroup.alpha = 1f; logo.localScale = Vector3.one;
                loadingFill.fillAmount = 1f;
                MarkReady();
            }

            if (!ready) return;
            pressKeyGroup.alpha = 0.55f + 0.45f * Mathf.Sin((Time.unscaledTime - readyTime) * 3.5f);
            if (Time.unscaledTime - readyTime > autoContinueDelay) Continue();
        }

        private void Continue()
        {
            leaving = true;
            AudioManager.Play(SoundId.UIConfirm);
            SceneNames.Load(nextScene);
        }
    }
}
