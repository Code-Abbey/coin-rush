using UnityEngine;

namespace CoinRush.Core
{
    /// <summary>Remembers which hero the player picked on the home page (persists between sessions).</summary>
    public static class SelectedCharacter
    {
        private const string Key = "CoinRush.SelectedCharacter";

        public static int Index
        {
            get => PlayerPrefs.GetInt(Key, 0);
            set
            {
                PlayerPrefs.SetInt(Key, value);
                PlayerPrefs.Save();
            }
        }
    }
}
