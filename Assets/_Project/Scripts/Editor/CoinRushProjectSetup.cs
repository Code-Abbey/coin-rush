using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace CoinRush.EditorTools
{
    /// <summary>
    /// One-click project setup: Linear colour space, company/product name, new Input System only,
    /// and VS Code as the C# editor (via Unity's official Visual Studio Editor package).
    /// </summary>
    public static class CoinRushProjectSetup
    {
        private const string IdePackage = "com.unity.ide.visualstudio";
        private const string VsCodePath = "/Applications/Visual Studio Code.app";
        private static AddRequest addRequest;

        [MenuItem("Coin Rush/Apply Recommended Project Settings", priority = 100)]
        public static void Apply()
        {
            // 1) Rendering: URP lighting, HDR bloom and post-processing are designed for Linear space.
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
                Debug.Log("[Coin Rush Setup] Colour space set to Linear.");
            }

            // 2) Identity.
            PlayerSettings.companyName = "Abiodun Adefusi";
            PlayerSettings.productName = "Coin Rush";

            // 3) Input: the game only uses the new Input System (InputReader + InputSystemUIInputModule).
            //    0 = old Input Manager, 1 = Input System package, 2 = both. Applied after an editor restart.
            bool inputChanged = false;
            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length > 0)
            {
                SerializedObject so = new SerializedObject(settings[0]);
                SerializedProperty handler = so.FindProperty("activeInputHandler");
                if (handler != null && handler.intValue != 1)
                {
                    handler.intValue = 1;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    inputChanged = true;
                    Debug.Log("[Coin Rush Setup] Active Input Handling set to 'Input System Package (New)'. Restart Unity to apply.");
                }
            }

            // 4) Code editor: VS Code as the external script editor.
            if (System.IO.Directory.Exists(VsCodePath))
            {
                EditorPrefs.SetString("kScriptsDefaultApp", VsCodePath);
                Debug.Log("[Coin Rush Setup] External script editor set to Visual Studio Code.");
            }

            AssetDatabase.SaveAssets();

            // 5) IDE integration package (IntelliSense / debugging). No version = Unity's recommended one.
            addRequest = Client.Add(IdePackage);
            EditorApplication.update += WaitForPackage;

            if (inputChanged)
                Debug.LogWarning("[Coin Rush Setup] Restart Unity once so the Input System-only setting takes effect.");
        }

        private static void WaitForPackage()
        {
            if (addRequest == null || !addRequest.IsCompleted) return;
            EditorApplication.update -= WaitForPackage;
            if (addRequest.Status == StatusCode.Success)
                Debug.Log($"[Coin Rush Setup] Installed {addRequest.Result.packageId}. Regenerate project files via Preferences › External Tools if needed.");
            else
                Debug.LogWarning($"[Coin Rush Setup] Could not install {IdePackage}: {addRequest.Error?.message}");
            addRequest = null;
        }
    }
}
