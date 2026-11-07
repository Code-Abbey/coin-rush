using CoinRush.Audio;
using CoinRush.Core;
using CoinRush.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>
    /// Full-screen hero selection: portrait grid on the right, live 3D preview + stats on the left.
    /// Clicking a portrait previews it; SELECT saves the choice, BACK cancels.
    /// </summary>
    public class CharacterSelectScreen : MonoBehaviour
    {
        [SerializeField] private CharacterDatabase database;
        [SerializeField] private HeroPreviewStage stage;
        [Tooltip("Panel hidden while this screen is open (the home page).")]
        [SerializeField] private GameObject homePanel;

        [Header("Grid (one button per hero, same order as the database)")]
        [SerializeField] private Button[] heroButtons;
        [SerializeField] private Image[] selectionFrames;

        [Header("Details")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image speedBar;
        [SerializeField] private Image jumpBar;
        [SerializeField] private Image staminaBar;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button backButton;

        private int previewIndex;

        private void Awake()
        {
            for (int i = 0; i < heroButtons.Length; i++)
            {
                int index = i;
                heroButtons[i].onClick.AddListener(() => Preview(index, cheer: true));
            }
            selectButton.onClick.AddListener(Confirm);
            backButton.onClick.AddListener(Cancel);
        }

        public void Open()
        {
            homePanel.SetActive(false);
            MonoBehaviourRunner.Run(UITween.ShowPanel(gameObject));
            Preview(SelectedCharacter.Index, cheer: false);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Step(-1);
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Step(1);
            if (kb.enterKey.wasPressedThisFrame) Confirm();
            if (kb.escapeKey.wasPressedThisFrame) Cancel();
        }

        private void Step(int dir)
        {
            AudioManager.Play(SoundId.UIHover);
            Preview(previewIndex + dir, cheer: true);
        }

        private void Preview(int index, bool cheer)
        {
            if (database == null || database.Count == 0) return;
            previewIndex = ((index % database.Count) + database.Count) % database.Count;
            CharacterDefinition hero = database.Get(previewIndex);

            stage.Show(hero, cheer);
            nameText.text = hero.displayName.ToUpper();
            nameText.color = hero.accentColor;
            roleText.text = hero.role;
            descriptionText.text = hero.description;
            speedBar.fillAmount = hero.speed / 5f;
            jumpBar.fillAmount = hero.jump / 5f;
            staminaBar.fillAmount = hero.stamina / 5f;

            for (int i = 0; i < selectionFrames.Length; i++)
                if (selectionFrames[i] != null) selectionFrames[i].enabled = i == previewIndex;

            if (cheer) StartCoroutine(UITween.Pop(nameText.transform, 0.3f, 1.25f));
        }

        private void Confirm()
        {
            SelectedCharacter.Index = previewIndex;
            AudioManager.Play(SoundId.UIConfirm);
            Close();
        }

        private void Cancel() => Close();

        private void Close()
        {
            gameObject.SetActive(false);
            MonoBehaviourRunner.Run(UITween.ShowPanel(homePanel));
        }
    }
}
