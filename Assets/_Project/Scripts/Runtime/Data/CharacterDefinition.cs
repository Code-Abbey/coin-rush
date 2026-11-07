using UnityEngine;

namespace CoinRush.Data
{
    /// <summary>
    /// A playable hero: which 3D model to use, how it's presented in the character picker,
    /// and small gameplay differences (ratings 1-5, 3 = baseline).
    /// </summary>
    [CreateAssetMenu(fileName = "Hero_", menuName = "Coin Rush/Character Definition", order = 10)]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Presentation")]
        public string displayName = "Hero";
        public string role = "Adventurer";
        [TextArea(2, 4)] public string description = "";
        public Color accentColor = new Color(1f, 0.82f, 0.22f);
        [Tooltip("Portrait shown in the hero grid and the HUD.")]
        public Sprite portrait;

        [Header("Model")]
        [Tooltip("Rigged model (Kenney Mini Characters FBX). Animations come from the shared Animator Controller.")]
        public GameObject model;
        [Tooltip("Rotation applied if the model's forward axis isn't +Z.")]
        public float yawOffset;

        [Header("Stats (1-5, 3 = normal)")]
        [Range(1, 5)] public int speed = 3;
        [Range(1, 5)] public int jump = 3;
        [Range(1, 5)] public int stamina = 3;

        /// <summary>Rating -> multiplier. 3 = 1.0, every step = ±6%.</summary>
        public static float ToMultiplier(int rating) => 1f + (rating - 3) * 0.06f;

        public float SpeedMultiplier => ToMultiplier(speed);
        public float JumpMultiplier => ToMultiplier(jump);
        public float StaminaMultiplier => ToMultiplier(stamina);
    }
}
