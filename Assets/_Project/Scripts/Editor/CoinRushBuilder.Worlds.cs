using System.Collections.Generic;
using System.Linq;
using CoinRush.Audio;
using CoinRush.CameraSystem;
using CoinRush.Collectibles;
using CoinRush.Core;
using CoinRush.Data;
using CoinRush.Enemies;
using CoinRush.InputHandling;
using CoinRush.Level;
using CoinRush.Spawning;
using CoinRush.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CoinRush.EditorTools
{
    /// <summary>Scene generation: shared world skeleton + hand-designed layouts for each world, and the main menu.</summary>
    public static partial class CoinRushBuilder
    {
        private const float ArenaHalf = 50f; // 100 x 100 m worlds (v1 was 50 x 50)

        private static Transform layoutRoot;
        private static Transform levelCoins;

        // =====================================================================
        // Shared world skeleton
        // =====================================================================

        private static void BuildWorldScene(int index)
        {
            WorldConfig world = worldConfigs[index];
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            WorldLook look = GetLook(index);
            SetupEnvironmentLighting(world, look);

            // ---------------- [Managers] ----------------
            Transform managers = new GameObject("[Managers]").transform;
            GameManager gm = Empty("GameManager", managers, Vector3.zero).AddComponent<GameManager>();
            Set(gm, "gameConfig", gameConfig);
            Set(gm, "world", world);
            SetInt(gm, "worldIndex", index);
            SetupAudioManager(Empty("AudioManager", managers, Vector3.zero).AddComponent<AudioManager>(), $"music_world{index + 1}");
            InputReader input = Empty("InputReader", managers, Vector3.zero).AddComponent<InputReader>();
            Set(input, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath));
            managers.Find("InputReader").gameObject.AddComponent<DevCheats>();

            // ---------------- [Environment] ----------------
            Transform environment = new GameObject("[Environment]").transform;
            CreateSun(environment, look.sunColor, look.sunIntensity, look.sunRotation);
            CreateGlobalVolume(environment, CreateVolumeProfile($"PP_{world.sceneName}", look.grade, look.saturation, look.exposure, look.bloom));

            Transform arena = Empty("Arena", environment, Vector3.zero).transform;
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(arena, false);
            ground.transform.localScale = new Vector3(ArenaHalf / 5f, 1f, ArenaHalf / 5f);
            ground.GetComponent<Renderer>().sharedMaterial = look.ground;
            ground.isStatic = true;
            BuildBoundary(arena, look.wall);

            layoutRoot = Empty("Layout", arena, Vector3.zero).transform;
            levelCoins = null;

            // ---------------- [Gameplay] ----------------
            Transform gameplay = new GameObject("[Gameplay]").transform;
            levelCoins = Empty("LevelCoins", gameplay, Vector3.zero).transform;
            Transform coinContainer = Empty("SpawnedCoins", gameplay, Vector3.zero).transform;
            Transform enemyContainer = Empty("Enemies", gameplay, Vector3.zero).transform;
            Transform powerUpContainer = Empty("PowerUps", gameplay, Vector3.zero).transform;

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetParent(gameplay, false);
            player.transform.position = new Vector3(0f, 0.05f, -40f);

            GameObject portal = Place(portalPrefab, gameplay, new Vector3(0f, 0f, 43f), 0f);
            portal.name = "Portal";
            Place(flagPrefab, layoutRoot, new Vector3(-4.5f, 0f, 42f), 0f);
            Place(flagPrefab, layoutRoot, new Vector3(4.5f, 0f, 42f), 180f);

            // World-specific layout (obstacles, hazards, platforms, coin trails).
            switch (index)
            {
                case 0: LayoutMeadow(); break;
                case 1: LayoutDesert(); break;
                default: LayoutFrozen(); break;
            }

            BuildAtmosphere(index, environment);

            // ---------------- [Spawning] ----------------
            Transform spawning = new GameObject("[Spawning]").transform;
            SpawnArea area = Empty("SpawnArea", spawning, Vector3.zero).AddComponent<SpawnArea>();
            SerializedObject areaSo = new SerializedObject(area);
            areaSo.FindProperty("size").vector2Value = new Vector2(ArenaHalf * 2f - 8f, ArenaHalf * 2f - 8f);
            areaSo.FindProperty("blockingLayers").intValue = (1 << ObstacleLayer) | (1 << HazardLayer);
            areaSo.FindProperty("clearanceRadius").floatValue = 1.2f;
            areaSo.ApplyModifiedPropertiesWithoutUndo();

            CoinSpawner coinSpawner = Empty("CoinSpawner", spawning, Vector3.zero).AddComponent<CoinSpawner>();
            Set(coinSpawner, "spawnArea", area);
            Set(coinSpawner, "coinPrefab", coinPrefab.GetComponent<Coin>());
            Set(coinSpawner, "gemPrefab", gemPrefab.GetComponent<Coin>());
            Set(coinSpawner, "container", coinContainer);

            EnemySpawner enemySpawner = Empty("EnemySpawner", spawning, Vector3.zero).AddComponent<EnemySpawner>();
            Set(enemySpawner, "spawnArea", area);
            Set(enemySpawner, "enemyPrefab", blobPrefab.GetComponent<EnemyBehaviour>());
            if (world.bruteChance > 0f) Set(enemySpawner, "brutePrefab", brutePrefab.GetComponent<EnemyBehaviour>());
            Set(enemySpawner, "player", player.transform);
            Set(enemySpawner, "container", enemyContainer);

            PowerUpSpawner powerUps = Empty("PowerUpSpawner", spawning, Vector3.zero).AddComponent<PowerUpSpawner>();
            Set(powerUps, "spawnArea", area);
            Set(powerUps, "container", powerUpContainer);
            SetObjectList(powerUps, "powerUpPrefabs", new Object[]
            {
                healthPrefab.GetComponent<Pickup>(), speedPrefab.GetComponent<Pickup>(),
                magnetPrefab.GetComponent<Pickup>(), shieldPrefab.GetComponent<Pickup>()
            });

            // ---------------- [Cameras] ----------------
            Transform cameras = new GameObject("[Cameras]").transform;
            Camera mainCam = CreateMainCamera(cameras, player.transform);
            Set(player.GetComponent<Player.PlayerController>(), "cameraTransform", mainCam.transform);
            CreateMinimapCamera(cameras, player.transform, mainCam.transform);

            // ---------------- [UI] ----------------
            BuildGameUI(world);

            EditorSceneManager.SaveScene(scene, WorldScenePaths[index]);
        }

        private struct WorldLook
        {
            public Material ground, wall, sky;
            public Color sunColor, grade, ambientSky, ambientEquator, ambientGround;
            public float sunIntensity, saturation, exposure, bloom;
            public Vector3 sunRotation;
        }

        private static WorldLook GetLook(int index)
        {
            WorldConfig w = worldConfigs[index];
            switch (index)
            {
                case 0:
                    return new WorldLook
                    {
                        ground = Lit("Environment", "M_Ground_Meadow", new Color(0.24f, 0.44f, 0.22f), 0f, 0.1f),
                        wall = Lit("Environment", "M_Wall_Meadow", new Color(0.6f, 0.55f, 0.48f), 0f, 0.2f),
                        sky = SkyboxMat("SKY_Meadow", w.skyTop, w.skyHorizon, new Color(0.35f, 0.42f, 0.35f), new Color(1f, 0.95f, 0.8f), 0f),
                        sunColor = new Color(1f, 0.96f, 0.88f), sunIntensity = 1.3f, sunRotation = new Vector3(50f, -35f, 0f),
                        grade = new Color(1f, 1f, 0.97f), saturation = 4f, exposure = -0.1f, bloom = 0.8f,
                        ambientSky = new Color(0.55f, 0.65f, 0.85f), ambientEquator = new Color(0.45f, 0.5f, 0.5f), ambientGround = new Color(0.22f, 0.26f, 0.2f)
                    };
                case 1:
                    return new WorldLook
                    {
                        ground = Lit("Environment", "M_Ground_Desert", new Color(0.86f, 0.72f, 0.5f), 0f, 0.1f),
                        wall = mSandDark,
                        sky = SkyboxMat("SKY_Desert", w.skyTop, w.skyHorizon, new Color(0.6f, 0.45f, 0.3f), new Color(1f, 0.8f, 0.5f), 0f),
                        sunColor = new Color(1f, 0.85f, 0.65f), sunIntensity = 1.5f, sunRotation = new Vector3(35f, 60f, 0f),
                        grade = new Color(1f, 0.98f, 0.94f), saturation = -4f, exposure = -0.15f, bloom = 0.9f,
                        ambientSky = new Color(0.7f, 0.66f, 0.62f), ambientEquator = new Color(0.55f, 0.48f, 0.4f), ambientGround = new Color(0.32f, 0.26f, 0.2f)
                    };
                default:
                    return new WorldLook
                    {
                        ground = mSnow,
                        wall = Lit("Environment", "M_Wall_Frozen", new Color(0.55f, 0.65f, 0.8f), 0.1f, 0.6f),
                        sky = SkyboxMat("SKY_Frozen", w.skyTop, w.skyHorizon, new Color(0.08f, 0.1f, 0.18f), new Color(0.75f, 0.85f, 1f), 0.6f),
                        sunColor = new Color(0.6f, 0.72f, 1f), sunIntensity = 0.7f, sunRotation = new Vector3(40f, 150f, 0f),
                        grade = new Color(0.9f, 0.95f, 1.05f), saturation = 5f, exposure = 0.35f, bloom = 1.4f,
                        ambientSky = new Color(0.3f, 0.38f, 0.6f), ambientEquator = new Color(0.2f, 0.25f, 0.4f), ambientGround = new Color(0.15f, 0.17f, 0.25f)
                    };
            }
        }

        private static void SetupEnvironmentLighting(WorldConfig world, WorldLook look)
        {
            RenderSettings.skybox = look.sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = look.ambientSky;
            RenderSettings.ambientEquatorColor = look.ambientEquator;
            RenderSettings.ambientGroundColor = look.ambientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = world.fogColor;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 160f;
        }

        private static void CreateSun(Transform parent, Color color, float intensity, Vector3 rotation)
        {
            GameObject sun = Empty("Directional Light", parent, new Vector3(0f, 20f, 0f));
            sun.transform.rotation = Quaternion.Euler(rotation);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;
        }

        private static void CreateGlobalVolume(Transform parent, VolumeProfile profile)
        {
            Volume volume = Empty("Global Volume", parent, Vector3.zero).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        private static void BuildBoundary(Transform arena, Material wallMat)
        {
            Transform walls = Empty("Walls", arena, Vector3.zero).transform;
            const float height = 3f, thick = 1.2f;
            float len = ArenaHalf * 2f + thick;
            Solid(PrimitiveType.Cube, "Wall_North", walls, new Vector3(0f, height / 2f, ArenaHalf), new Vector3(len, height, thick), wallMat);
            Solid(PrimitiveType.Cube, "Wall_South", walls, new Vector3(0f, height / 2f, -ArenaHalf), new Vector3(len, height, thick), wallMat);
            Solid(PrimitiveType.Cube, "Wall_East", walls, new Vector3(ArenaHalf, height / 2f, 0f), new Vector3(thick, height, len), wallMat);
            Solid(PrimitiveType.Cube, "Wall_West", walls, new Vector3(-ArenaHalf, height / 2f, 0f), new Vector3(thick, height, len), wallMat);
        }

        private static Camera CreateMainCamera(Transform parent, Transform player)
        {
            GameObject camGo = Empty("Main Camera", parent, new Vector3(0f, 7f, -48f));
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 58f;
            cam.farClipPlane = 250f;
            cam.cullingMask = ~(1 << MinimapLayer);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraShake>();
            ThirdPersonCamera tpc = camGo.AddComponent<ThirdPersonCamera>();
            Set(tpc, "target", player);
            SetInt(tpc, "collisionMask", (1 << 0) | (1 << ObstacleLayer));
            return cam;
        }

        private static void CreateMinimapCamera(Transform parent, Transform player, Transform view)
        {
            GameObject go = Empty("Minimap Camera", parent, new Vector3(0f, 60f, -40f));
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.14f, 1f);
            cam.cullingMask = (1 << 0) | (1 << ObstacleLayer) | (1 << HazardLayer) | (1 << MinimapLayer);
            cam.targetTexture = minimapTexture;
            cam.farClipPlane = 120f;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            MinimapCamera mm = go.AddComponent<MinimapCamera>();
            Set(mm, "target", player);
            Set(mm, "viewTransform", view);
        }

        /// <summary>
        /// Wires the AudioManager: CC0 music by Juhani Junkala (one track per scene + victory) and
        /// CC0 effects (Juhani Junkala 512 SFX collection, Kenney Interface Sounds).
        /// </summary>
        private static void SetupAudioManager(AudioManager manager, string musicFile)
        {
            var map = new List<(SoundId id, string file, float volume, float pitchVar)>
            {
                (SoundId.CoinPickup, "coin_pickup", 0.55f, 0f), (SoundId.GemPickup, "gem_pickup", 0.7f, 0f),
                (SoundId.PowerUp, "powerup", 0.7f, 0f), (SoundId.Heal, "heal", 0.7f, 0f),
                (SoundId.PlayerHurt, "player_hurt", 0.85f, 0.05f), (SoundId.Jump, "jump", 0.4f, 0.06f),
                (SoundId.Land, "land", 0.45f, 0.08f), (SoundId.Footstep, "footstep", 0.22f, 0.12f),
                (SoundId.EnemyAlert, "enemy_alert", 0.45f, 0.05f), (SoundId.EnemyAttack, "enemy_attack", 0.6f, 0.08f),
                (SoundId.CountdownBeep, "countdown_beep", 0.6f, 0f), (SoundId.CountdownGo, "countdown_go", 0.7f, 0f),
                (SoundId.Win, "win", 0.8f, 0f), (SoundId.Lose, "lose", 0.8f, 0f),
                (SoundId.PortalOpen, "portal_open", 0.8f, 0f), (SoundId.PortalEnter, "portal_enter", 0.8f, 0f),
                (SoundId.Dash, "dash", 0.6f, 0.08f), (SoundId.JumpPad, "jump_pad", 0.7f, 0.05f),
                (SoundId.ShieldBlock, "shield_block", 0.75f, 0f), (SoundId.Lava, "lava", 0.5f, 0.1f),
                (SoundId.UIClick, "ui_click", 0.6f, 0f), (SoundId.UIHover, "ui_hover", 0.35f, 0.04f),
                (SoundId.UIConfirm, "ui_confirm", 0.6f, 0f),
            };

            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("musicClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/Music/{musicFile}.ogg");
            so.FindProperty("victoryClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/Music/music_victory.ogg");
            so.FindProperty("musicVolume").floatValue = 0.45f;
            SerializedProperty sounds = so.FindProperty("sounds");
            sounds.arraySize = map.Count;
            for (int i = 0; i < map.Count; i++)
            {
                SerializedProperty entry = sounds.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").enumValueIndex = (int)map[i].id;
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/SFX/{map[i].file}.ogg");
                if (clip == null) Debug.LogWarning($"[Coin Rush] Missing SFX {map[i].file}.ogg");
                entry.FindPropertyRelative("clip").objectReferenceValue = clip;
                entry.FindPropertyRelative("volume").floatValue = map[i].volume;
                entry.FindPropertyRelative("pitchVariation").floatValue = map[i].pitchVar;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // Atmosphere: drifting clouds + ambient particles (fireflies, dust, snow)
        // =====================================================================

        private static void BuildAtmosphere(int index, Transform environment)
        {
            Transform atmo = Empty("Atmosphere", environment, Vector3.zero).transform;
            if (index == 0)
            {
                BuildClouds(atmo, 10, 70f, 38f, new Color(1f, 1f, 1f));
                MakeParticles("Fireflies", atmo, new Vector3(0f, 2.5f, 0f), new Color(1f, 0.95f, 0.5f), true, 35f, 0, 0.35f, 7f, 0.12f,
                    ParticleSystemShapeType.Box, 1f);
                ScaleBoxShape(atmo.Find("Fireflies"), new Vector3(90f, 4f, 90f));
            }
            else if (index == 1)
            {
                BuildClouds(atmo, 6, 75f, 42f, new Color(1f, 0.95f, 0.88f));
                MakeParticles("DustMotes", atmo, new Vector3(0f, 2f, 0f), new Color(1f, 0.85f, 0.6f, 0.6f), true, 60f, 0, 1.2f, 6f, 0.1f,
                    ParticleSystemShapeType.Box, 1f);
                ScaleBoxShape(atmo.Find("DustMotes"), new Vector3(90f, 3f, 90f));
            }
        }

        private static void ScaleBoxShape(Transform t, Vector3 size)
        {
            if (t == null) return;
            ParticleSystem.ShapeModule shape = t.GetComponent<ParticleSystem>().shape;
            shape.scale = size;
        }

        /// <summary>Low-poly puffy clouds (sphere clusters) on a slowly rotating ring.</summary>
        private static void BuildClouds(Transform parent, int count, float radius, float height, Color tint)
        {
            Material cloudMat = Lit("Environment", "M_Cloud", Color.white, 0f, 0f, new Color(0.35f, 0.35f, 0.38f));
            GameObject ring = Empty("Clouds", parent, Vector3.zero);
            SetVector3(ring.AddComponent<Rotator>(), "degreesPerSecond", new Vector3(0f, 1.2f, 0f));

            Random.State state = Random.state;
            Random.InitState(555 + count);
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float r = radius * Random.Range(0.6f, 1f);
                Transform cloud = Empty($"Cloud_{i + 1:00}", ring.transform, new Vector3(Mathf.Cos(a) * r, height + Random.Range(-4f, 4f), Mathf.Sin(a) * r)).transform;
                int puffs = Random.Range(3, 6);
                for (int p = 0; p < puffs; p++)
                {
                    float s = Random.Range(5f, 9f);
                    GameObject puff = Primitive(PrimitiveType.Sphere, "Puff", cloud,
                        new Vector3((p - puffs / 2f) * s * 0.6f, Random.Range(-0.8f, 1.2f), Random.Range(-2f, 2f)),
                        new Vector3(s * 1.2f, s * 0.7f, s), cloudMat);
                    Renderer rend = puff.GetComponent<Renderer>();
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rend.receiveShadows = false;
                }
            }
            Random.state = state;
        }

        // =====================================================================
        // Layout helpers
        // =====================================================================

        /// <summary>Jump pad that launches the player along an arc onto <paramref name="landing"/> (world space).</summary>
        private static void PadTo(Vector3 padPos, Vector3 landing, float arcHeight = 2.5f)
        {
            GameObject pad = Place(jumpPadPrefab, layoutRoot, padPos, 0f);
            Vector3 flat = new Vector3(landing.x - padPos.x, 0f, landing.z - padPos.z);
            if (flat.sqrMagnitude > 0.01f) pad.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            Transform target = Empty("LandingTarget", pad.transform, Vector3.zero).transform;
            target.position = landing;
            SerializedObject so = new SerializedObject(pad.GetComponent<JumpPad>());
            so.FindProperty("landingTarget").objectReferenceValue = target;
            so.FindProperty("arcHeight").floatValue = arcHeight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Place(GameObject prefab, Transform parent, Vector3 position, float yaw, Vector3? scale = null)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent != null ? parent : layoutRoot, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (scale.HasValue) go.transform.localScale = scale.Value;
            return go;
        }

        private static void CoinLine(Vector3 from, Vector3 to, int count, bool gems = false)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                Place(gems ? gemPrefab : coinPrefab, levelCoins, Vector3.Lerp(from, to, t), 0f);
            }
        }

        private static void CoinCircle(Vector3 center, float radius, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                Place(coinPrefab, levelCoins, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius), 0f);
            }
        }

        private static GameObject Block(string name, Vector3 pos, Vector3 scale, Material mat, float yaw = 0f)
        {
            GameObject go = Solid(PrimitiveType.Cube, name, layoutRoot, pos, scale, mat);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        /// <summary>Walkable ramp from ground to 'height' along +Z (rotate with yaw).</summary>
        private static GameObject Ramp(string name, Vector3 bottomCenter, float height, float length, float width, float yaw, Material mat)
        {
            float angle = Mathf.Atan2(height, length) * Mathf.Rad2Deg;
            float slopeLen = Mathf.Sqrt(height * height + length * length);
            GameObject root = Empty(name, layoutRoot, bottomCenter);
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            GameObject slab = Solid(PrimitiveType.Cube, "Slab", root.transform, new Vector3(0f, height / 2f, length / 2f),
                new Vector3(width, 0.4f, slopeLen), mat);
            slab.transform.localRotation = Quaternion.Euler(-angle, 0f, 0f);
            Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            walkways.Add((bottomCenter - fwd * 1.5f, bottomCenter + fwd * (length + 1.2f))); // keep trees off the ramp's path
            return root;
        }

        private static void MovingPlatformAt(Vector3 start, Vector3 travel, float cycle, float phase, Vector3 size)
        {
            GameObject p = Place(platformPrefab, layoutRoot, start, 0f, size);
            MovingPlatform mp = p.GetComponent<MovingPlatform>();
            SetVector3(mp, "travel", travel);
            SetFloat(mp, "cycleSeconds", cycle);
            SetFloat(mp, "phaseOffset", phase);
        }

        private static void Pool(GameObject prefab, Vector3 center, Vector3 size)
        {
            GameObject p = Place(prefab, layoutRoot, center, 0f, new Vector3(size.x, size.y, size.z));
            p.isStatic = true;
        }

        // =====================================================================
        // WORLD 1 – Sunny Meadow: central plateau with ramps, jump pads to sky islands,
        // moving platforms, tree groves and a windmill.
        // =====================================================================

        private static void LayoutMeadow()
        {
            // Central plateau (height 2.5) with two ramps.
            Block("Plateau", new Vector3(0f, 1.25f, 0f), new Vector3(14f, 2.5f, 14f), mPlatform);
            Ramp("Ramp_South", new Vector3(0f, 0f, -17f), 2.5f, 10f, 5f, 0f, mPlatform);
            Ramp("Ramp_North", new Vector3(0f, 0f, 17f), 2.5f, 10f, 5f, 180f, mPlatform);
            CoinCircle(new Vector3(0f, 2.5f, 0f), 4.5f, 10);
            CoinLine(new Vector3(0f, 0.6f, -16f), new Vector3(0f, 2.4f, -8f), 4);

            // Sky islands reached by jump pads, each holding gems.
            Vector3[] islands = { new Vector3(-30f, 4.2f, -18f), new Vector3(30f, 4.2f, -18f), new Vector3(-30f, 4.2f, 22f), new Vector3(30f, 4.2f, 22f) };
            foreach (Vector3 island in islands)
            {
                Block("SkyIsland", island, new Vector3(7f, 0.6f, 7f), mPlatform);
                Primitive(PrimitiveType.Sphere, "IslandRock", layoutRoot, island + Vector3.down * 1.2f, new Vector3(6f, 2.4f, 6f), mRock);
                Vector3 padPos = new Vector3(island.x * 0.78f, 0f, island.z);
                PadTo(padPos, island + new Vector3(0f, 0.3f, 0f)); // lands in the middle of the gem row
                CoinLine(island + new Vector3(-1.5f, 0.3f, 0f), island + new Vector3(1.5f, 0.3f, 0f), 3, gems: true);
            }

            // Moving platforms shuttle between the plateau and two side ledges.
            Block("Ledge_East", new Vector3(26f, 1.25f, 2f), new Vector3(6f, 2.5f, 6f), mPlatform);
            Block("Ledge_West", new Vector3(-26f, 1.25f, 2f), new Vector3(6f, 2.5f, 6f), mPlatform);
            MovingPlatformAt(new Vector3(9.5f, 2.2f, 2f), new Vector3(11f, 0f, 0f), 6f, 0f, new Vector3(4f, 0.6f, 4f));
            MovingPlatformAt(new Vector3(-9.5f, 2.2f, 2f), new Vector3(-11f, 0f, 0f), 6f, 0.5f, new Vector3(4f, 0.6f, 4f));
            CoinLine(new Vector3(12f, 3.3f, 2f), new Vector3(20f, 3.3f, 2f), 5);
            CoinLine(new Vector3(-12f, 3.3f, 2f), new Vector3(-20f, 3.3f, 2f), 5);
            CoinCircle(new Vector3(26f, 2.5f, 2f), 1.6f, 6);
            CoinCircle(new Vector3(-26f, 2.5f, 2f), 1.6f, 6);

            // Tree groves and rocks (deterministic pseudo-random scatter).
            Random.State state = Random.state;
            Random.InitState(1234);
            Transform grove = Empty("Trees", layoutRoot, Vector3.zero).transform;
            for (int i = 0; i < 26; i++)
            {
                Vector3 p = new Vector3(Random.Range(-45f, 45f), 0f, Random.Range(-45f, 45f));
                if (Mathf.Abs(p.x) < 18f && Mathf.Abs(p.z) < 26f) continue;   // keep the centre open
                if (Mathf.Abs(p.x) < 6f) continue;                               // keep the main path open
                if (p.z > 36f && Mathf.Abs(p.x) < 10f) continue;                 // portal area
                bool nearIsland = false;
                foreach (Vector3 isl in islands) if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(isl.x, isl.z)) < 9f) nearIsland = true;
                if (nearIsland) continue;
                GameObject t = Place(i % 3 == 0 ? Pick(rocks) : Pick(meadowTrees), grove, p, Random.Range(0f, 360f));
                float s = Random.Range(0.85f, 1.25f);
                t.transform.localScale = Vector3.one * s;
            }
            Random.state = state;

            // Fences (jumpable) and crates.
            Block("Fence_A", new Vector3(-14f, 0.55f, -28f), new Vector3(10f, 1.1f, 0.6f), mTrunk, 15f);
            Block("Fence_B", new Vector3(14f, 0.55f, -28f), new Vector3(10f, 1.1f, 0.6f), mTrunk, -15f);
            Block("Fence_C", new Vector3(-16f, 0.55f, 32f), new Vector3(10f, 1.1f, 0.6f), mTrunk, -10f);
            Block("Fence_D", new Vector3(16f, 0.55f, 32f), new Vector3(10f, 1.1f, 0.6f), mTrunk, 10f);
            Place(cratePrefab, layoutRoot, new Vector3(7f, 0f, -24f), 20f);
            Place(cratePrefab, layoutRoot, new Vector3(-7f, 0f, -22f), -10f);
            Place(cratePrefab, layoutRoot, new Vector3(-8f, 0f, 36f), 35f);
            CoinLine(new Vector3(-20f, 0f, -36f), new Vector3(20f, 0f, -36f), 9);

            // Procedural L-system trees (grown at runtime, style from coinrush_config.json).
            FractalGrove(0, p => !(Mathf.Abs(p.x) < 18f && Mathf.Abs(p.z) < 26f) && Mathf.Abs(p.x) > 6f &&
                                 islands.All(isl => new Vector2(p.x - isl.x, p.z - isl.z).magnitude > 8f));

            // Kenney flowers, grass, bushes and mushrooms everywhere except the paths.
            ScatterDecor(smallDecor, 260, 77, p => Mathf.Abs(p.x) > 4f && !(Mathf.Abs(p.x) < 9f && Mathf.Abs(p.z) < 22f));

            // Little camp next to the start.
            Place(campfirePrefab, layoutRoot, new Vector3(-12f, 0f, -42f), 0f);
            Place(tentPrefab, layoutRoot, new Vector3(-16f, 0f, -44f), 30f);
            Place(logStackPrefab, layoutRoot, new Vector3(-9f, 0f, -45f), -20f);
            Place(signPrefab, layoutRoot, new Vector3(3.5f, 0f, -36f), 200f);
            Light fireLight = Empty("CampfireLight", layoutRoot, new Vector3(-12f, 1f, -42f)).AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.6f, 0.25f);
            fireLight.range = 8f;
            fireLight.intensity = 3f;
            MakeParticles("CampfireSparks", layoutRoot, new Vector3(-12f, 0.4f, -42f), new Color(1f, 0.6f, 0.2f),
                true, 12f, 0, 1.2f, 1.2f, 0.12f, ParticleSystemShapeType.Cone, 0.2f, -0.2f);

            // Windmill landmark (decorative Rotator).
            Transform mill = Empty("Windmill", layoutRoot, new Vector3(38f, 0f, -38f)).transform;
            Solid(PrimitiveType.Cylinder, "Tower", mill, new Vector3(0f, 4f, 0f), new Vector3(3f, 4f, 3f), mWhite);
            Primitive(PrimitiveType.Cube, "Roof", mill, new Vector3(0f, 8.5f, 0f), new Vector3(3.4f, 1.2f, 3.4f), mRoof);
            GameObject hub = Empty("Blades", mill, new Vector3(0f, 7f, -1.8f));
            hub.AddComponent<Rotator>();
            SetVector3(hub.GetComponent<Rotator>(), "degreesPerSecond", new Vector3(0f, 0f, 40f));
            for (int i = 0; i < 4; i++)
                Primitive(PrimitiveType.Cube, "Blade", hub.transform, Vector3.zero, new Vector3(0.6f, 7f, 0.1f), mCrate)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
        }

        // =====================================================================
        // WORLD 2 – Desert Ruins: two lava rivers with bridges, spinning blades,
        // a ziggurat with gems on top, ruin pillars.
        // =====================================================================

        private static void LayoutDesert()
        {
            // Lava rivers split into segments; the gaps are bridges.
            float[] riverZ = { -14f, 20f };
            float[][] segments = { new[] { -50f, -33f }, new[] { -27f, -3f }, new[] { 3f, 27f }, new[] { 33f, 50f } };
            foreach (float z in riverZ)
            {
                foreach (float[] seg in segments)
                {
                    float cx = (seg[0] + seg[1]) / 2f, len = seg[1] - seg[0];
                    Pool(lavaPrefab, new Vector3(cx, 0f, z), new Vector3(len, 0.7f, 7f));
                    Light glow = Empty("LavaGlow", layoutRoot, new Vector3(cx, 1.5f, z)).AddComponent<Light>();
                    glow.type = LightType.Point;
                    glow.color = new Color(1f, 0.45f, 0.1f);
                    glow.range = 14f;
                    glow.intensity = 2f;
                }
                foreach (float bx in new[] { -30f, 0f, 30f })
                {
                    Block("Bridge", new Vector3(bx, 0.15f, z), new Vector3(5.6f, 0.3f, 8f), mSandDark);
                    Block("Rail_L", new Vector3(bx - 2.6f, 0.6f, z), new Vector3(0.3f, 0.9f, 8f), mSandstone);
                    Block("Rail_R", new Vector3(bx + 2.6f, 0.6f, z), new Vector3(0.3f, 0.9f, 8f), mSandstone);
                    CoinLine(new Vector3(bx, 0.3f, z - 3f), new Vector3(bx, 0.3f, z + 3f), 3);
                    walkways.Add((new Vector3(bx, 0f, z - 5.5f), new Vector3(bx, 0f, z + 5.5f))); // keep trees off the bridge's path
                }
            }

            // Spinning blade hazards guarding the middle band.
            Place(bladePrefab, layoutRoot, new Vector3(-18f, 0f, 3f), 0f);
            Place(bladePrefab, layoutRoot, new Vector3(18f, 0f, 3f), 0f);
            Place(bladePrefab, layoutRoot, new Vector3(16f, 0f, 33f), 0f);
            CoinCircle(new Vector3(-18f, 0f, 3f), 6f, 10);
            CoinCircle(new Vector3(18f, 0f, 3f), 6f, 10);

            // Ziggurat temple with stairs and gems on top.
            Transform temple = Empty("Ziggurat", layoutRoot, new Vector3(-32f, 0f, 36f)).transform;
            Solid(PrimitiveType.Cube, "Tier1", temple, new Vector3(0f, 1f, 0f), new Vector3(14f, 2f, 12f), mSandstone);
            Solid(PrimitiveType.Cube, "Tier2", temple, new Vector3(0f, 3f, 0f), new Vector3(10f, 2f, 8f), mSandDark);
            Solid(PrimitiveType.Cube, "Tier3", temple, new Vector3(0f, 5f, 0f), new Vector3(6f, 2f, 4.5f), mSandstone);
            Ramp("TempleStairs", new Vector3(-32f, 0f, 22f), 2f, 8f, 4f, 0f, mSandDark);
            PadTo(new Vector3(-32f, 2f, 30.6f), new Vector3(-28f, 4f, 36f), 3.5f); // tier 1 -> lands on the tier-2 pad (chain bounce); high arc clears the top tier
            PadTo(new Vector3(-28f, 4f, 36f), new Vector3(-32f, 6f, 36f), 2f);   // tier 2 -> temple top with the gems
            CoinLine(new Vector3(-34f, 6f, 36f), new Vector3(-30f, 6f, 36f), 3, gems: true);
            CoinLine(new Vector3(-32f, 0.8f, 22.5f), new Vector3(-32f, 2.4f, 29f), 4); // breadcrumb trail up the stairs to the pad

            // Second raised ruin reachable by jump pad.
            Block("RuinDeck", new Vector3(34f, 4.2f, -32f), new Vector3(9f, 0.8f, 9f), mSandDark);
            Place(pillarPrefab, layoutRoot, new Vector3(30.5f, 4.6f, -35.5f), 0f, Vector3.one * 0.6f);
            Place(pillarPrefab, layoutRoot, new Vector3(37.5f, 4.6f, -28.5f), 0f, Vector3.one * 0.6f);
            PadTo(new Vector3(25f, 0f, -32f), new Vector3(34f, 4.6f, -32f));
            CoinLine(new Vector3(32f, 4.9f, -32f), new Vector3(36f, 4.9f, -32f), 3, gems: true);

            // Pillar field and broken walls.
            Random.State state = Random.state;
            Random.InitState(4321);
            for (int i = 0; i < 18; i++)
            {
                Vector3 p = new Vector3(Random.Range(-44f, 44f), 0f, Random.Range(-44f, 44f));
                if (Mathf.Abs(p.z - riverZ[0]) < 6f || Mathf.Abs(p.z - riverZ[1]) < 6f) continue;
                if (Mathf.Abs(p.x) < 6f) continue;
                if (p.x < -22f && p.z > 26f) continue;
                if (p.x > 24f && p.z < -24f) continue;
                GameObject[] pool = i % 3 == 0 ? desertPlants : (i % 3 == 1 ? ruinColumns : rocks);
                Place(Pick(pool), layoutRoot, p, Random.Range(0f, 360f));
            }
            Random.state = state;
            Block("BrokenWall_A", new Vector3(-12f, 1f, -32f), new Vector3(9f, 2f, 1f), mSandstone, 20f);
            Block("BrokenWall_B", new Vector3(14f, 0.6f, -30f), new Vector3(7f, 1.2f, 1f), mSandstone, -30f);
            Block("BrokenWall_C", new Vector3(12f, 1f, 30f), new Vector3(8f, 2f, 1f), mSandstone, 10f);
            CoinLine(new Vector3(-6f, 0f, -38f), new Vector3(6f, 0f, -26f), 6);

            FractalGrove(1, p => Mathf.Abs(p.z - riverZ[0]) > 7f && Mathf.Abs(p.z - riverZ[1]) > 7f && Mathf.Abs(p.x) > 6f &&
                                 !(p.x < -20f && p.z > 24f) && !(p.x > 22f && p.z < -22f));
            ScatterDecor(desertDecor, 70, 88, p => Mathf.Abs(p.x) > 4f && Mathf.Abs(p.z - riverZ[0]) > 6f && Mathf.Abs(p.z - riverZ[1]) > 6f);
            Place(Pick(desertPlants), layoutRoot, new Vector3(-8f, 0f, -44f), 0f);
            Place(Pick(desertPlants), layoutRoot, new Vector3(9f, 0f, -43f), 90f);
        }

        // =====================================================================
        // WORLD 3 – Frozen Night: freezing lake with moving ice floes, slippery ice fields,
        // crystal spires, snowmen, cliff decks, falling snow.
        // =====================================================================

        private static void LayoutFrozen()
        {
            // Freezing lake in the middle (hurts) – cross it on moving floes or walk around the shore.
            Pool(BuildFrostLakePrefabOnce(), new Vector3(0f, 0f, 8f), new Vector3(40f, 0.6f, 22f));
            // Floes/platforms sit higher than the lake trigger (0.6 m) so standing on them is safe.
            Block("Floe_Static_A", new Vector3(-12f, 0.5f, 8f), new Vector3(4f, 1f, 4f), mIce);
            Block("Floe_Static_B", new Vector3(12f, 0.5f, 8f), new Vector3(4f, 1f, 4f), mIce);
            MovingPlatformAt(new Vector3(0f, 0.8f, -2f), new Vector3(0f, 0f, 20f), 7f, 0f, new Vector3(4f, 0.6f, 4f));
            MovingPlatformAt(new Vector3(-6f, 0.8f, 8f), new Vector3(12f, 0f, 0f), 5f, 0.25f, new Vector3(3.5f, 0.6f, 3.5f));
            CoinCircle(new Vector3(-12f, 1f, 8f), 1.3f, 5);
            CoinCircle(new Vector3(12f, 1f, 8f), 1.3f, 5);
            CoinLine(new Vector3(0f, 1.2f, 0f), new Vector3(0f, 1.2f, 16f), 6, gems: false);
            Place(gemPrefab, levelCoins, new Vector3(0f, 1.2f, 8f), 0f);

            // Slippery ice fields.
            Pool(icePrefab, new Vector3(-30f, 0f, -26f), new Vector3(22f, 0.5f, 16f));
            Pool(icePrefab, new Vector3(30f, 0f, -26f), new Vector3(22f, 0.5f, 16f));
            Pool(icePrefab, new Vector3(0f, 0f, 32f), new Vector3(30f, 0.5f, 8f));
            CoinLine(new Vector3(-40f, 0f, -26f), new Vector3(-20f, 0f, -26f), 8);
            CoinLine(new Vector3(20f, 0f, -26f), new Vector3(40f, 0f, -26f), 8);

            // Cliff decks with jump pads and gems.
            Block("CliffDeck_W", new Vector3(-38f, 4.5f, 30f), new Vector3(10f, 9f, 10f), mWhite);
            Block("CliffDeck_E", new Vector3(38f, 4.5f, 30f), new Vector3(10f, 9f, 10f), mWhite);
            Block("MidDeck_W", new Vector3(-38f, 2.25f, 18f), new Vector3(8f, 4.5f, 8f), mSnow);
            Block("MidDeck_E", new Vector3(38f, 2.25f, 18f), new Vector3(8f, 4.5f, 8f), mSnow);
            // Ground pad -> lands in the middle of the mid-deck coin ring -> step onto the next pad -> cliff top gems.
            PadTo(new Vector3(-38f, 0f, 9f), new Vector3(-38f, 4.5f, 17f));
            PadTo(new Vector3(38f, 0f, 9f), new Vector3(38f, 4.5f, 17f));
            PadTo(new Vector3(-36.5f, 4.5f, 20.8f), new Vector3(-38f, 9f, 30f));
            PadTo(new Vector3(36.5f, 4.5f, 20.8f), new Vector3(38f, 9f, 30f));
            CoinLine(new Vector3(-40f, 9.3f, 30f), new Vector3(-36f, 9.3f, 30f), 3, gems: true);
            CoinLine(new Vector3(36f, 9.3f, 30f), new Vector3(40f, 9.3f, 30f), 3, gems: true);
            CoinCircle(new Vector3(-38f, 4.5f, 17f), 2f, 6);
            CoinCircle(new Vector3(38f, 4.5f, 17f), 2f, 6);

            // Crystal spires + snowmen.
            Random.State state = Random.state;
            Random.InitState(999);
            for (int i = 0; i < 20; i++)
            {
                Vector3 p = new Vector3(Random.Range(-45f, 45f), 0f, Random.Range(-45f, 45f));
                if (Mathf.Abs(p.x) < 24f && p.z > -6f && p.z < 22f) continue; // lake
                if (Mathf.Abs(p.x) < 6f) continue;
                if (Mathf.Abs(p.x) > 30f && p.z > 10f) continue; // cliffs
                if (i % 5 == 0) Place(snowmanPrefab, layoutRoot, p, Random.Range(0f, 360f));
                else if (i % 2 == 0) Place(pineTrees[0], layoutRoot, p, Random.Range(0f, 360f), Vector3.one * Random.Range(0.85f, 1.3f));
                else Place(crystalPrefab, layoutRoot, p, Random.Range(0f, 360f), Vector3.one * Random.Range(0.8f, 1.4f));
            }
            Random.state = state;

            FractalGrove(2, p => !(Mathf.Abs(p.x) < 26f && p.z > -8f && p.z < 24f) && Mathf.Abs(p.x) > 6f &&
                                 !(Mathf.Abs(p.x) > 27f && p.z > 3f));
            ScatterDecor(frozenDecor, 60, 99, p => Mathf.Abs(p.x) > 5f && !(Mathf.Abs(p.x) < 24f && p.z > -6f && p.z < 22f));

            // Glowing lamp posts along the main path (cool point lights).
            for (int i = 0; i < 4; i++)
            {
                foreach (float x in new[] { -5f, 5f })
                {
                    Vector3 p = new Vector3(x, 0f, -36f + i * 9f);
                    Solid(PrimitiveType.Cylinder, "LampPost", layoutRoot, p + Vector3.up * 1.5f, new Vector3(0.25f, 1.5f, 0.25f), mMetal);
                    Primitive(PrimitiveType.Sphere, "LampBulb", layoutRoot, p + Vector3.up * 3.2f, Vector3.one * 0.5f, mCrystal);
                    Light l = Empty("LampLight", layoutRoot, p + Vector3.up * 3.2f).AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(0.55f, 0.8f, 1f);
                    l.range = 9f;
                    l.intensity = 2.5f;
                }
            }

            // Falling snow over the whole arena.
            ParticleSystem snow = MakeParticles("Snowfall", layoutRoot, new Vector3(0f, 22f, 0f), new Color(1f, 1f, 1f, 0.9f),
                true, 450f, 0, 0.5f, 9f, 0.18f, ParticleSystemShapeType.Box, 1f, 0.25f);
            ParticleSystem.ShapeModule shape = snow.shape;
            shape.scale = new Vector3(100f, 1f, 100f);
            ParticleSystem.MainModule main = snow.main;
            main.maxParticles = 5000;
            main.prewarm = true;
        }

        private static GameObject frostLakePrefab;

        private static GameObject BuildFrostLakePrefabOnce()
        {
            if (frostLakePrefab == null)
                frostLakePrefab = SavePrefab(BuildHazardPool("FrostLake", sgFrostWater, 8f), PrefabDir + "/Level/FrostLake.prefab");
            return frostLakePrefab;
        }

        // =====================================================================
        // MAIN MENU – small diorama with an orbiting camera behind the menu.
        // =====================================================================

        private static void BuildMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            WorldLook look = GetLook(0);
            SetupEnvironmentLighting(worldConfigs[0], look);
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 110f;

            Transform environment = new GameObject("[Environment]").transform;
            CreateSun(environment, new Color(1f, 0.9f, 0.78f), 1.35f, new Vector3(32f, -40f, 0f));
            CreateGlobalVolume(environment, CreateVolumeProfile("PP_MainMenu", new Color(1f, 0.97f, 0.92f), 8f, -0.05f, 1.3f));

            // ---------- Diorama island built from Kenney Nature / Platformer kit models ----------
            layoutRoot = Empty("Diorama", environment, Vector3.zero).transform;
            levelCoins = Empty("Coins", layoutRoot, Vector3.zero).transform;
            Solid(PrimitiveType.Cylinder, "Island", layoutRoot, new Vector3(0f, -0.5f, 0f), new Vector3(34f, 0.5f, 34f), look.ground);
            Primitive(PrimitiveType.Cylinder, "IslandCliff", layoutRoot, new Vector3(0f, -3f, 0f), new Vector3(33f, 2.5f, 33f), mRock);

            Place(portalPrefab, layoutRoot, new Vector3(0f, 0f, 7f), 180f);
            Place(flagPrefab, layoutRoot, new Vector3(-4f, 0f, 6.5f), 0f);
            Place(flagPrefab, layoutRoot, new Vector3(4f, 0f, 6.5f), 180f);
            Place(jumpPadPrefab, layoutRoot, new Vector3(-7f, 0f, -1f), 0f);
            Place(lavaPrefab, layoutRoot, new Vector3(8f, 0f, 2f), 25f, new Vector3(5f, 0.6f, 3f));
            Place(campfirePrefab, layoutRoot, new Vector3(-3f, 0f, -6f), 0f);
            Place(tentPrefab, layoutRoot, new Vector3(-8f, 0f, -7f), 40f);
            Place(logStackPrefab, layoutRoot, new Vector3(-1f, 0f, -9f), -30f);
            Light fire = Empty("CampfireLight", layoutRoot, new Vector3(-3f, 1f, -6f)).AddComponent<Light>();
            fire.type = LightType.Point; fire.color = new Color(1f, 0.55f, 0.2f); fire.range = 8f; fire.intensity = 3f;
            MakeParticles("CampfireSparks", layoutRoot, new Vector3(-3f, 0.4f, -6f), new Color(1f, 0.6f, 0.2f),
                true, 14f, 0, 1.2f, 1.2f, 0.12f, ParticleSystemShapeType.Cone, 0.2f, -0.2f);

            Random.State state = Random.state;
            Random.InitState(2024);
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                float r = Random.Range(13.5f, 16f);
                Vector3 p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                GameObject[] pool = i % 4 == 0 ? rocks : (i % 4 == 1 ? desertPlants : (i % 4 == 2 ? pineTrees : meadowTrees));
                Place(Pick(pool), layoutRoot, p, Random.Range(0f, 360f), Vector3.one * Random.Range(0.5f, 0.7f));
            }
            Random.state = state;
            Place(Pick(ruinColumns), layoutRoot, new Vector3(9f, 0f, -6f), 20f);
            Place(snowmanPrefab, layoutRoot, new Vector3(6f, 0f, -10f), 200f);
            ScatterDecorIsland(smallDecor, 70, 31, 12f);
            CoinCircle(new Vector3(0f, 0f, 0f), 4f, 12);
            Place(gemPrefab, levelCoins, new Vector3(0f, 0.3f, 0f), 0f);

            // Static display enemies (behaviour removed, they just idle).
            GameObject hog = Place(blobPrefab, layoutRoot, new Vector3(5f, 0f, -3f), 210f);
            GameObject orc = Place(brutePrefab, layoutRoot, new Vector3(-5f, 0f, 3f), 140f);
            foreach (GameObject e in new[] { hog, orc })
            {
                Object.DestroyImmediate(e.GetComponent<EnemyAnimator>());
                Object.DestroyImmediate(e.GetComponent<EnemyVisual>());
                Object.DestroyImmediate(e.GetComponent<EnemyBehaviour>());
                Transform marker = e.transform.Find("AlertMarker");
                if (marker != null) marker.gameObject.SetActive(false);
            }

            // ---------- Camera orbits the diorama ----------
            Transform cameras = new GameObject("[Cameras]").transform;
            GameObject pivot = Empty("CameraPivot", cameras, Vector3.zero);
            pivot.AddComponent<Rotator>();
            SetVector3(pivot.GetComponent<Rotator>(), "degreesPerSecond", new Vector3(0f, 5f, 0f));
            GameObject camGo = Empty("Main Camera", pivot.transform, new Vector3(0f, 10f, -25f));
            camGo.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 48f;
            cam.cullingMask = ~((1 << MinimapLayer) | (1 << HeroStageLayer));
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();

            // ---------- Hero preview stage (far below, own layer, rendered to a RenderTexture) ----------
            Transform stage = Empty("[HeroStage]", null, new Vector3(0f, -200f, 0f)).transform;
            GameObject pedestal = Primitive(PrimitiveType.Cylinder, "Pedestal", stage, new Vector3(0f, -0.15f, 0f), new Vector3(1.9f, 0.15f, 1.9f), mPadBase);
            GameObject ring = Primitive(PrimitiveType.Cylinder, "GlowRing", stage, new Vector3(0f, 0.01f, 0f), new Vector3(2.2f, 0.02f, 2.2f), sgJumpPad);
            Transform turntable = Empty("Turntable", stage, Vector3.zero).transform;
            _ = pedestal; _ = ring;

            Light key = Empty("KeyLight", stage, new Vector3(1.5f, 3f, -2.5f)).AddComponent<Light>();
            key.type = LightType.Spot; key.spotAngle = 60f; key.range = 10f; key.intensity = 7f; key.color = new Color(1f, 0.95f, 0.88f);
            key.transform.LookAt(stage.position + Vector3.up * 0.8f);
            Light rim = Empty("RimLight", stage, new Vector3(-1.6f, 2.2f, 1.8f)).AddComponent<Light>();
            rim.type = LightType.Point; rim.range = 6f; rim.intensity = 2.5f; rim.color = new Color(0.5f, 0.7f, 1f);

            string heroRtPath = RenderingDir + "/RT_HeroPreview.renderTexture";
            RenderTexture heroRt = AssetDatabase.LoadAssetAtPath<RenderTexture>(heroRtPath);
            if (heroRt == null)
            {
                heroRt = new RenderTexture(900, 1100, 24) { name = "RT_HeroPreview", antiAliasing = 4 };
                AssetDatabase.CreateAsset(heroRt, heroRtPath);
            }
            GameObject heroCamGo = Empty("HeroCamera", stage, new Vector3(0f, 1.25f, -5.6f));
            heroCamGo.transform.LookAt(stage.position + Vector3.up * 0.82f);
            Camera heroCam = heroCamGo.AddComponent<Camera>();
            heroCam.fieldOfView = 26f;
            heroCam.clearFlags = CameraClearFlags.SolidColor;
            heroCam.backgroundColor = new Color(0.1f, 0.11f, 0.2f, 1f);
            heroCam.cullingMask = 1 << HeroStageLayer;
            heroCam.targetTexture = heroRt;
            heroCam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            foreach (Transform t in stage.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = HeroStageLayer;

            Transform managers = new GameObject("[Managers]").transform;
            AudioManager audio = Empty("AudioManager", managers, Vector3.zero).AddComponent<AudioManager>();
            SetupAudioManager(audio, "music_menu");

            BuildMenuUI(turntable, heroRt);
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        private static void ScatterDecorIsland(GameObject[] pool, int count, int seed, float radius)
        {
            Transform parent = Empty("Decoration", layoutRoot, Vector3.zero).transform;
            Random.State state = Random.state;
            Random.InitState(seed);
            for (int i = 0; i < count; i++)
            {
                Vector2 c = Random.insideUnitCircle * radius;
                if (c.magnitude < 5.5f) continue; // keep the coin ring clear
                Vector3 p = new Vector3(c.x, 0f, c.y);
                GameObject d = Place(Pick(pool), parent, p, Random.Range(0f, 360f));
                d.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
            }
            Random.state = state;
        }
    }
}
