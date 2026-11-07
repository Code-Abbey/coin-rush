using System;
using UnityEngine;

namespace CoinRush.Core
{
    /// <summary>Player options persisted with PlayerPrefs (volume, mouse sensitivity).</summary>
    public static class SettingsService
    {
        private const string MusicKey = "CoinRush.MusicVolume";
        private const string SfxKey = "CoinRush.SfxVolume";
        private const string SensitivityKey = "CoinRush.MouseSensitivity";

        public static event Action Changed;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.6f);
            set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
            set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        /// <summary>0.2 .. 3, multiplies the camera's base sensitivity.</summary>
        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat(SensitivityKey, 1f);
            set { PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, 0.2f, 3f)); Changed?.Invoke(); }
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
