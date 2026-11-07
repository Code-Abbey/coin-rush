using CoinRush.UI;
using UnityEngine.SceneManagement;

namespace CoinRush.Core
{
    /// <summary>Scene names in one place, and scene loading that goes through the screen fader.</summary>
    public static class SceneNames
    {
        public const string Splash = "Splash";
        public const string Story = "Story";
        public const string MainMenu = "MainMenu";
        public const string World1 = "World1_Meadow";
        public const string World2 = "World2_Desert";
        public const string World3 = "World3_Frozen";

        public static void Load(string sceneName) => ScreenFader.FadeToScene(sceneName);
        public static void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);
    }
}
