using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>
    /// Persistent full-screen fade used for every scene change (DontDestroyOnLoad).
    /// Creates itself on first use, so no scene needs to contain it.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private static ScreenFader instance;

        private CanvasGroup group;
        private CanvasGroup loadingGroup;
        private TMP_Text tipText;
        private TMP_Text loadingText;
        private bool busy;

        private static readonly string[] Tips =
        {
            "Dashing makes you invulnerable for a moment. Dash through danger!",
            "Quick pickups build a COMBO – up to x4 points.",
            "Gems are worth 5 coins. Look for them on high ground.",
            "Enemies turn red when they spot you. Break line of sight to escape.",
            "The magnet power-up pulls in every coin nearby.",
            "A shield blocks one hit – save it for the orc brutes.",
            "Jump pads launch you to hidden coins.",
            "Ice is slippery: start turning early.",
            "Time left when you reach the portal becomes bonus points.",
            "Sprinting drains stamina. Let it refill before a big chase.",
        };

        private const float FadeOutTime = 0.45f;
        private const float FadeInTime = 0.6f;

        private static ScreenFader Instance
        {
            get
            {
                if (instance == null) instance = Create();
                return instance;
            }
        }

        public static void FadeToScene(string sceneName) => Instance.StartFade(sceneName);

        private static ScreenFader Create()
        {
            GameObject go = new GameObject("[ScreenFader]");
            DontDestroyOnLoad(go);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            go.AddComponent<GraphicRaycaster>();

            GameObject img = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(go.transform, false);
            RectTransform rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.GetComponent<Image>().color = new Color(0.063f, 0.075f, 0.122f, 1f);

            ScreenFader fader = go.AddComponent<ScreenFader>();
            fader.BuildLoadingCard(go.transform);
            fader.group = go.AddComponent<CanvasGroup>();
            fader.group.alpha = 0f;
            fader.group.blocksRaycasts = false;
            return fader;
        }

        /// <summary>"Loading" spinner + random gameplay tip shown while a world loads.</summary>
        private void BuildLoadingCard(Transform root)
        {
            GameObject card = new GameObject("Loading", typeof(RectTransform), typeof(CanvasGroup));
            card.transform.SetParent(root, false);
            RectTransform cardRt = (RectTransform)card.transform;
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0f);
            cardRt.anchoredPosition = new Vector2(0f, 120f);
            cardRt.sizeDelta = new Vector2(1400f, 160f);
            loadingGroup = card.GetComponent<CanvasGroup>();
            loadingGroup.alpha = 0f;

            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            header.transform.SetParent(card.transform, false);
            RectTransform headerRt = (RectTransform)header.transform;
            headerRt.anchoredPosition = new Vector2(0f, 40f);
            headerRt.sizeDelta = new Vector2(600f, 50f);
            loadingText = header.GetComponent<TextMeshProUGUI>();
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingText.fontSize = 34f;
            loadingText.fontStyle = FontStyles.Bold;
            loadingText.characterSpacing = 8f;
            loadingText.color = new Color(1f, 0.71f, 0.2f);

            GameObject tip = new GameObject("Tip", typeof(RectTransform), typeof(TextMeshProUGUI));
            tip.transform.SetParent(card.transform, false);
            RectTransform tipRt = (RectTransform)tip.transform;
            tipRt.anchoredPosition = new Vector2(0f, -30f);
            tipRt.sizeDelta = new Vector2(1400f, 60f);
            tipText = tip.GetComponent<TextMeshProUGUI>();
            tipText.alignment = TextAlignmentOptions.Center;
            tipText.fontSize = 30f;
            tipText.color = new Color(0.85f, 0.88f, 1f);
        }

        private void Update()
        {
            if (loadingText != null && loadingGroup.alpha > 0f)
                loadingText.text = "LOADING" + new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);
        }

        private void StartFade(string sceneName)
        {
            if (busy) return;
            StartCoroutine(FadeRoutine(sceneName));
        }

        private IEnumerator FadeRoutine(string sceneName)
        {
            busy = true;
            group.blocksRaycasts = true;
            bool showTip = sceneName.StartsWith("World");
            if (showTip) tipText.text = $"<color=#FFB534>TIP</color>   {Tips[Random.Range(0, Tips.Length)]}";
            loadingGroup.alpha = 0f;
            yield return UITween.Fade(group, 1f, FadeOutTime);
            if (showTip) yield return UITween.Fade(loadingGroup, 1f, 0.25f);

            Time.timeScale = 1f;
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (load != null && !load.isDone) yield return null;
            if (showTip) yield return new WaitForSecondsRealtime(1.2f); // long enough to read the tip
            loadingGroup.alpha = 0f;

            yield return UITween.Fade(group, 0f, FadeInTime);
            group.blocksRaycasts = false;
            busy = false;
        }
    }
}
