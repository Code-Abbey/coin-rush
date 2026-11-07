#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoinRush.Core
{
    /// <summary>
    /// Browser version only. Switches to the lighter "web" graphics profile (quality level Medium,
    /// which uses CoinRush_URP_Web) and lowers the 3D resolution a little whenever the browser can't
    /// keep up, then raises it again when there is headroom. The HUD and menus stay sharp because
    /// render scale only affects the 3D view.
    /// Add <c>?quality=high</c> to the page address to play with the full desktop graphics instead.
    /// </summary>
    public class WebPerformance : MonoBehaviour
    {
        private const int WebQualityLevel = 2;
        private const float MinScale = 0.6f, MaxScale = 1f;
        private const float SlowFrame = 1f / 48f, FastFrame = 1f / 57f;

        private float average = 1f / 60f;
        private float timer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if ((Application.absoluteURL ?? "").Contains("quality=high")) return;
            QualitySettings.SetQualityLevel(WebQualityLevel, true);
            var go = new GameObject("WebPerformance") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            go.AddComponent<WebPerformance>();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.25f) return; // ignore loading hitches
            average = Mathf.Lerp(average, dt, 0.05f);
            timer += dt;
            if (timer < 1.5f) return;
            timer = 0f;

            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)) return;
            float scale = urp.renderScale;
            if (average > SlowFrame) scale -= 0.1f;
            else if (average < FastFrame) scale += 0.05f;
            urp.renderScale = Mathf.Clamp(scale, MinScale, MaxScale);
        }
    }
}
#endif
