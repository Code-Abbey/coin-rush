using CoinRush.Audio;
using CoinRush.Intro;
using CoinRush.Level;
using CoinRush.UI;
using CoinRush.Utilities;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoinRush.EditorTools
{
    /// <summary>Splash screen and the illustrated intro story ("The Stolen Coins of Aurum").</summary>
    public static partial class CoinRushBuilder
    {
        // =====================================================================
        // SPLASH
        // =====================================================================

        private static void BuildSplashScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = SkyboxMat("SKY_Splash", new Color(0.035f, 0.045f, 0.085f), new Color(0.2f, 0.12f, 0.11f),
                new Color(0.02f, 0.02f, 0.04f), new Color(1f, 0.6f, 0.3f), 0.8f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.3f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.18f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.07f, 0.12f);
            RenderSettings.fog = false;

            Transform env = new GameObject("[Environment]").transform;
            CreateSun(env, new Color(1f, 0.9f, 0.75f), 1.1f, new Vector3(25f, -30f, 0f));
            CreateGlobalVolume(env, CreateVolumeProfile("PP_Splash", TextColor, 12f, 0f, 2.4f));

            // Giant spinning coin with an orbit of smaller coins and sparkles.
            Transform hero = Empty("CoinCentrepiece", env, new Vector3(0f, 1.5f, 0f)).transform;
            Place(coinPrefab, hero, hero.position, 0f, Vector3.one * 3.6f);
            GameObject orbit = Empty("CoinOrbit", hero, hero.position);
            SetVector3(orbit.AddComponent<Rotator>(), "degreesPerSecond", new Vector3(0f, 45f, 0f));
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                Vector3 p = hero.position + new Vector3(Mathf.Cos(a) * 3.4f, Mathf.Sin(a * 2f) * 0.6f, Mathf.Sin(a) * 3.4f);
                Place(coinPrefab, orbit.transform, p, 0f, Vector3.one * 1.3f);
            }
            MakeParticles("Sparkles", hero, Vector3.zero, new Color(1f, 0.85f, 0.4f), true, 14f, 0, 0.4f, 2.5f, 0.07f,
                ParticleSystemShapeType.Sphere, 3.8f);
            PointLight(env, "GoldKey", new Vector3(0f, 1.5f, -3f), new Color(1f, 0.78f, 0.35f), 7f, 9f);
            PointLight(env, "CoralRim", new Vector3(3.5f, 3.5f, 2.5f), new Color(1f, 0.44f, 0.38f), 5f, 10f);
            PointLight(env, "TealRim", new Vector3(-3.5f, 0f, 2.5f), new Color(0.26f, 0.85f, 0.74f), 4f, 10f);

            Camera cam = StoryCamera(new Vector3(0f, 0.4f, -15f));
            cam.transform.LookAt(new Vector3(0f, 0.1f, 0f));
            cam.fieldOfView = 40f;

            AudioManager audio = Empty("AudioManager", new GameObject("[Managers]").transform, Vector3.zero).AddComponent<AudioManager>();
            SetupAudioManager(audio, "music_menu");

            BuildSplashUI();
            EditorSceneManager.SaveScene(scene, SplashScenePath);
        }

        private static void BuildSplashUI()
        {
            EnsureFontMaterials();
            Transform uiRoot = new GameObject("[UI]").transform;
            Canvas canvas = CreateCanvas(uiRoot, "SplashCanvas");
            Transform c = canvas.transform;
            Vector2 mid = new Vector2(0.5f, 0.5f);

            Image vignette = Stretch(Rect("Vignette", c)).gameObject.AddComponent<Image>();
            vignette.sprite = SpriteAsset("ui_vignette");
            vignette.color = new Color(Ink.r, Ink.g, Ink.b, 0.92f);
            vignette.raycastTarget = false;

            RectTransform logoRoot = Stretch(Rect("Logo", c));
            CanvasGroup logoGroup = logoRoot.gameObject.AddComponent<CanvasGroup>();
            TMP_Text logo = Centered(Text(logoRoot, "Title", "COIN RUSH", 170, TextAlignmentOptions.Center, TextColor, mid, Vector2.zero, new Vector2(1400, 190), fontTitle), new Vector2(0, -225));
            logo.enableVertexGradient = true;
            logo.colorGradient = new VertexGradient(new Color(1f, 0.97f, 0.6f), new Color(1f, 0.97f, 0.6f), new Color(1f, 0.62f, 0.1f), new Color(1f, 0.55f, 0.08f));
            logo.characterSpacing = 6f;

            RectTransform taglineRoot = Stretch(Rect("Tagline", c));
            CanvasGroup taglineGroup = taglineRoot.gameObject.AddComponent<CanvasGroup>();
            Centered(Text(taglineRoot, "Text", "THE STOLEN COINS OF AURUM", 30, TextAlignmentOptions.Center, Primary, mid, Vector2.zero, new Vector2(1200, 40), fontOutline), new Vector2(0, -320))
                .characterSpacing = 10f;

            Image loading = Bar(c, "LoadingBar", new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(560, 14), Primary, centred: true);

            RectTransform press = Rect("PressAnyKey", c);
            Place(press, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(800, 40));
            press.pivot = new Vector2(0.5f, 0.5f);
            CanvasGroup pressGroup = press.gameObject.AddComponent<CanvasGroup>();
            Centered(Text(press, "Text", "PRESS ANY KEY", 28, TextAlignmentOptions.Center, TextColor, mid, Vector2.zero, new Vector2(800, 40), fontOutline), Vector2.zero)
                .characterSpacing = 12f;

            SplashController splash = canvas.gameObject.AddComponent<SplashController>();
            Set(splash, "logoGroup", logoGroup);
            Set(splash, "logo", logo.rectTransform);
            Set(splash, "taglineGroup", taglineGroup);
            Set(splash, "loadingFill", loading);
            Set(splash, "pressKeyGroup", pressGroup);
            CreateEventSystem(uiRoot);
        }

        // =====================================================================
        // STORY
        // =====================================================================

        private struct SlideDef
        {
            public string title, body, icon;
            public Color accent;
            public Transform shot;
        }

        private static void BuildStoryScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = SkyboxMat("SKY_Story", new Color(0.22f, 0.3f, 0.62f), new Color(1f, 0.62f, 0.42f),
                new Color(0.25f, 0.2f, 0.3f), new Color(1f, 0.82f, 0.55f), 0.15f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.6f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.45f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.22f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.85f, 0.6f, 0.5f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 110f;

            Transform env = new GameObject("[Environment]").transform;
            CreateSun(env, new Color(1f, 0.86f, 0.7f), 1.25f, new Vector3(28f, -40f, 0f));
            CreateGlobalVolume(env, CreateVolumeProfile("PP_Story", new Color(1f, 0.97f, 0.93f), 10f, 0f, 1.3f));

            // Each chapter is a small 3D set, 200 m apart so fog hides the others.
            Transform sets = new GameObject("[StorySets]").transform;
            SlideDef[] defs =
            {
                new SlideDef { title = "The Island of Aurum", icon = "icon_portal", accent = Gold, shot = BuildSetAurum(sets, new Vector3(0f, 0f, 0f)),
                    body = "Above the clouds drifts Aurum, an island built on gold. At its heart glows the Golden Portal, powered by a thousand coins: the only road home for every traveller." },
                new SlideDef { title = "The Orc King's Raid", icon = "icon_skull", accent = Coral, shot = BuildSetOrcKing(sets, new Vector3(200f, 0f, 0f)),
                    body = "Then came the Orc King. Greedy for treasure, he tore the portal apart. Its coins burst across the sky like fireworks, and Aurum's light went out." },
                new SlideDef { title = "Three Shattered Worlds", icon = "icon_gem", accent = Sky, shot = BuildSetWorlds(sets, new Vector3(400f, 0f, 0f)),
                    body = "The gold rained down on three wild worlds: a sunny meadow, scorching desert ruins and a frozen night. His hogs and orc brutes now guard every coin." },
                new SlideDef { title = "Five Unlikely Heroes", icon = "icon_star", accent = Teal, shot = BuildSetHeroes(sets, new Vector3(600f, 0f, 0f)),
                    body = "An explorer, a sprinter, an acrobat, a strongman and a spy. Different strengths, one mission. Pick the hero whose style matches yours." },
                new SlideDef { title = "Collect. Relight. Escape.", icon = "icon_coin", accent = Primary, shot = BuildSetEscape(sets, new Vector3(800f, 0f, 0f)),
                    body = "Grab enough coins to relight each world's portal. Dodge the guards, beat the clock and leap through. Clear all three to bring Aurum's gold home!" },
            };

            Camera cam = StoryCamera(defs[0].shot.position);
            cam.transform.rotation = defs[0].shot.rotation;
            cam.fieldOfView = 50f;

            AudioManager audio = Empty("AudioManager", new GameObject("[Managers]").transform, Vector3.zero).AddComponent<AudioManager>();
            SetupAudioManager(audio, "music_menu");

            BuildStoryUI(cam, defs);
            EditorSceneManager.SaveScene(scene, StoryScenePath);
        }

        // ---------- Chapter 1: Aurum, the golden portal island ----------
        private static Transform BuildSetAurum(Transform parent, Vector3 origin)
        {
            Transform set = Empty("Set1_Aurum", parent, origin).transform;
            Material grass = GetLook(0).ground;
            Primitive(PrimitiveType.Cylinder, "Island", set, new Vector3(0f, -0.25f, 0f), new Vector3(30f, 0.25f, 30f), grass);
            Primitive(PrimitiveType.Cylinder, "Cliff", set, new Vector3(0f, -2.6f, 0f), new Vector3(28f, 2.2f, 28f), mRock);
            Place(portalPrefab, set, origin + new Vector3(0f, 0f, 5f), 180f);
            Place(flagPrefab, set, origin + new Vector3(-3.5f, 0f, 4.5f), 0f);
            Place(flagPrefab, set, origin + new Vector3(3.5f, 0f, 4.5f), 180f);

            GameObject ring = Empty("GoldenCoinRing", set, new Vector3(0f, 3.4f, 5f));
            SetVector3(ring.AddComponent<Rotator>(), "degreesPerSecond", new Vector3(0f, 30f, 0f));
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                Place(coinPrefab, ring.transform, ring.transform.position + new Vector3(Mathf.Cos(a) * 3.6f, Mathf.Sin(a * 3f) * 0.4f, Mathf.Sin(a) * 3.6f), 0f, Vector3.one * 1.2f);
            }
            MakeParticles("GoldDust", set, new Vector3(0f, 2.5f, 5f), new Color(1f, 0.85f, 0.4f), true, 25f, 0, 0.4f, 3f, 0.12f, ParticleSystemShapeType.Sphere, 4f);
            PointLight(set, "PortalGlow", new Vector3(0f, 2.5f, 3.5f), new Color(1f, 0.8f, 0.4f), 6f, 10f);
            Ring(set, meadowTrees, 9, 9f, 13f, 11, 0.8f, 1.2f);
            Ring(set, smallDecor, 18, 5f, 12f, 12, 0.9f, 1.3f);
            Place(campfirePrefab, set, origin + new Vector3(-5f, 0f, -1f), 0f);
            Place(tentPrefab, set, origin + new Vector3(-8f, 0f, 1f), 60f);
            return Shot(set, new Vector3(0.5f, 4.6f, -14f), new Vector3(-2.6f, 1.6f, 4f));
        }

        // ---------- Chapter 2: the Orc King smashes the portal ----------
        private static Transform BuildSetOrcKing(Transform parent, Vector3 origin)
        {
            Transform set = Empty("Set2_OrcKing", parent, origin).transform;
            Material scorched = Lit("Environment", "M_Story_Scorched", new Color(0.2f, 0.16f, 0.16f), 0f, 0.15f);
            Primitive(PrimitiveType.Cylinder, "Ground", set, new Vector3(0f, -0.25f, 0f), new Vector3(30f, 0.25f, 30f), scorched);

            GameObject orc = SpawnModel(ModelAsset(KenneyDir + "/MiniDungeon/Models/character-orc.fbx"), set, new Vector3(0f, 0f, 2.5f), 3.4f, 180f);
            Actor(orc, orcController, 0f, "Attack", 2.4f, 0f);
            Crown(orc.transform, mSandstone);

            foreach (Vector3 p in new[] { new Vector3(-3f, 0f, 1f), new Vector3(3.2f, 0f, 0.5f) })
            {
                GameObject hog = SpawnModel(ModelAsset(KenneyDir + "/CubePets/Models/animal-hog.fbx"), set, p, 1.1f, p.x < 0 ? 150f : 210f);
                Actor(hog, petController, 0f, "", 3f, 0f);
            }

            // Broken portal: toppled pillars, a cracked frame and spilled gold.
            Material stone = mSandstone;
            Primitive(PrimitiveType.Cube, "FallenPillarL", set, new Vector3(-2.6f, 0.5f, 6.5f), new Vector3(1f, 1f, 4.5f), stone).transform.localRotation = Quaternion.Euler(0f, 35f, 8f);
            Primitive(PrimitiveType.Cube, "FallenPillarR", set, new Vector3(2.8f, 0.5f, 6f), new Vector3(1f, 1f, 4f), stone).transform.localRotation = Quaternion.Euler(0f, -25f, -6f);
            Primitive(PrimitiveType.Cube, "BrokenArch", set, new Vector3(0.4f, 0.35f, 7.5f), new Vector3(4f, 0.7f, 1f), stone).transform.localRotation = Quaternion.Euler(0f, 10f, 12f);
            Random.State state = Random.state;
            Random.InitState(77);
            for (int i = 0; i < 16; i++)
            {
                Vector2 c = Random.insideUnitCircle * 5f;
                Place(i % 5 == 0 ? gemPrefab : coinPrefab, set, origin + new Vector3(c.x, 0.2f, Mathf.Abs(c.y) + 3f), Random.Range(0f, 360f));
            }
            Random.state = state;
            Ring(set, rocks, 10, 8f, 13f, 21, 0.8f, 1.4f);

            PointLight(set, "FireLight", new Vector3(0f, 2f, -1f), new Color(1f, 0.3f, 0.15f), 9f, 12f);
            PointLight(set, "BackLight", new Vector3(0f, 4f, 6f), new Color(0.9f, 0.2f, 0.3f), 8f, 12f);
            MakeParticles("Embers", set, new Vector3(0f, 0.2f, 3f), new Color(1f, 0.5f, 0.2f), true, 40f, 0, 1.6f, 2.5f, 0.1f, ParticleSystemShapeType.Circle, 6f, -0.15f)
                .transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            MakeParticles("Smoke", set, new Vector3(0f, 0.5f, 7f), new Color(0.15f, 0.12f, 0.14f, 0.6f), true, 12f, 0, 0.8f, 4f, 2.2f, ParticleSystemShapeType.Circle, 3f, -0.05f)
                .transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            LocalVolume(set, "PP_Story_OrcKing", new Color(1f, 0.75f, 0.75f), -10f, -0.4f, 1.6f);
            return Shot(set, new Vector3(0.6f, 2.0f, -9.5f), new Vector3(-1.9f, 1.9f, 2.5f));
        }

        // ---------- Chapter 3: coins rain on three floating worlds ----------
        private static Transform BuildSetWorlds(Transform parent, Vector3 origin)
        {
            Transform set = Empty("Set3_Worlds", parent, origin).transform;
            (string name, Vector3 pos, Material ground, GameObject[] props)[] islands =
            {
                ("Meadow", new Vector3(-7f, 4f, 7f), GetLook(0).ground, meadowTrees),
                ("Desert", new Vector3(0f, 6.5f, 11f), GetLook(1).ground, desertPlants),
                ("Frozen", new Vector3(7f, 4f, 7f), mSnow, pineTrees),
            };
            for (int i = 0; i < islands.Length; i++)
            {
                var isl = islands[i];
                GameObject root = Empty("Island_" + isl.name, set, isl.pos);
                SetVector3(root.AddComponent<Rotator>(), "degreesPerSecond", new Vector3(0f, i % 2 == 0 ? 8f : -8f, 0f));
                Primitive(PrimitiveType.Cylinder, "Top", root.transform, Vector3.zero, new Vector3(5f, 0.3f, 5f), isl.ground);
                Primitive(PrimitiveType.Cylinder, "Rock", root.transform, new Vector3(0f, -1.1f, 0f), new Vector3(3.6f, 0.9f, 3.6f), mRock);
                Primitive(PrimitiveType.Cylinder, "Tip", root.transform, new Vector3(0f, -2.3f, 0f), new Vector3(1.6f, 0.6f, 1.6f), mRock);
                for (int k = 0; k < 3; k++)
                {
                    float a = k * 2.1f;
                    Place(isl.props[k % isl.props.Length], root.transform, root.transform.position + new Vector3(Mathf.Cos(a) * 1.4f, 0.3f, Mathf.Sin(a) * 1.4f), k * 70f, Vector3.one * 0.6f);
                }
                if (i == 1) Place(ruinColumns[0], root.transform, root.transform.position + new Vector3(0.3f, 0.3f, 0.2f), 30f, Vector3.one * 0.6f);
                if (i == 2) Place(snowmanPrefab, root.transform, root.transform.position + new Vector3(-0.6f, 0.3f, -0.8f), 160f, Vector3.one * 0.7f);
                MakeParticles("CoinRain", root.transform, new Vector3(0f, 6f, 0f), new Color(1f, 0.82f, 0.25f), true, 18f, 0, 0.2f, 2.5f, 0.22f, ParticleSystemShapeType.Circle, 2.2f, 0.6f)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            // A soft sea of small clouds below the floating islands.
            Material cloud = Lit("Environment", "M_Cloud", TextColor, 0f, 0f, new Color(0.35f, 0.35f, 0.38f));
            Random.State state = Random.state;
            Random.InitState(303);
            for (int i = 0; i < 26; i++)
            {
                Vector3 p = new Vector3(Random.Range(-16f, 16f), Random.Range(-2.2f, -1f), Random.Range(2f, 20f));
                float sz = Random.Range(1.8f, 3.4f);
                GameObject puff = Primitive(PrimitiveType.Sphere, "CloudPuff", set, p, new Vector3(sz * 1.5f, sz * 0.7f, sz), cloud);
                puff.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            Random.state = state;
            return Shot(set, new Vector3(0f, 5.6f, -11f), new Vector3(-2.2f, 3.6f, 8f));
        }

        // ---------- Chapter 4: the five heroes ----------
        private static Transform BuildSetHeroes(Transform parent, Vector3 origin)
        {
            Transform set = Empty("Set4_Heroes", parent, origin).transform;
            Primitive(PrimitiveType.Cylinder, "Plateau", set, new Vector3(0f, -0.25f, 0f), new Vector3(22f, 0.25f, 22f), GetLook(0).ground);
            Primitive(PrimitiveType.Cylinder, "Stage", set, new Vector3(0f, 0.05f, 0.5f), new Vector3(9f, 0.1f, 4f), mPadBase);

            int count = characterDatabase.Count;
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) / 2f) * 1.5f;
                float z = Mathf.Abs(x) * 0.25f;
                GameObject h = SpawnModel(characterDatabase.Get(i).model, set, new Vector3(x, 0.1f, z), characterDatabase.heroHeight, 180f - x * 6f);
                Actor(h, heroController, 0f, "Cheer", 3.2f + i * 0.3f, 0f);
            }
            Ring(set, meadowTrees, 8, 8f, 10f, 31, 0.8f, 1.1f);
            Ring(set, smallDecor, 16, 4.5f, 9f, 32, 0.9f, 1.2f);
            Place(flagPrefab, set, origin + new Vector3(-4.5f, 0f, 2f), 0f);
            Place(flagPrefab, set, origin + new Vector3(4.5f, 0f, 2f), 180f);
            PointLight(set, "Spot", new Vector3(0f, 3.5f, -2.5f), new Color(1f, 0.95f, 0.85f), 5f, 9f);
            MakeParticles("Fireflies", set, new Vector3(0f, 2f, 1f), new Color(1f, 0.95f, 0.5f), true, 15f, 0, 0.3f, 4f, 0.08f, ParticleSystemShapeType.Sphere, 4f);
            return Shot(set, new Vector3(0.4f, 2.3f, -8.5f), new Vector3(-1.7f, 0.6f, 0.6f));
        }

        // ---------- Chapter 5: run for the portal ----------
        private static Transform BuildSetEscape(Transform parent, Vector3 origin)
        {
            Transform set = Empty("Set5_Escape", parent, origin).transform;
            Primitive(PrimitiveType.Cube, "Ground", set, new Vector3(0f, -0.25f, 8f), new Vector3(24f, 0.5f, 34f), GetLook(0).ground);
            Primitive(PrimitiveType.Cube, "Path", set, new Vector3(0f, 0.01f, 6f), new Vector3(2.4f, 0.04f, 18f), mSandstone);
            Place(portalPrefab, set, origin + new Vector3(0f, 0f, 15f), 180f);
            PointLight(set, "PortalGlow", new Vector3(0f, 2.5f, 13.5f), new Color(0.75f, 0.5f, 1f), 7f, 10f);
            for (int i = 0; i < 10; i++) Place(coinPrefab, set, origin + new Vector3(0f, 0.2f, 1.5f + i * 1.2f), 0f);
            for (int i = 0; i < 8; i++)
            {
                float z = 1.5f + i * 2.2f;
                Place(Pick(meadowTrees), set, origin + new Vector3(-5.5f - (i % 2), 0f, z), i * 40f, Vector3.one * 0.9f);
                Place(Pick(meadowTrees), set, origin + new Vector3(5.5f + (i % 2), 0f, z + 1.1f), i * 55f, Vector3.one * 0.9f);
            }
            Ring(set, smallDecor, 24, 3.5f, 11f, 41, 0.9f, 1.3f);

            GameObject runner = SpawnModel(characterDatabase.Get(0).model, set, new Vector3(0f, 0f, -1f), characterDatabase.heroHeight, 0f);
            Actor(runner, heroController, 7f, "", 9f, 3.2f, 11f);
            return Shot(set, new Vector3(1.6f, 2.6f, -7.5f), new Vector3(-1.3f, 0.9f, 7f));
        }

        // ---------------- Story UI ----------------

        private static void BuildStoryUI(Camera cam, SlideDef[] defs)
        {
            EnsureFontMaterials();
            Transform uiRoot = new GameObject("[UI]").transform;
            Canvas canvas = CreateCanvas(uiRoot, "StoryCanvas");
            Transform c = canvas.transform;

            // Cinematic letterbox bars.
            foreach (bool top in new[] { true, false })
            {
                RectTransform bar = Rect(top ? "LetterboxTop" : "LetterboxBottom", c);
                bar.anchorMin = new Vector2(0f, top ? 1f : 0f);
                bar.anchorMax = new Vector2(1f, top ? 1f : 0f);
                bar.pivot = new Vector2(0.5f, top ? 1f : 0f);
                bar.sizeDelta = new Vector2(0f, 90f);
                Image img = bar.gameObject.AddComponent<Image>();
                img.color = new Color(Ink.r * 0.6f, Ink.g * 0.6f, Ink.b * 0.6f, 1f);
                img.raycastTarget = false;
            }

            // Caption card (bottom-left).
            RectTransform card = Card(c, "CaptionCard", new Vector2(0, 0), new Vector2(80, 120), new Vector2(880, 310));
            CanvasGroup captionGroup = card.gameObject.AddComponent<CanvasGroup>();
            TMP_Text chapter = Text(card, "Chapter", "CHAPTER 1 OF 5", 20, TextAlignmentOptions.Left, Muted, new Vector2(0, 1), new Vector2(40, -30), new Vector2(600, 28), null);
            chapter.characterSpacing = 8f;
            TMP_Text title = Text(card, "Title", "TITLE", 54, TextAlignmentOptions.Left, Gold, new Vector2(0, 1), new Vector2(36, -62), new Vector2(760, 66), fontTitle);
            TMP_Text body = Body(Text(card, "Body", "", 26, TextAlignmentOptions.TopLeft, TextColor, new Vector2(0, 1), new Vector2(40, -140), new Vector2(800, 160), fontOutline));
            body.textWrappingMode = TextWrappingModes.Normal;
            Image icon = Icon(card, "Icon", "icon_coin", new Vector2(1, 1), new Vector2(-40, -32), 76);
            ((RectTransform)icon.transform).pivot = new Vector2(0.5f, 0.5f);
            ((RectTransform)icon.transform).anchoredPosition = new Vector2(-70, -70);

            // Page dots (inside the bottom letterbox).
            RectTransform dotRow = Rect("Dots", c);
            Place(dotRow, new Vector2(0.5f, 0), new Vector2(0, 38), new Vector2(400, 16));
            dotRow.pivot = new Vector2(0.5f, 0.5f);
            HorizontalLayoutGroup dl = dotRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            dl.childAlignment = TextAnchor.MiddleCenter; dl.spacing = 12f;
            dl.childControlWidth = dl.childControlHeight = false; dl.childForceExpandWidth = dl.childForceExpandHeight = false;
            Image[] dots = new Image[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                RectTransform d = Rect($"Dot{i + 1}", dotRow);
                d.sizeDelta = new Vector2(14, 14);
                dots[i] = SlicedImage(d, "ui_pill", TextColor);
                dots[i].raycastTarget = false;
            }

            Button next = Btn(c, "NextButton", "NEXT", new Vector2(-90, 130), anchor: new Vector2(1, 0));
            ((RectTransform)next.transform).pivot = new Vector2(1, 0);
            ((RectTransform)next.transform).sizeDelta = new Vector2(300, 84);
            TMP_Text nextLabel = next.GetComponentInChildren<TMP_Text>();
            Button skip = Btn(c, "SkipButton", "SKIP", new Vector2(-40, -14), secondary: true, anchor: new Vector2(1, 1));
            ((RectTransform)skip.transform).pivot = new Vector2(1, 1);
            ((RectTransform)skip.transform).sizeDelta = new Vector2(170, 60);
            skip.GetComponentInChildren<TMP_Text>().fontSize = 24;
            Text(c, "Hint", "SPACE / ENTER  next     ←  back     ESC  skip", 18, TextAlignmentOptions.Right, new Color(1f, 1f, 1f, 0.45f),
                new Vector2(1, 0), new Vector2(-90, 34), new Vector2(700, 24), null);

            StoryController story = canvas.gameObject.AddComponent<StoryController>();
            Set(story, "storyCamera", cam);
            Set(story, "captionCard", card);
            Set(story, "captionGroup", captionGroup);
            Set(story, "chapterText", chapter);
            Set(story, "titleText", title);
            Set(story, "bodyText", body);
            Set(story, "iconImage", icon);
            SetObjectArray(story, "dots", dots);
            Set(story, "nextButton", next);
            Set(story, "nextLabel", nextLabel);
            Set(story, "skipButton", skip);

            SerializedObject so = new SerializedObject(story);
            SerializedProperty slides = so.FindProperty("slides");
            slides.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                SerializedProperty sp = slides.GetArrayElementAtIndex(i);
                sp.FindPropertyRelative("title").stringValue = defs[i].title;
                sp.FindPropertyRelative("body").stringValue = defs[i].body;
                sp.FindPropertyRelative("shot").objectReferenceValue = defs[i].shot;
                sp.FindPropertyRelative("accent").colorValue = defs[i].accent;
                sp.FindPropertyRelative("icon").objectReferenceValue = SpriteAsset(defs[i].icon);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            CreateEventSystem(uiRoot);
        }

        // ---------------- Helpers ----------------

        private static Camera StoryCamera(Vector3 position)
        {
            GameObject camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(new GameObject("[Cameras]").transform, false);
            camGo.transform.position = position;
            Camera cam = camGo.AddComponent<Camera>();
            cam.cullingMask = ~((1 << MinimapLayer) | (1 << HeroStageLayer));
            cam.farClipPlane = 300f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            return cam;
        }

        /// <summary>Camera pose marker for a story page, looking at a target (both local to the set).</summary>
        private static Transform Shot(Transform set, Vector3 localPos, Vector3 localTarget)
        {
            Transform shot = Empty("CameraShot", set, localPos).transform;
            shot.LookAt(set.TransformPoint(localTarget));
            return shot;
        }

        private static void PointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range)
        {
            Light l = Empty(name, parent, localPos).AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
        }

        private static void Ring(Transform set, GameObject[] pool, int count, float minR, float maxR, int seed, float minS, float maxS)
        {
            Random.State state = Random.state;
            Random.InitState(seed);
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float r = Random.Range(minR, maxR);
                if (Mathf.Sin(a) < -0.35f) continue; // keep the camera side (-Z) open
                Place(Pick(pool), set, set.position + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), Random.Range(0f, 360f), Vector3.one * Random.Range(minS, maxS));
            }
            Random.state = state;
        }

        private static void Actor(GameObject model, RuntimeAnimatorController controller, float animSpeed, string trigger, float interval, float moveSpeed, float loop = 6f)
        {
            Animator anim = model.GetComponentInChildren<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            StoryActor actor = model.AddComponent<StoryActor>();
            Set(actor, "animator", anim);
            Set(actor, "controller", controller);
            SerializedObject so = new SerializedObject(actor);
            so.FindProperty("animSpeed").floatValue = animSpeed;
            so.FindProperty("trigger").stringValue = trigger;
            so.FindProperty("triggerInterval").floatValue = interval;
            so.FindProperty("moveSpeed").floatValue = moveSpeed;
            so.FindProperty("loopDistance").floatValue = loop;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Gold crown on the Orc King's head bone (falls back to the top of the model).</summary>
        private static void Crown(Transform orc, Material gold)
        {
            Transform head = null;
            foreach (Transform t in orc.GetComponentsInChildren<Transform>())
                if (t.name.ToLowerInvariant().Contains("head")) { head = t; break; }
            Bounds b = ModelUtility.GetBounds(orc.gameObject);
            Vector3 top = new Vector3(b.center.x, b.max.y + 0.05f, b.center.z);

            Material crownMat = Lit("Characters", "M_Crown", new Color(1f, 0.78f, 0.2f), 0.9f, 0.7f, new Color(0.6f, 0.4f, 0.05f));
            GameObject crown = Empty("Crown", head != null ? head : orc, Vector3.zero);
            crown.transform.position = top;
            crown.transform.rotation = Quaternion.identity;
            Vector3 parentScale = crown.transform.lossyScale;
            crown.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z); // world-sized children
            Primitive(PrimitiveType.Cylinder, "Band", crown.transform, Vector3.zero, new Vector3(0.75f, 0.12f, 0.75f), crownMat);
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                Primitive(PrimitiveType.Cube, "Spike", crown.transform, new Vector3(Mathf.Cos(a) * 0.32f, 0.2f, Mathf.Sin(a) * 0.32f),
                    new Vector3(0.12f, 0.3f, 0.12f), crownMat).transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
            _ = gold;
        }

        private static void LocalVolume(Transform set, string profileName, Color tint, float saturation, float exposure, float bloom)
        {
            GameObject go = Empty("LocalVolume", set, Vector3.zero);
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(60f, 30f, 60f);
            Volume v = go.AddComponent<Volume>();
            v.isGlobal = false;
            v.blendDistance = 5f;
            v.priority = 10f;
            v.sharedProfile = CreateVolumeProfile(profileName, tint, saturation, exposure, bloom);
        }

        private static TMP_Text Centered(TMP_Text t, Vector2 pos)
        {
            t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            t.rectTransform.anchoredPosition = pos;
            return t;
        }
    }
}
