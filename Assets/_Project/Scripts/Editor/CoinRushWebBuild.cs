using System.IO;
using System.Linq;
using UnityEditor;
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
        public static void Build()
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

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolder));
            if (Directory.Exists(output)) Directory.Delete(output, true);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            });

            BuildSummary s = report.summary;
            Report($"{s.result}: {s.totalSize / (1024f * 1024f):F1} MB, {s.totalErrors} errors, {s.totalWarnings} warnings, " +
                   $"{(s.buildEndedAt - s.buildStartedAt).TotalMinutes:F1} min, scenes={scenes.Length}, output={output}");
        }

        private static void Report(string line)
        {
            Debug.Log("[Coin Rush] Web build " + line);
            File.WriteAllText(Path.Combine(Application.dataPath, "../Temp/web_build_result.txt"), line + "\n");
        }
    }
}
