using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CoinRush.EditorTools
{
    /// <summary>
    /// Builds the browser (WebGL) version of the game into <c>Builds/WebGL</c>, with settings that work on
    /// static hosts such as GitHub Pages (Gzip + decompression fallback, because those hosts can't send
    /// the special headers Unity's default Brotli compression needs).
    /// </summary>
    public static class CoinRushWebBuild
    {
        public const string OutputFolder = "Builds/WebGL";

        [MenuItem("Coin Rush/Build Web Version", priority = 30)]
        public static void Build() => Build(debug: false);

        /// <summary>
        /// Development build with full error messages (C# exceptions with stack traces) and the testing
        /// shortcuts (F1-F3), written to Builds/WebGL-Debug. Slower; use it to track down browser-only problems.
        /// </summary>
        [MenuItem("Coin Rush/Build Web Version (Debug)", priority = 31)]
        public static void BuildDebug() => Build(debug: true);

        private static void Build(bool debug)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Coin Rush] Exit Play mode first."); return; }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Report("FAILED: WebGL Build Support is not installed (Unity Hub > Installs > Add modules).");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true; // works without server-side headers
            PlayerSettings.WebGL.dataCaching = true;            // faster second visit
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            // Faster game code in the browser (longer build, quicker frames).
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSpeed);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Master);

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            WebGLExceptionSupport releaseExceptions = PlayerSettings.WebGL.exceptionSupport;
            if (debug) PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", debug ? OutputFolder + "-Debug" : OutputFolder));
            if (Directory.Exists(output)) Directory.Delete(output, true);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = debug ? BuildOptions.Development : BuildOptions.None,
            });
            PlayerSettings.WebGL.exceptionSupport = releaseExceptions;

            BuildSummary s = report.summary;
            if (s.result == BuildResult.Succeeded) PatchIndexHtml(Path.Combine(output, "index.html"));
            Report($"{s.result}: {s.totalSize / (1024f * 1024f):F1} MB, {s.totalErrors} errors, {s.totalWarnings} warnings, " +
                   $"{(s.buildEndedAt - s.buildStartedAt).TotalMinutes:F1} min, scenes={scenes.Length}, output={output}");
        }

        /// <summary>
        /// Caps the canvas resolution on Retina screens (a 2x screen otherwise draws 4x the pixels), shows a
        /// friendly reload message if the browser ever drops the game's graphics, and adds an optional
        /// frame-rate readout. Page address options: ?quality=high (full resolution and
        /// desktop graphics), ?fps (show frames per second).
        /// </summary>
        private static void PatchIndexHtml(string path)
        {
            const string anchor = "      document.querySelector(\"#unity-loading-bar\").style.display = \"block\";";
            string html = File.ReadAllText(path);
            if (!html.Contains(anchor)) { Debug.LogWarning("[Coin Rush] index.html layout changed; not patched."); return; }
            const string patch =
@"      // Coin Rush: browsers are slower than the editor, so don't render at full Retina resolution.
      // Add ?quality=high to the address for full desktop graphics, or ?fps to show the frame rate.
      if (!/[?&]quality=high/.test(location.search))
        config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, 1.5);
      // If the browser resets the graphics (for example when the GPU runs out of memory) the canvas goes
      // blank while the music keeps playing. Explain what happened instead of leaving a blank screen.
      canvas.addEventListener('webglcontextlost', function () {
        var lost = document.createElement('div');
        lost.style.cssText = 'position:fixed;inset:0;z-index:10;display:flex;align-items:center;justify-content:center;' +
          'flex-direction:column;gap:12px;background:rgba(10,12,24,.92);color:#fff;font:600 18px sans-serif;cursor:pointer';
        lost.innerHTML = '<div>The browser reset the game\'s graphics.</div>' +
          '<div style=""color:#FFC94D"">Click to reload Coin Rush</div>';
        lost.onclick = function () { location.reload(); };
        document.body.appendChild(lost);
      });
      if (/[?&]fps/.test(location.search)) {
        var fpsBox = document.createElement('div');
        fpsBox.style.cssText = 'position:fixed;top:8px;left:8px;z-index:9;padding:4px 8px;border-radius:6px;' +
          'background:rgba(0,0,0,.65);color:#7CFC7C;font:600 14px monospace;pointer-events:none';
        document.body.appendChild(fpsBox);
        var fpsFrames = 0, fpsStart = performance.now();
        (function fpsTick(now) {
          fpsFrames++;
          if (now - fpsStart >= 500) {
            fpsBox.textContent = Math.round(fpsFrames * 1000 / (now - fpsStart)) + ' fps';
            fpsFrames = 0; fpsStart = now;
          }
          requestAnimationFrame(fpsTick);
        })(performance.now());
      }

";
            File.WriteAllText(path, html.Replace(anchor, patch + anchor));
        }

        private static void Report(string line)
        {
            Debug.Log("[Coin Rush] Web build " + line);
            File.WriteAllText(Path.Combine(Application.dataPath, "../Temp/web_build_result.txt"), line + "\n");
        }
    }
}
