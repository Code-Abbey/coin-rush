using System;
using System.Collections;
using CoinRush.Audio;
using CoinRush.Core;
using CoinRush.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.Intro
{
    /// <summary>One illustrated page of the intro story: a 3D camera shot plus a caption.</summary>
    [Serializable]
    public class StorySlide
    {
        public string title;
        [TextArea(2, 4)] public string body;
        [Tooltip("Camera pose for this page. The camera slowly drifts from here (Ken Burns style).")]
        public Transform shot;
        public Color accent = Color.white;
        public Sprite icon;
    }

    /// <summary>
    /// Intro story: cuts between 3D vignettes while a caption card slides out and back in
    /// with typewriter text, page dots, and Next / Back / Skip. Ends by loading the main menu.
    /// </summary>
    public class StoryController : MonoBehaviour
    {
        [SerializeField] private Camera storyCamera;
        [SerializeField] private StorySlide[] slides;

        [Header("UI")]
        [SerializeField] private RectTransform captionCard;
        [SerializeField] private CanvasGroup captionGroup;
        [SerializeField] private TMP_Text chapterText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image[] dots;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextLabel;
        [SerializeField] private Button skipButton;

        [Header("Timing")]
        [SerializeField, Min(1f)] private float slideDuration = 8f;
        [SerializeField, Min(1f)] private float charactersPerSecond = 55f;
        [SerializeField, Min(0f)] private float cameraDrift = 1.6f;
        [SerializeField] private string nextScene = SceneNames.MainMenu;

        private int index = -1;
        private float slideStart;
        private bool transitioning;
        private bool leaving;
        private Coroutine typing;
        private Vector2 captionHome;

        private void Awake()
        {
            nextButton.onClick.AddListener(Next);
            skipButton.onClick.AddListener(Finish);
            captionHome = captionCard.anchoredPosition;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StartCoroutine(GoTo(0, firstPage: true));
        }

        private void Update()
        {
            if (leaving || index < 0) return;

            // Ken Burns: the camera slowly slides and pushes in during each page.
            StorySlide s = slides[index];
            if (s.shot != null)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - slideStart) / slideDuration);
                float e = Mathf.SmoothStep(0f, 1f, t);
                storyCamera.transform.SetPositionAndRotation(
                    s.shot.position + s.shot.right * (cameraDrift * (e - 0.5f)) + s.shot.forward * (cameraDrift * 0.6f * e),
                    s.shot.rotation);
            }

            if (transitioning) return;
            if (IntroInput.SkipPressed()) { Finish(); return; }
            if (IntroInput.NextPressed()) { Next(); return; }
            if (IntroInput.BackPressed() && index > 0) { StartCoroutine(GoTo(index - 1, false)); return; }
            if (Time.unscaledTime - slideStart > slideDuration) Next();
        }

        private void Next()
        {
            if (transitioning || leaving) return;
            // First press while text is still typing just completes the text.
            if (typing != null) { CompleteText(); return; }
            if (index >= slides.Length - 1) { Finish(); return; }
            AudioManager.Play(SoundId.UIClick);
            StartCoroutine(GoTo(index + 1, false));
        }

        private void Finish()
        {
            if (leaving) return;
            leaving = true;
            AudioManager.Play(SoundId.UIConfirm);
            SceneNames.Load(nextScene);
        }

        private IEnumerator GoTo(int target, bool firstPage)
        {
            transitioning = true;
            // 1) Caption slides away (the first page simply slides in).
            if (!firstPage)
            {
                StartCoroutine(UITween.Fade(captionGroup, 0f, 0.25f));
                yield return Slide(captionCard, captionCard.anchoredPosition, captionHome + new Vector2(-900f, 0f), 0.3f);
            }

            // 2) Swap page: camera cuts to the next set.
            index = target;
            StorySlide s = slides[index];
            slideStart = Time.unscaledTime;
            chapterText.text = $"CHAPTER {index + 1} OF {slides.Length}";
            titleText.text = s.title;
            titleText.color = s.accent;
            iconImage.sprite = s.icon;
            iconImage.color = s.accent;
            iconImage.enabled = s.icon != null;
            bodyText.text = s.body;
            bodyText.maxVisibleCharacters = 0;
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i].color = i == index ? s.accent : new Color(1f, 1f, 1f, 0.3f);
                dots[i].rectTransform.sizeDelta = new Vector2(i == index ? 44f : 14f, 14f);
            }
            nextLabel.text = index == slides.Length - 1 ? "PLAY" : "NEXT";
            Update(); // snap the camera to the new shot before revealing it

            // 3) Caption slides in, text types out.
            captionGroup.alpha = 0f;
            StartCoroutine(UITween.Fade(captionGroup, 1f, 0.4f));
            StartCoroutine(UITween.Pop(iconImage.transform, 0.5f, 0.3f));
            yield return Slide(captionCard, captionHome + new Vector2(-900f, 0f), captionHome, 0.5f);

            transitioning = false;
            typing = StartCoroutine(TypeText());
        }

        private IEnumerator TypeText()
        {
            int total = bodyText.text.Length;
            float shown = 0f;
            while (shown < total)
            {
                shown += charactersPerSecond * Time.unscaledDeltaTime;
                bodyText.maxVisibleCharacters = Mathf.Min(total, (int)shown);
                yield return null;
            }
            typing = null;
        }

        private void CompleteText()
        {
            if (typing != null) StopCoroutine(typing);
            typing = null;
            bodyText.maxVisibleCharacters = bodyText.text.Length;
        }

        private static IEnumerator Slide(RectTransform rt, Vector2 from, Vector2 to, float duration)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, UITween.EaseOutCubic(t));
                yield return null;
            }
            rt.anchoredPosition = to;
        }
    }
}
