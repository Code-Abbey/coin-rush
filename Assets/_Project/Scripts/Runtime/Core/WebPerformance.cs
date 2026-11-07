#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine;

namespace CoinRush.Core
{
    /// <summary>
    /// Browser version only: switches to the lighter "web" graphics profile (quality level Medium, which
    /// uses CoinRush_URP_Web). Add <c>?quality=high</c> to the page address for the full desktop graphics.
    /// <para>
    /// The resolution is deliberately fixed. Changing URP's render scale while playing makes the browser
    /// allocate a new set of full-screen buffers for every size, which can exhaust graphics memory and
    /// make the browser drop the game's canvas.
    /// </para>
    /// </summary>
    public static class WebPerformance
    {
        private const int WebQualityLevel = 2;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if ((Application.absoluteURL ?? "").Contains("quality=high")) return;
            QualitySettings.SetQualityLevel(WebQualityLevel, true);
        }
    }
}
#endif
