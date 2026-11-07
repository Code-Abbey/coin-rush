using CoinRush.Data;
using CoinRush.Utilities;
using UnityEngine;

namespace CoinRush.UI
{
    /// <summary>
    /// Turntable far away from the diorama that shows one hero model. A dedicated camera renders it
    /// into a RenderTexture, which the UI displays in a RawImage (works anywhere in the menu).
    /// </summary>
    public class HeroPreviewStage : MonoBehaviour
    {
        private static readonly int CheerId = Animator.StringToHash("Cheer");

        [SerializeField] private CharacterDatabase database;
        [SerializeField] private Transform turntable;
        [SerializeField] private int stageLayer = 14;
        [SerializeField] private float spinSpeed = 25f;

        private GameObject current;
        private CharacterDefinition shown;

        private void Update()
        {
            if (turntable != null) turntable.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
        }

        /// <summary>Displays a hero; optionally plays the cheer animation.</summary>
        public void Show(CharacterDefinition hero, bool cheer)
        {
            if (hero == null || hero.model == null || turntable == null) return;
            if (hero == shown && current != null)
            {
                if (cheer) Cheer();
                return;
            }

            if (current != null) Destroy(current);
            shown = hero;
            current = Instantiate(hero.model, turntable);
            current.transform.localRotation = Quaternion.Euler(0f, hero.yawOffset, 0f);
            ModelUtility.FitToHeight(current, database != null ? database.heroHeight : 1.55f);
            ModelUtility.SetLayer(current, stageLayer);

            Animator anim = current.GetComponentInChildren<Animator>();
            if (anim == null) anim = current.AddComponent<Animator>();
            if (database != null) anim.runtimeAnimatorController = database.heroController;
            anim.applyRootMotion = false;
            if (cheer) anim.SetTrigger(CheerId);
        }

        private void Cheer()
        {
            Animator anim = current.GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null) anim.SetTrigger(CheerId);
        }
    }
}
