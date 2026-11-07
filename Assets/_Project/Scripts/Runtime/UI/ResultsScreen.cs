using System.Collections;
using CoinRush.Audio;
using CoinRush.Core;
using CoinRush.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>
    /// End-of-run results: victory / defeat banner, 3-star rating (one per world cleared), the hero's
    /// portrait, animated stat tiles, a NEW BEST badge and confetti on victory.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        [Header("Banner")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image bannerIcon;
        [SerializeField] private Sprite winIcon;
        [SerializeField] private Sprite loseIcon;
        [SerializeField] private Image rays;
        [SerializeField] private Image backdrop;
        [SerializeField] private Image[] stars;

        [Header("Hero")]
        [SerializeField] private CharacterDatabase heroDatabase;
        [SerializeField] private Image heroPortrait;
        [SerializeField] private Image heroRing;
        [SerializeField] private TMP_Text heroNameText;

        [Header("Stats")]
        [SerializeField] private TMP_Text scoreValue;
        [SerializeField] private TMP_Text coinsValue;
        [SerializeField] private TMP_Text comboValue;
        [SerializeField] private TMP_Text timeValue;
        [SerializeField] private TMP_Text worldValue;
        [SerializeField] private TMP_Text bestValue;
        [SerializeField] private GameObject newBestBadge;

        [Header("Buttons & FX")]
        [SerializeField] private Button retryWorldButton;
        [SerializeField] private UIConfetti confetti;

        [Header("Colours")]
        [SerializeField] private Color winColor = new Color(1f, 0.71f, 0.204f);
        [SerializeField] private Color loseColor = new Color(1f, 0.435f, 0.38f);
        [SerializeField] private Color starOff = new Color(1f, 1f, 1f, 0.15f);

        private bool won;

        private void Update()
        {
            if (rays != null && rays.enabled) rays.rectTransform.Rotate(0f, 0f, 12f * Time.unscaledDeltaTime);
        }

        /// <param name="worldsCleared">0–3; also the number of stars.</param>
        public void Show(bool victory, string reason, int worldsCleared, string worldName, bool newBest, float runTime)
        {
            won = victory;
            Color accent = won ? winColor : loseColor;

            titleText.text = won ? "VICTORY!" : reason;
            titleText.color = accent;
            bannerIcon.sprite = won ? winIcon : loseIcon;
            bannerIcon.color = accent;
            rays.enabled = won;
            rays.color = new Color(accent.r, accent.g, accent.b, 0.35f);
            backdrop.color = won ? new Color(0.063f, 0.075f, 0.122f, 0.84f) : new Color(0.12f, 0.05f, 0.06f, 0.88f);

            CharacterDefinition hero = heroDatabase != null ? heroDatabase.Get(SelectedCharacter.Index) : null;
            if (hero != null)
            {
                heroPortrait.sprite = hero.portrait;
                heroPortrait.color = won ? Color.white : new Color(0.75f, 0.7f, 0.75f);
                heroRing.color = hero.accentColor;
                heroNameText.text = hero.displayName.ToUpper();
                heroNameText.color = hero.accentColor;
            }

            worldValue.text = won ? "ALL 3" : worldName.ToUpper();
            timeValue.text = runTime >= 60f ? $"{(int)runTime / 60}:{runTime % 60f:00.0}" : $"{runTime:0.0}s";
            bestValue.text = HighScoreService.HighScore.ToString("N0");
            newBestBadge.SetActive(false);
            retryWorldButton.gameObject.SetActive(!won);
            foreach (Image s in stars) { s.color = starOff; s.transform.localScale = Vector3.one; }

            StopAllCoroutines();
            StartCoroutine(Animate(Mathf.Clamp(worldsCleared, 0, stars.Length), newBest, accent));
        }

        private IEnumerator Animate(int starCount, bool newBest, Color accent)
        {
            scoreValue.text = coinsValue.text = comboValue.text = "0";
            if (won && confetti != null) confetti.Burst();
            StartCoroutine(UITween.Pop(bannerIcon.transform, 0.6f, 0.2f));
            StartCoroutine(UITween.Pop(titleText.transform, 0.5f, 1.6f));
            yield return new WaitForSecondsRealtime(0.45f);

            for (int i = 0; i < starCount; i++)
            {
                stars[i].color = accent;
                AudioManager.Play(SoundId.GemPickup);
                yield return UITween.Pop(stars[i].transform, 0.3f, 1.8f);
            }

            yield return CountUp(scoreValue, RunSession.TotalScore, 1.1f);
            yield return CountUp(coinsValue, RunSession.TotalCoins, 0.5f);
            yield return CountUp(comboValue, RunSession.BestCombo, 0.35f);

            if (newBest)
            {
                newBestBadge.SetActive(true);
                AudioManager.Play(SoundId.PowerUp);
                StartCoroutine(UITween.Pop(newBestBadge.transform, 0.45f, 0.3f));
            }
        }

        private static IEnumerator CountUp(TMP_Text text, int target, float duration)
        {
            if (target <= 0) { text.text = "0"; yield break; }
            int last = -1;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                int v = Mathf.RoundToInt(target * UITween.EaseOutCubic(t));
                if (v != last) { text.text = v.ToString("N0"); last = v; }
                yield return null;
            }
            text.text = target.ToString("N0");
            AudioManager.Play(SoundId.CoinPickup);
        }
    }
}
