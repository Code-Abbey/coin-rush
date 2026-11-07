using CoinRush.Core;
using CoinRush.Data;
using CoinRush.Utilities;
using UnityEngine;

namespace CoinRush.Player
{
    /// <summary>
    /// Spawns the hero chosen on the home page inside the player's ModelSocket, scales it,
    /// hooks up the shared Animator Controller and applies the hero's stat multipliers.
    /// Runs before the other player scripts so they see the final model.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCharacterLoader : MonoBehaviour
    {
        [SerializeField] private CharacterDatabase database;
        [Tooltip("Child transform the model is spawned under (keeps the root free for physics).")]
        [SerializeField] private Transform modelSocket;

        public CharacterDefinition Current { get; private set; }
        public Animator ModelAnimator { get; private set; }

        private void Awake()
        {
            if (database == null || modelSocket == null || database.Count == 0) return;

            Current = database.Get(SelectedCharacter.Index);
            if (Current == null || Current.model == null) return;

            // Remove the editor preview model baked into the prefab.
            for (int i = modelSocket.childCount - 1; i >= 0; i--) Destroy(modelSocket.GetChild(i).gameObject);

            GameObject model = Instantiate(Current.model, modelSocket);
            model.name = Current.displayName;
            model.transform.localRotation = Quaternion.Euler(0f, Current.yawOffset, 0f);
            ModelUtility.FitToHeight(model, database.heroHeight);
            ModelUtility.SetLayer(model, gameObject.layer);

            ModelAnimator = model.GetComponentInChildren<Animator>();
            if (ModelAnimator == null) ModelAnimator = model.AddComponent<Animator>();
            ModelAnimator.runtimeAnimatorController = database.heroController;
            ModelAnimator.applyRootMotion = false;
            ModelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            GetComponent<PlayerController>().ApplyCharacterModifiers(
                Current.SpeedMultiplier, Current.JumpMultiplier, Current.StaminaMultiplier);

            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null) health.SetBlinkRenderers(model.GetComponentsInChildren<Renderer>());
        }
    }
}
