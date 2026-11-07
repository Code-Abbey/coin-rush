using System.IO;
using CoinRush.Data;
using CoinRush.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinRush.EditorTools
{
    /// <summary>
    /// Renders preview images of the procedural L-system trees (three seeds per world style) into
    /// Docs/Design/images, for the design vault. Uses a temporary additive scene, so nothing is saved.
    /// </summary>
    public static class FractalTreePreview
    {
        private static readonly Vector3 Far = new Vector3(5000f, 0f, 5000f);
        [MenuItem("Coin Rush/Render Fractal Tree Previews", priority = 21)]
        public static void Render()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Coin Rush] Exit Play mode first."); return; }
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Design/images"));
            Directory.CreateDirectory(outDir);

            Material barkBase = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Environment/M_FractalBark.mat");
            Material leafBase = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Environment/M_FractalLeaves.mat");
            if (barkBase == null || leafBase == null) { Debug.LogError("[Coin Rush] Run Build Everything first (fractal materials missing)."); return; }

            CoinRushConfigData json = JsonConfig.Load();
            string[] scenes = { "World1_Meadow", "World2_Desert", "World3_Frozen" };
            Color[] backgrounds = { new Color(0.62f, 0.8f, 0.95f), new Color(0.98f, 0.8f, 0.55f), new Color(0.1f, 0.14f, 0.28f) };

            Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(temp);
            try
            {
                var sun = new GameObject("PreviewSun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.3f;
                sun.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

                var cam = new GameObject("PreviewCamera").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.fieldOfView = 30f;
                var rt = new RenderTexture(1800, 700, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
                cam.targetTexture = rt;
                cam.aspect = 1800f / 700f;

                for (int w = 0; w < scenes.Length; w++)
                {
                    ForestData s = JsonConfig.FindForest(json, scenes[w]);
                    if (s == null) continue;
                    var root = new GameObject("Trees");
                    var bark = new Material(barkBase); bark.SetColor("_BaseColor", JsonConfig.ParseColor(s.barkColor, Color.gray));
                    var leaves = new Material(leafBase); leaves.SetColor("_BaseColor", JsonConfig.ParseColor(s.leafColor, Color.green));
                    float maxH = 0f;
                    for (int i = 0; i < 3; i++)
                    {
                        var go = new GameObject($"Tree{i}", typeof(MeshFilter), typeof(MeshRenderer));
                        go.transform.SetParent(root.transform);
                        go.transform.position = Far + new Vector3((i - 1) * 9f, 0f, 0f);
                        go.AddComponent<FractalTree>().Generate(s, s.seed * 1000 + i, bark, leaves);
                        go.transform.localScale = Vector3.one; // skip the grow animation
                        maxH = Mathf.Max(maxH, go.GetComponent<MeshRenderer>().bounds.max.y - Far.y);
                    }
                    cam.backgroundColor = backgrounds[w];
                    cam.transform.position = Far + new Vector3(0f, maxH * 0.55f, -30f);
                    cam.transform.LookAt(Far + new Vector3(0f, maxH * 0.45f, 0f));
                    cam.farClipPlane = 80f; // only the preview trees, nothing from the open scenes
                    cam.Render();

                    RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    string file = Path.Combine(outDir, $"fractal_{scenes[w].Substring(scenes[w].IndexOf('_') + 1).ToLowerInvariant()}.png");
                    File.WriteAllBytes(file, tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    Object.DestroyImmediate(root);
                    Debug.Log($"[Coin Rush] Wrote {file}");
                }
                cam.targetTexture = null;
                rt.Release();
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(temp, true);
            }
        }
    }
}
