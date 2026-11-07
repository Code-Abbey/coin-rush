using System.IO;
using CoinRush.Audio;
using CoinRush.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoinRush.EditorTools
{
    /// <summary>
    /// One-click project generator (menu: Coin Rush > Build Everything).
    /// Creates layers, URP render settings + post-processing volumes, materials (URP Lit + Shader Graph),
    /// ScriptableObjects, sprites import settings, prefabs and all four scenes (MainMenu + 3 worlds).
    ///
    /// Partial files:
    ///   CoinRushBuilder.cs          – entry point, folders, layers, rendering, materials, data, helpers
    ///   CoinRushBuilder.Prefabs.cs  – every prefab (player, enemies, pickups, level pieces, props)
    ///   CoinRushBuilder.Worlds.cs   – level layouts of the three worlds + main menu scene
    ///   CoinRushBuilder.UI.cs       – TextMeshPro HUD, menus and panels
    /// </summary>
    public static partial class CoinRushBuilder
    {
        // ---------- Paths ----------
        // Folder layout:
        //   _Project/Art/{Animation, Materials, Models/Kenney, Shaders, Sprites}
        //   _Project/{Audio, Data, Prefabs, Scenes, Scripts/{Runtime,Editor}, Settings}
        private const string Root = "Assets/_Project";
        private const string ArtDir = Root + "/Art";
        private const string MaterialsDir = ArtDir + "/Materials";
        private const string DataDir = Root + "/Data";
        private const string WorldsDataDir = DataDir + "/Worlds";
        private const string PrefabDir = Root + "/Prefabs";
        private const string AudioDir = Root + "/Audio";
        private const string ScenesDir = Root + "/Scenes";
        private const string ShaderGraphDir = ArtDir + "/Shaders/ShaderGraphs";
        private const string RenderingDir = Root + "/Settings/Rendering";
        private const string InputAssetPath = Root + "/Settings/Input/CoinRushControls.inputactions";
        private const string SpritesDir = ArtDir + "/Sprites/UI";
        private const string HeroPortraitDir = ArtDir + "/Sprites/Heroes";

        private const string SplashScenePath = ScenesDir + "/Splash.unity";
        private const string StoryScenePath = ScenesDir + "/Story.unity";
        private const string MenuScenePath = ScenesDir + "/MainMenu.unity";
        private static readonly string[] WorldScenePaths =
        {
            ScenesDir + "/World1_Meadow.unity",
            ScenesDir + "/World2_Desert.unity",
            ScenesDir + "/World3_Frozen.unity"
        };

        // ---------- Layers ----------
        private const int ObstacleLayer = 8;
        private const int PlayerLayer = 9;
        private const int EnemyLayer = 10;
        private const int CollectibleLayer = 11;
        private const int MinimapLayer = 12;
        private const int HazardLayer = 13;

        // ---------- Generated assets shared between build steps ----------
        private static UniversalRenderPipelineAsset urpAsset;
        private static GameConfig gameConfig;
        private static WorldConfig[] worldConfigs;
        private static PlayerStats playerStats;
        private static EnemyStats blobStats, bruteStats;
        private static CollectibleData coinData, gemData;
        private static RenderTexture minimapTexture;
        private static Material particleMat;

        [MenuItem("Coin Rush/Build Everything", priority = 0)]
        public static void BuildEverything()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Coin Rush] Exit Play mode before building.");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Coin Rush", "Folders, layers and rendering...", 0.05f);
                EnsureFolders();
                AssetDatabase.Refresh();
                CleanupLegacyAssets();
                EnsureLayers();
                SetupRendering();
                ConfigureSpriteImports();
                ConfigureAudioImports();

                EditorUtility.DisplayProgressBar("Coin Rush", "Materials and data...", 0.2f);
                CreateMaterials();
                CreateData();
                RefreshWorldTexts();
                ExportConfig(overwrite: false); // StreamingAssets/Config/coinrush_config.json (kept if it exists)

                EditorUtility.DisplayProgressBar("Coin Rush", "Kenney models, animations and heroes...", 0.28f);
                ConfigureModelImports();
                BuildAnimatorControllers();
                CreateCharacterDatabase();

                EditorUtility.DisplayProgressBar("Coin Rush", "Prefabs...", 0.35f);
                CreatePrefabs();
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayProgressBar("Coin Rush", "Splash screen and story...", 0.45f);
                BuildSplashScene();
                BuildStoryScene();

                EditorUtility.DisplayProgressBar("Coin Rush", "Main menu...", 0.5f);
                BuildMenuScene();
                for (int i = 0; i < worldConfigs.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Coin Rush", $"World {i + 1}: {worldConfigs[i].worldName}...", 0.55f + i * 0.13f);
                    BuildWorldScene(i);
                }

                RegisterScenesInBuildSettings();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SplashScenePath);
            Debug.Log("[Coin Rush] Build complete: Splash + Story + MainMenu + 3 worlds. Press Play in Splash.");
        }

        // =====================================================================
        // Folders, cleanup, layers
        // =====================================================================

        private static void EnsureFolders()
        {
            string[] folders =
            {
                MaterialsDir, MaterialsDir + "/Environment", MaterialsDir + "/Characters", MaterialsDir + "/Pickups",
                MaterialsDir + "/ShaderGraph", MaterialsDir + "/UI", DataDir, WorldsDataDir,
                PrefabDir + "/Player", PrefabDir + "/Enemies", PrefabDir + "/Collectibles", PrefabDir + "/Level",
                PrefabDir + "/Props", PrefabDir + "/VFX", AudioDir + "/SFX", AudioDir + "/Music", ScenesDir,
                RenderingDir, Root + "/Settings/Input", HeroPortraitDir
            };
            foreach (string folder in folders) Directory.CreateDirectory(folder);
        }

        /// <summary>Deletes assets from earlier versions that nothing uses any more (keeps the project lean).</summary>
        private static void CleanupLegacyAssets()
        {
            string[] legacy =
            {
                ScenesDir + "/Game.unity",
                PrefabDir + "/Enemies/Enemy_Blob.prefab", PrefabDir + "/Enemies/Enemy_Brute.prefab",
                PrefabDir + "/Props/Tree.prefab", PrefabDir + "/Props/Rock.prefab", PrefabDir + "/Props/RuinPillar.prefab",
                MaterialsDir + "/Characters/M_Player.mat", MaterialsDir + "/Characters/M_EnemyBlob.mat",
                MaterialsDir + "/Characters/M_EnemyBrute.mat", MaterialsDir + "/Characters/M_Horn.mat",
                MaterialsDir + "/Pickups/M_HealthBox.mat", MaterialsDir + "/Pickups/M_HealthCross.mat",
                MaterialsDir + "/Environment/M_Leaves.mat",
                AudioDir + "/Music/music_loop.wav",
                MaterialsDir + "/UI/TMP_HUD_Outline.mat", MaterialsDir + "/UI/TMP_Title_Thick.mat", // replaced by per-font presets
            };
            foreach (string path in legacy)
                if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            // v1/v2 procedurally generated .wav effects were replaced by the CC0 .ogg packs.
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir + "/SFX" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".wav")) AssetDatabase.DeleteAsset(path);
            }
        }

        private static void EnsureLayers()
        {
            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            SerializedObject tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("layers");

            void SetLayer(int index, string name)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(index);
                if (string.IsNullOrEmpty(layer.stringValue) || layer.stringValue == name) layer.stringValue = name;
                else Debug.LogWarning($"[Coin Rush] Layer {index} is already '{layer.stringValue}', expected '{name}'.");
            }

            SetLayer(ObstacleLayer, "Obstacle");
            SetLayer(PlayerLayer, "Player");
            SetLayer(EnemyLayer, "Enemy");
            SetLayer(CollectibleLayer, "Collectible");
            SetLayer(MinimapLayer, "Minimap");
            SetLayer(HazardLayer, "Hazard");
            SetLayer(HeroStageLayer, "HeroStage");
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            Physics.IgnoreLayerCollision(CollectibleLayer, EnemyLayer, true);
            Physics.IgnoreLayerCollision(CollectibleLayer, ObstacleLayer, true);
            Physics.IgnoreLayerCollision(CollectibleLayer, CollectibleLayer, true);
            Physics.IgnoreLayerCollision(CollectibleLayer, HazardLayer, true);
            Physics.IgnoreLayerCollision(EnemyLayer, HazardLayer, true);
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(MinimapLayer, i, true);
        }

        // =====================================================================
        // Rendering: URP asset, quality, minimap render texture
        // =====================================================================

        private static void SetupRendering()
        {
            // Prefer the URP asset created by hand in Settings/Rendering; otherwise create one.
            urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RenderingDir + "/CoinRush_URP.asset");
            if (urpAsset == null)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { RenderingDir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path == WebUrpPath) continue;
                    urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                    if (urpAsset != null) break;
                }
            }

            if (urpAsset == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RenderingDir + "/CoinRush_URP_Renderer.asset");
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urpAsset, RenderingDir + "/CoinRush_URP.asset");
            }

            urpAsset.supportsHDR = true;
            urpAsset.msaaSampleCount = 4;
            urpAsset.shadowDistance = 90f;
            urpAsset.shadowCascadeCount = 4;
            urpAsset.mainLightShadowmapResolution = 4096;
            EditorUtility.SetDirty(urpAsset);
            AddAmbientOcclusion();

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            UniversalRenderPipelineAsset webAsset = SetupWebRendering();

            // Very Low / Low / Medium use the lighter browser profile; High and above use the full one.
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = i <= WebQualityLevel ? webAsset : urpAsset;
            }
            QualitySettings.SetQualityLevel(current, false);

            // Render texture for the HUD minimap.
            string rtPath = RenderingDir + "/RT_Minimap.renderTexture";
            minimapTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            if (minimapTexture == null)
            {
                minimapTexture = new RenderTexture(512, 512, 16) { name = "RT_Minimap" };
                AssetDatabase.CreateAsset(minimapTexture, rtPath);
            }
            UsePortableFormat(minimapTexture);
        }

        /// <summary>
        /// RGBA8 (sRGB) works on every platform, including browsers. The default format on a Mac is BGRA,
        /// which WebGL cannot render into.
        /// </summary>
        private static void UsePortableFormat(RenderTexture rt)
        {
            const UnityEngine.Experimental.Rendering.GraphicsFormat portable =
                UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB;
            if (rt == null || rt.graphicsFormat == portable) return;
            rt.Release();
            rt.graphicsFormat = portable;
            EditorUtility.SetDirty(rt);
        }

        private const string WebUrpPath = RenderingDir + "/CoinRush_URP_Web.asset";
        private const string WebRendererPath = RenderingDir + "/CoinRush_URP_Web_Renderer.asset";

        /// <summary>Quality level the browser version uses (see CoinRush.Core.WebPerformance).</summary>
        private const int WebQualityLevel = 2;

        /// <summary>
        /// A lighter copy of the URP asset for the browser version: no MSAA, no ambient occlusion and
        /// smaller, shorter-range shadows. Browsers (Safari especially) are much slower than the editor.
        /// </summary>
        private static UniversalRenderPipelineAsset SetupWebRendering()
        {
            var mainRenderer = urpAsset.rendererDataList.Length > 0 ? urpAsset.rendererDataList[0] : null;
            if (AssetDatabase.LoadAssetAtPath<UniversalRendererData>(WebRendererPath) == null && mainRenderer != null)
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(mainRenderer), WebRendererPath);
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(WebUrpPath) == null)
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(urpAsset), WebUrpPath);

            var webRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(WebRendererPath);
            var web = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(WebUrpPath);
            if (webRenderer != null)
            {
                foreach (ScriptableRendererFeature feature in webRenderer.rendererFeatures)
                    if (feature is ScreenSpaceAmbientOcclusion) feature.SetActive(false);
                EditorUtility.SetDirty(webRenderer);

                var so = new SerializedObject(web);
                SerializedProperty list = so.FindProperty("m_RendererDataList");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = webRenderer;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            web.supportsHDR = true;            // keeps the bloom glow
            web.msaaSampleCount = 1;
            web.renderScale = 1f;              // lowered automatically at runtime if the browser struggles
            web.shadowDistance = 50f;
            web.shadowCascadeCount = 2;
            web.mainLightShadowmapResolution = 2048;
            EditorUtility.SetDirty(web);
            return web;
        }

        /// <summary>Adds URP's Screen Space Ambient Occlusion renderer feature (soft contact shadows) once.</summary>
        private static void AddAmbientOcclusion()
        {
            ScriptableRendererData data = urpAsset.rendererDataList.Length > 0 ? urpAsset.rendererDataList[0] : null;
            if (data == null || data.rendererFeatures.Exists(f => f is ScreenSpaceAmbientOcclusion)) return;

            var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            ssao.name = "ScreenSpaceAmbientOcclusion";
            AssetDatabase.AddObjectToAsset(ssao, data);
            data.rendererFeatures.Add(ssao);

            // Keep the renderer's feature map in sync (what the Inspector does when you click "Add Renderer Feature").
            SerializedObject so = new SerializedObject(data);
            SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
            if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out string _, out long localId))
            {
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            data.SetDirty();
            EditorUtility.SetDirty(data);
        }

        /// <summary>Music streams from disk; short effects are decompressed on load for zero latency.</summary>
        private static void ConfigureAudioImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) continue;
                bool music = path.Contains("/Music/");
                AudioImporterSampleSettings s = importer.defaultSampleSettings;
                AudioClipLoadType load = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                if (s.loadType == load && importer.forceToMono == !music) continue;
                s.loadType = load;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = music ? 0.6f : 0.8f;
                importer.defaultSampleSettings = s;
                importer.forceToMono = !music;
                importer.SaveAndReimport();
            }
        }

        /// <summary>Volume profile per world: bloom (makes Shader Graph HDR emission glow), vignette, colour grading.</summary>
        private static VolumeProfile CreateVolumeProfile(string name, Color tint, float saturation, float exposure, float bloom)
        {
            string path = $"{RenderingDir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            Bloom b = profile.Add<Bloom>(true);
            b.intensity.Override(bloom);
            b.threshold.Override(1.0f);
            b.scatter.Override(0.65f);

            Vignette v = profile.Add<Vignette>(true);
            v.intensity.Override(0.28f);
            v.smoothness.Override(0.45f);

            ColorAdjustments c = profile.Add<ColorAdjustments>(true);
            c.colorFilter.Override(tint);
            c.saturation.Override(saturation);
            c.postExposure.Override(exposure);
            c.contrast.Override(8f);

            Tonemapping t = profile.Add<Tonemapping>(true);
            t.mode.Override(TonemappingMode.ACES);

            foreach (VolumeComponent component in profile.components)
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureSpriteImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesDir, HeroPortraitDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;

                bool changed = importer.textureType != TextureImporterType.Sprite;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;

                // 9-slice borders so rounded panels stretch without distorting the corners.
                string file = Path.GetFileNameWithoutExtension(path);
                Vector4 border = file switch
                {
                    "ui_rounded" => new Vector4(30, 30, 30, 30),
                    "ui_rounded_small" => new Vector4(16, 16, 16, 16),
                    "ui_pill" => new Vector4(31, 31, 31, 31),
                    _ => Vector4.zero
                };
                if (importer.spriteBorder != border) { importer.spriteBorder = border; changed = true; }
                if (changed) importer.SaveAndReimport();
            }
        }

        // =====================================================================
        // Materials
        // =====================================================================

        private static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit");
        private static Shader UnlitShader => Shader.Find("Universal Render Pipeline/Unlit");

        /// <summary>URP Lit material (created or updated in place).</summary>
        private static Material Lit(string folder, string name, Color color, float metallic = 0f, float smoothness = 0.3f, Color? emission = null)
        {
            Material mat = LoadOrCreateMaterial($"{MaterialsDir}/{folder}/{name}.mat", LitShader);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else mat.DisableKeyword("_EMISSION");
            mat.enableInstancing = false; // URP SRP Batcher batches these; instancing would conflict with static batching
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>URP Unlit cut-out material with a texture (used for minimap icons).</summary>
        private static Material UnlitIcon(string name, Texture2D texture, Color color)
        {
            Material mat = LoadOrCreateMaterial($"{MaterialsDir}/UI/{name}.mat", UnlitShader);
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Material using one of our Shader Graphs; property values are set by reference name.</summary>
        private static Material GraphMat(string name, string graph, params (string prop, object value)[] values)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>($"{ShaderGraphDir}/{graph}.shadergraph");
            if (shader == null)
            {
                Debug.LogError($"[Coin Rush] Shader Graph '{graph}' failed to load, falling back to URP Lit.");
                shader = LitShader;
            }
            Material mat = LoadOrCreateMaterial($"{MaterialsDir}/ShaderGraph/{name}.mat", shader);
            foreach ((string prop, object value) in values)
            {
                if (!mat.HasProperty(prop)) { Debug.LogWarning($"[Coin Rush] {graph} has no property {prop}"); continue; }
                if (value is Color c) mat.SetColor(prop, c);
                else if (value is float f) mat.SetFloat(prop, f);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material SkyboxMat(string name, Color top, Color horizon, Color bottom, Color sun, float stars)
        {
            Material mat = LoadOrCreateMaterial($"{MaterialsDir}/Environment/{name}.mat", Shader.Find("CoinRush/GradientSkybox"));
            mat.SetColor("_TopColor", top);
            mat.SetColor("_HorizonColor", horizon);
            mat.SetColor("_BottomColor", bottom);
            mat.SetColor("_SunColor", sun);
            mat.SetFloat("_StarDensity", stars);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader) mat.shader = shader;
            return mat;
        }

        private static Sprite SpriteAsset(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/{file}.png");
        private static Texture2D TextureAsset(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{SpritesDir}/{file}.png");

        // =====================================================================
        // ScriptableObjects
        // =====================================================================

        private static void CreateData()
        {
            playerStats = LoadOrCreate<PlayerStats>(DataDir, "PlayerStats", out _);
            blobStats = LoadOrCreate<EnemyStats>(DataDir, "EnemyStats_Hog", out _);

            bruteStats = LoadOrCreate<EnemyStats>(DataDir, "EnemyStats_Orc", out bool newBrute);
            if (newBrute) // defaults only on creation, so Inspector tweaks survive rebuilds
            {
                bruteStats.patrolSpeed = 1.8f;
                bruteStats.chaseSpeed = 3.9f;
                bruteStats.sightRange = 12f;
                bruteStats.hearingRange = 4.5f;
                bruteStats.fieldOfView = 100f;
                bruteStats.damage = 25f;
                bruteStats.attackCooldown = 2f;
                bruteStats.attackRange = 1.9f;
                bruteStats.knockbackForce = 20f;
                EditorUtility.SetDirty(bruteStats);
            }

            coinData = LoadOrCreate<CollectibleData>(DataDir, "CollectibleData_Coin", out _);
            coinData.displayName = "Coin";
            coinData.scoreValue = 1;
            coinData.pickupSound = SoundId.CoinPickup;
            coinData.effectColor = new Color(1f, 0.84f, 0.25f);
            EditorUtility.SetDirty(coinData);

            gemData = LoadOrCreate<CollectibleData>(DataDir, "CollectibleData_Gem", out _);
            gemData.displayName = "Gem";
            gemData.scoreValue = 5;
            gemData.pickupSound = SoundId.GemPickup;
            gemData.effectColor = new Color(0.45f, 0.95f, 1f);
            EditorUtility.SetDirty(gemData);

            worldConfigs = new WorldConfig[3];

            WorldConfig w = worldConfigs[0] = LoadOrCreate<WorldConfig>(WorldsDataDir, "World1_Meadow", out bool isNew);
            if (isNew)
            {
                w.worldName = "Sunny Meadow"; w.subtitle = "Rolling hills, bouncy jump pads and grumpy wild hogs";
                w.sceneName = "World1_Meadow"; w.coinQuota = 30; w.timeLimit = 150f;
                w.maxActiveCoins = 26; w.gemChance = 0.12f;
                w.startingEnemies = 3; w.maxEnemies = 7; w.scorePerNewEnemy = 12; w.bruteChance = 0f;
                w.powerUpSpawnInterval = 9f; w.maxPowerUpsOnField = 3;
                w.themeColor = new Color(0.45f, 0.9f, 0.4f);
                w.skyTop = new Color(0.22f, 0.48f, 0.92f); w.skyHorizon = new Color(0.78f, 0.9f, 1f);
                w.fogColor = new Color(0.75f, 0.85f, 0.95f); w.musicPitch = 1f;
                EditorUtility.SetDirty(w);
            }

            w = worldConfigs[1] = LoadOrCreate<WorldConfig>(WorldsDataDir, "World2_Desert", out isNew);
            if (isNew)
            {
                w.worldName = "Desert Ruins"; w.subtitle = "Ancient ruins, lava rivers and spinning blades";
                w.sceneName = "World2_Desert"; w.coinQuota = 45; w.timeLimit = 170f;
                w.maxActiveCoins = 28; w.gemChance = 0.16f;
                w.startingEnemies = 4; w.maxEnemies = 9; w.scorePerNewEnemy = 12; w.bruteChance = 0.3f;
                w.powerUpSpawnInterval = 9f; w.maxPowerUpsOnField = 3;
                w.themeColor = new Color(1f, 0.62f, 0.25f);
                w.skyTop = new Color(0.35f, 0.6f, 0.9f); w.skyHorizon = new Color(1f, 0.88f, 0.7f);
                w.fogColor = new Color(0.95f, 0.86f, 0.72f); w.musicPitch = 0.94f;
                EditorUtility.SetDirty(w);
            }

            w = worldConfigs[2] = LoadOrCreate<WorldConfig>(WorldsDataDir, "World3_Frozen", out isNew);
            if (isNew)
            {
                w.worldName = "Frozen Night"; w.subtitle = "Slippery ice beneath a sky full of stars";
                w.sceneName = "World3_Frozen"; w.coinQuota = 60; w.timeLimit = 185f;
                w.maxActiveCoins = 30; w.gemChance = 0.2f;
                w.startingEnemies = 5; w.maxEnemies = 11; w.scorePerNewEnemy = 12; w.bruteChance = 0.45f;
                w.powerUpSpawnInterval = 8f; w.maxPowerUpsOnField = 4;
                w.themeColor = new Color(0.55f, 0.85f, 1f);
                w.skyTop = new Color(0.02f, 0.03f, 0.12f); w.skyHorizon = new Color(0.15f, 0.22f, 0.4f);
                w.fogColor = new Color(0.12f, 0.17f, 0.3f); w.musicPitch = 1.06f;
                EditorUtility.SetDirty(w);
            }

            gameConfig = LoadOrCreate<GameConfig>(DataDir, "GameConfig", out _);
            gameConfig.worlds = worldConfigs;
            EditorUtility.SetDirty(gameConfig);
        }

        /// <summary>World names / subtitles are story text: always refreshed (balance values are left as tuned).</summary>
        private static void RefreshWorldTexts()
        {
            string[] subtitles =
            {
                "Rolling hills, bouncy jump pads and grumpy wild hogs",
                "Ancient ruins, lava rivers and spinning blades",
                "Slippery ice beneath a sky full of stars",
            };
            for (int i = 0; i < worldConfigs.Length && i < subtitles.Length; i++)
            {
                worldConfigs[i].subtitle = subtitles[i];
                EditorUtility.SetDirty(worldConfigs[i]);
            }
        }

        private static T LoadOrCreate<T>(string dir, string name, out bool isNew) where T : ScriptableObject
        {
            string path = $"{dir}/{name}.asset";
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            isNew = asset == null;
            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        // =====================================================================
        // Build settings
        // =====================================================================

        private static void RegisterScenesInBuildSettings()
        {
            // Build index 0 is the first scene a player sees: Splash -> Story -> MainMenu -> worlds.
            var scenes = new EditorBuildSettingsScene[3 + WorldScenePaths.Length];
            scenes[0] = new EditorBuildSettingsScene(SplashScenePath, true);
            scenes[1] = new EditorBuildSettingsScene(StoryScenePath, true);
            scenes[2] = new EditorBuildSettingsScene(MenuScenePath, true);
            for (int i = 0; i < WorldScenePaths.Length; i++) scenes[i + 3] = new EditorBuildSettingsScene(WorldScenePaths[i], true);
            EditorBuildSettings.scenes = scenes;
        }

        // =====================================================================
        // Generic helpers
        // =====================================================================

        private static GameObject Empty(string name, Transform parent, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        /// <summary>Primitive used as a visual. Its auto-added collider is removed unless keepCollider.</summary>
        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos,
            Vector3 localScale, Material mat, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                if (child.gameObject.layer != MinimapLayer) SetLayerRecursive(child.gameObject, layer);
        }

        /// <summary>Assigns a private [SerializeField] object reference, exactly like dragging it in the Inspector.</summary>
        private static void Set(Object target, string property, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null) { Debug.LogError($"[Coin Rush] '{property}' not found on {target.GetType().Name}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string property, int value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null) { Debug.LogError($"[Coin Rush] '{property}' not found on {target.GetType().Name}"); return; }
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string property, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null) { Debug.LogError($"[Coin Rush] '{property}' not found on {target.GetType().Name}"); return; }
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector3(Object target, string property, Vector3 value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null) { Debug.LogError($"[Coin Rush] '{property}' not found on {target.GetType().Name}"); return; }
            prop.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectList(Object target, string property, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
