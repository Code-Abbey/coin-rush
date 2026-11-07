using UnityEngine;

namespace CoinRush.Data
{
    /// <summary>All selectable heroes plus the animation set they share.</summary>
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Coin Rush/Character Database", order = 11)]
    public class CharacterDatabase : ScriptableObject
    {
        public CharacterDefinition[] characters;
        [Tooltip("Animator Controller shared by every hero (same Kenney rig).")]
        public RuntimeAnimatorController heroController;
        [Tooltip("Height in metres every hero model is scaled to.")]
        [Min(0.5f)] public float heroHeight = 1.55f;

        public int Count => characters != null ? characters.Length : 0;

        public CharacterDefinition Get(int index)
        {
            if (Count == 0) return null;
            index = ((index % Count) + Count) % Count; // wrap around
            return characters[index];
        }
    }
}
