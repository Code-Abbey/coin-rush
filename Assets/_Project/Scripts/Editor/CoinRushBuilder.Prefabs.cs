using CoinRush.CameraSystem;
using CoinRush.Collectibles;
using CoinRush.Enemies;
using CoinRush.Level;
using CoinRush.Player;
using System.Linq;
using CoinRush.Utilities;
using UnityEditor;
using UnityEngine;

namespace CoinRush.EditorTools
{
    /// <summary>Materials + every prefab used by the worlds.</summary>
    public static partial class CoinRushBuilder
    {
        // ---- Materials ----
        private static Material mPlayerAccent, mEye, mWhite, mAlert, mRoof,
            mGem, mSpeed, mMagnet, mShieldOrb, mPlatform, mPadBase, mCrate,
            mTrunk, mRock, mSandstone, mSandDark, mIce, mSnow, mCrystal, mMetal;
        private static Material sgCoin, sgPortal, sgPortalBarrier, sgShield, sgMagnetRing, sgJumpPad, sgLava, sgFrostWater;
        private static Material iconPlayer, iconEnemy, iconCoin, iconPortal, iconPower;

        // ---- Prefabs ----
        private static GameObject playerPrefab, blobPrefab, brutePrefab, coinPrefab, gemPrefab, healthPrefab, speedPrefab,
            magnetPrefab, shieldPrefab, portalPrefab, jumpPadPrefab, platformPrefab, lavaPrefab, icePrefab, bladePrefab,
            treePrefab, rockPrefab, cratePrefab, pillarPrefab, crystalPrefab, snowmanPrefab;
        private static ParticleSystem collectFxPrefab;

        private static void CreateMaterials()
        {
            particleMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");

            mPlayerAccent = Lit("Characters", "M_PlayerAccent", new Color(0.95f, 0.95f, 1f), 0f, 0.8f);
            mEye = Lit("Characters", "M_Eye", new Color(0.04f, 0.04f, 0.07f), 0f, 0.9f);
            mWhite = Lit("Characters", "M_White", Color.white, 0f, 0.6f);
            mRoof = Lit("Environment", "M_Roof", new Color(0.85f, 0.35f, 0.25f), 0f, 0.3f);
            mAlert = Lit("Characters", "M_Alert", new Color(1f, 0.9f, 0.1f), 0f, 0.5f, new Color(2f, 1.6f, 0f));

            mGem = Lit("Pickups", "M_Gem", new Color(0.25f, 0.95f, 1f), 0.2f, 0.95f, new Color(0f, 0.9f, 1.1f));
            mSpeed = Lit("Pickups", "M_SpeedBoost", new Color(0.3f, 0.6f, 1f), 0.3f, 0.9f, new Color(0.2f, 0.6f, 1.6f));
            mMagnet = Lit("Pickups", "M_Magnet", new Color(0.95f, 0.2f, 0.25f), 0.6f, 0.7f, new Color(0.6f, 0f, 0.05f));
            mShieldOrb = Lit("Pickups", "M_ShieldOrb", new Color(0.4f, 1f, 0.85f), 0.2f, 0.9f, new Color(0.2f, 1.2f, 0.9f));

            mPlatform = Lit("Environment", "M_Platform", new Color(0.55f, 0.6f, 0.75f), 0.2f, 0.4f);
            mPadBase = Lit("Environment", "M_PadBase", new Color(0.25f, 0.28f, 0.35f), 0.5f, 0.5f);
            mCrate = Lit("Environment", "M_Crate", new Color(0.72f, 0.5f, 0.3f), 0f, 0.2f);
            mTrunk = Lit("Environment", "M_Trunk", new Color(0.45f, 0.3f, 0.18f), 0f, 0.15f);
            mRock = Lit("Environment", "M_Rock", new Color(0.5f, 0.5f, 0.52f), 0f, 0.15f);
            mSandstone = Lit("Environment", "M_Sandstone", new Color(0.86f, 0.7f, 0.48f), 0f, 0.15f);
            mSandDark = Lit("Environment", "M_SandstoneDark", new Color(0.66f, 0.48f, 0.3f), 0f, 0.15f);
            mIce = Lit("Environment", "M_Ice", new Color(0.7f, 0.88f, 1f), 0.1f, 0.95f);
            mSnow = Lit("Environment", "M_Snow", new Color(0.92f, 0.95f, 1f), 0f, 0.35f);
            mCrystal = Lit("Environment", "M_IceCrystal", new Color(0.55f, 0.85f, 1f), 0.3f, 0.95f, new Color(0.1f, 0.5f, 0.9f));
            mMetal = Lit("Environment", "M_Metal", new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.7f);

            // ---- Shader Graph materials (property names = Shader Graph reference names) ----
            sgCoin = GraphMat("MSG_Coin", "SG_Coin",
                ("_CoinColor", new Color(1f, 0.8f, 0.2f)), ("_RimColor", new Color(1.8f, 1.2f, 0.25f)), ("_RimPower", 2.5f), ("_ShineSpeed", 1.6f),
                ("_MetallicAmount", 0.35f), ("_SmoothnessAmount", 0.6f));
            sgPortal = GraphMat("MSG_Portal", "SG_Portal",
                ("_ColorA", new Color(1.8f, 0.5f, 4f)), ("_ColorB", new Color(0.2f, 1.8f, 3.2f)), ("_SwirlSpeed", 0.3f), ("_Intensity", 0.3f), ("_TwirlStrength", 9f));
            sgPortalBarrier = GraphMat("MSG_PortalBarrier", "SG_ForceField",
                ("_Color", new Color(1.5f, 0.4f, 1.2f)), ("_FresnelPower", 1.5f), ("_StripeSpeed", 2f), ("_StripeDensity", 30f), ("_BaseAlpha", 0.12f));
            sgShield = GraphMat("MSG_Shield", "SG_ForceField",
                ("_Color", new Color(0.3f, 2f, 1.6f)), ("_FresnelPower", 2.5f), ("_StripeSpeed", 1f), ("_StripeDensity", 18f), ("_BaseAlpha", 0.05f));
            sgMagnetRing = GraphMat("MSG_MagnetRing", "SG_ForceField",
                ("_Color", new Color(2f, 0.3f, 0.35f)), ("_FresnelPower", 1f), ("_StripeSpeed", 3f), ("_StripeDensity", 10f), ("_BaseAlpha", 0.1f));
            sgJumpPad = GraphMat("MSG_JumpPad", "SG_ForceField",
                ("_Color", new Color(0.4f, 2.2f, 0.6f)), ("_FresnelPower", 1.2f), ("_StripeSpeed", 4f), ("_StripeDensity", 12f), ("_BaseAlpha", 0.25f));
            sgLava = GraphMat("MSG_Lava", "SG_Lava",
                ("_HotColor", new Color(1f, 0.55f, 0.08f)), ("_CoolColor", new Color(0.25f, 0.02f, 0.01f)), ("_FlowSpeed", 0.05f),
                ("_NoiseScale", 5f), ("_EmissionStrength", 3.5f));
            sgFrostWater = GraphMat("MSG_FrostWater", "SG_Lava",
                ("_HotColor", new Color(0.6f, 0.95f, 1f)), ("_CoolColor", new Color(0.02f, 0.08f, 0.2f)), ("_FlowSpeed", 0.03f),
                ("_NoiseScale", 4f), ("_EmissionStrength", 1.2f));

            // ---- Minimap icons (unlit cut-out, only seen by the minimap camera) ----
            iconPlayer = UnlitIcon("M_IconPlayer", TextureAsset("icon_arrow"), new Color(0.3f, 0.85f, 1f));
            iconEnemy = UnlitIcon("M_IconEnemy", TextureAsset("ui_circle"), new Color(1f, 0.25f, 0.3f));
            iconCoin = UnlitIcon("M_IconCoin", TextureAsset("ui_circle"), new Color(1f, 0.82f, 0.2f));
            iconPortal = UnlitIcon("M_IconPortal", TextureAsset("icon_portal"), Color.white);
            iconPower = UnlitIcon("M_IconPower", TextureAsset("ui_circle"), new Color(0.6f, 1f, 0.7f));
        }

        // =====================================================================
        // Prefabs
        // =====================================================================

        private static void CreatePrefabs()
        {
            collectFxPrefab = SavePrefab(BuildCollectFx(), PrefabDir + "/VFX/FX_CollectBurst.prefab").GetComponent<ParticleSystem>();

            playerPrefab = SavePrefab(BuildPlayer(), PrefabDir + "/Player/Player.prefab");
            blobPrefab = SavePrefab(BuildEnemy("Enemy_Hog", blobStats, ModelAsset(KenneyDir + "/CubePets/Models/animal-hog.fbx"), petController, 1f, 1.05f, false),
                PrefabDir + "/Enemies/Enemy_Hog.prefab");
            brutePrefab = SavePrefab(BuildEnemy("Enemy_Orc", bruteStats, ModelAsset(KenneyDir + "/MiniDungeon/Models/character-orc.fbx"), orcController, 1.35f, 2.1f, true),
                PrefabDir + "/Enemies/Enemy_Orc.prefab");

            coinPrefab = SavePrefab(BuildCoin(), PrefabDir + "/Collectibles/Coin.prefab");
            gemPrefab = SavePrefab(BuildGem(), PrefabDir + "/Collectibles/Gem.prefab");
            healthPrefab = SavePrefab(BuildHealthPickup(), PrefabDir + "/Collectibles/HealthPack.prefab");
            speedPrefab = SavePrefab(BuildSpeedPickup(), PrefabDir + "/Collectibles/SpeedBoost.prefab");
            magnetPrefab = SavePrefab(BuildMagnetPickup(), PrefabDir + "/Collectibles/Magnet.prefab");
            shieldPrefab = SavePrefab(BuildShieldPickup(), PrefabDir + "/Collectibles/Shield.prefab");

            portalPrefab = SavePrefab(BuildPortal(), PrefabDir + "/Level/Portal.prefab");
            jumpPadPrefab = SavePrefab(BuildJumpPad(), PrefabDir + "/Level/JumpPad.prefab");
            platformPrefab = SavePrefab(BuildMovingPlatform(), PrefabDir + "/Level/MovingPlatform.prefab");
            lavaPrefab = SavePrefab(BuildHazardPool("LavaPool", sgLava, 12f), PrefabDir + "/Level/LavaPool.prefab");
            icePrefab = SavePrefab(BuildIcePatch(), PrefabDir + "/Level/IcePatch.prefab");
            bladePrefab = SavePrefab(BuildSpinningBlade(), PrefabDir + "/Level/SpinningBlade.prefab");

            CreateModelProps(); // Kenney Nature / Platformer kit props (trees, rocks, cacti, ruins, decor)
            crystalPrefab = SavePrefab(BuildIceCrystal(), PrefabDir + "/Props/IceCrystal.prefab");
            snowmanPrefab = SavePrefab(BuildSnowman(), PrefabDir + "/Props/Snowman.prefab");
        }

        private static GameObject SavePrefab(GameObject go, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Flat quad on the Minimap layer, high above the object so the top-down camera draws it on top.</summary>
        private static void AddMinimapIcon(Transform parent, Material icon, float size, float height = 25f)
        {
            GameObject quad = Primitive(PrimitiveType.Quad, "MinimapIcon", parent, new Vector3(0f, height, 0f),
                new Vector3(size, size, size), icon);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.layer = MinimapLayer;
            Renderer r = quad.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private static ParticleSystem MakeParticles(string name, Transform parent, Vector3 localPos, Color color, bool loop,
            float rate, int burst, float speed, float lifetime, float size, ParticleSystemShapeType shape, float radius, float gravity = 0f)
        {
            GameObject go = Empty(name, parent, localPos);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = loop;
            main.playOnAwake = loop;
            main.duration = loop ? 5f : 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 500;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
            if (burst > 0) emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = shape;
            sh.radius = radius;

            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = particleMat;
            return ps;
        }

        private static GameObject BuildCollectFx()
        {
            GameObject root = Empty("FX_CollectBurst", null, Vector3.zero);
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            root.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMat;
            return root;
        }

        // ------------------------------------------------------------------ player

        private static GameObject BuildPlayer()
        {
            GameObject root = new GameObject("Player") { tag = "Player" };

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.45f;
            cc.slopeLimit = 50f;

            // Kinematic Rigidbody: lets the CharacterController fire OnTrigger events against static
            // triggers (portal, jump pads, lava, ice). Physics never moves it – the controller does.
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            PlayerController controller = root.AddComponent<PlayerController>();
            PlayerHealth health = root.AddComponent<PlayerHealth>();
            PlayerPowerUps powerUps = root.AddComponent<PlayerPowerUps>();

            // Visual: the chosen Kenney hero is spawned into ModelSocket at runtime by PlayerCharacterLoader.
            // A default hero is baked in so the prefab looks right in the editor.
            Transform socket = Empty("ModelSocket", root.transform, Vector3.zero).transform;
            GameObject preview = SpawnModel(characterDatabase.Get(0).model, socket, Vector3.zero, characterDatabase.heroHeight);
            Animator previewAnim = preview.GetComponentInChildren<Animator>();
            if (previewAnim != null) previewAnim.runtimeAnimatorController = heroController;
            Transform visual = socket;

            // Power-up visuals (Shader Graph force field).
            GameObject shield = Primitive(PrimitiveType.Sphere, "ShieldBubble", root.transform, new Vector3(0f, 0.95f, 0f), Vector3.one * 2.4f, sgShield);
            GameObject magnet = Primitive(PrimitiveType.Cylinder, "MagnetRing", root.transform, new Vector3(0f, 0.05f, 0f), new Vector3(4f, 0.02f, 4f), sgMagnetRing);
            shield.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            magnet.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject trailGo = Empty("BoostTrail", root.transform, new Vector3(0f, 0.6f, 0f));
            TrailRenderer trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.35f;
            trail.startWidth = 0.7f;
            trail.endWidth = 0f;
            trail.sharedMaterial = particleMat;
            trail.startColor = new Color(0.3f, 0.7f, 1f, 0.9f);
            trail.endColor = new Color(0.3f, 0.7f, 1f, 0f);
            trail.emitting = false;

            ParticleSystem dashFx = MakeParticles("DashFX", root.transform, new Vector3(0f, 0.8f, 0f), new Color(0.6f, 0.9f, 1f),
                false, 0f, 25, 4f, 0.4f, 0.35f, ParticleSystemShapeType.Sphere, 0.5f);

            // Footstep / landing dust (emitted on demand by PlayerFootsteps).
            ParticleSystem footDust = MakeParticles("FootDust", root.transform, new Vector3(0f, 0.1f, 0f), new Color(0.85f, 0.8f, 0.7f, 0.6f),
                true, 0f, 0, 1.2f, 0.6f, 0.35f, ParticleSystemShapeType.Circle, 0.25f, -0.05f);
            ParticleSystem landDust = MakeParticles("LandDust", root.transform, new Vector3(0f, 0.1f, 0f), new Color(0.9f, 0.85f, 0.75f, 0.7f),
                true, 0f, 0, 3.5f, 0.7f, 0.5f, ParticleSystemShapeType.Circle, 0.4f, -0.05f);
            footDust.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // emit upwards from a flat ring
            landDust.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            PlayerFootsteps steps = root.AddComponent<PlayerFootsteps>();
            Set(steps, "footDust", footDust);
            Set(steps, "landDust", landDust);

            AddMinimapIcon(root.transform, iconPlayer, 6f, 30f);

            Set(controller, "stats", playerStats);
            Set(controller, "dashEffect", dashFx);
            SetInt(controller, "groundMask", ~((1 << PlayerLayer) | (1 << CollectibleLayer) | (1 << MinimapLayer)));

            SerializedObject healthSo = new SerializedObject(health);
            SerializedProperty renderers = healthSo.FindProperty("blinkRenderers");
            Renderer[] all = visual.GetComponentsInChildren<Renderer>();
            renderers.arraySize = all.Length;
            for (int i = 0; i < all.Length; i++) renderers.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            PlayerCharacterLoader loader = root.AddComponent<PlayerCharacterLoader>();
            Set(loader, "database", characterDatabase);
            Set(loader, "modelSocket", socket);
            root.AddComponent<PlayerAnimator>();

            Set(powerUps, "boostTrail", trail);
            Set(powerUps, "magnetVisual", magnet);
            Set(powerUps, "shieldVisual", shield);

            SetLayerRecursive(root, PlayerLayer);
            return root;
        }

        // ------------------------------------------------------------------ enemies

        private static GameObject BuildEnemy(string name, Data.EnemyStats stats, GameObject model, RuntimeAnimatorController controller,
            float scale, float modelHeight, bool brute)
        {
            GameObject root = new GameObject(name);

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.3f * scale;
            cc.radius = 0.55f * scale;
            cc.center = new Vector3(0f, 0.65f * scale, 0f);
            cc.stepOffset = 0.3f;

            EnemyBehaviour behaviour = root.AddComponent<EnemyBehaviour>();
            EnemyVisual visual = root.AddComponent<EnemyVisual>();
            EnemyAnimator enemyAnim = root.AddComponent<EnemyAnimator>();

            // Animated Kenney model (Cube Pets hog / Mini Dungeon orc).
            Transform bodyRoot = Empty("BodyRoot", root.transform, Vector3.zero).transform;
            GameObject body = SpawnModel(model, bodyRoot, Vector3.zero, modelHeight);
            body.name = "Model";
            Animator animator = body.GetComponentInChildren<Animator>();
            if (animator == null) animator = body.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            GameObject alert = Empty("AlertMarker", root.transform, new Vector3(0f, modelHeight + 0.6f, 0f));
            alert.AddComponent<Billboard>();
            Primitive(PrimitiveType.Cube, "Bar", alert.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.14f, 0.4f, 0.14f), mAlert);
            Primitive(PrimitiveType.Cube, "Dot", alert.transform, new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.14f, 0.14f), mAlert);

            AddMinimapIcon(root.transform, iconEnemy, brute ? 3.6f : 2.6f, 26f);

            Set(behaviour, "stats", stats);
            SetInt(behaviour, "sightBlockers", 1 << ObstacleLayer);
            Set(enemyAnim, "animator", animator);
            Set(enemyAnim, "controller", controller);
            Set(visual, "bodyRenderer", body.GetComponentInChildren<Renderer>());
            Set(visual, "alertMarker", alert);
            // Textured models: tint only slightly (white = original colours), no squash & stretch.
            SerializedObject so = new SerializedObject(visual);
            so.FindProperty("patrolColor").colorValue = Color.white;
            so.FindProperty("chaseColor").colorValue = new Color(1f, 0.55f, 0.5f);
            so.FindProperty("searchColor").colorValue = new Color(1f, 0.85f, 0.55f);
            so.FindProperty("bounceAmount").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();

            SetLayerRecursive(root, EnemyLayer);
            return root;
        }

        // ------------------------------------------------------------------ pickups

        private static GameObject PickupRoot<T>(string name, float radius) where T : Pickup
        {
            GameObject root = new GameObject(name);
            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;
            col.center = new Vector3(0f, 0.8f, 0f);
            Rigidbody rb = root.AddComponent<Rigidbody>(); // kinematic body => reliable trigger events
            rb.isKinematic = true;
            rb.useGravity = false;
            T pickup = root.AddComponent<T>();
            Set(pickup, "collectEffectPrefab", collectFxPrefab);
            return root;
        }

        private static Transform PickupVisual(GameObject root, float height = 0.8f)
        {
            Transform visual = Empty("Visual", root.transform, new Vector3(0f, height, 0f)).transform;
            Set(root.GetComponent<Pickup>(), "visual", visual);
            return visual;
        }

        private static GameObject BuildCoin()
        {
            GameObject root = PickupRoot<Coin>("Coin", 0.75f);
            Transform v = PickupVisual(root);
            // Kenney Platformer Kit coin mesh rendered with our SG_Coin Shader Graph material.
            GameObject coinModel = SpawnModel(Platformer("coinGold"), v, Vector3.zero, 0.85f);
            coinModel.transform.localPosition -= Vector3.up * 0.42f;
            foreach (Renderer r in coinModel.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = Enumerable.Repeat(sgCoin, r.sharedMaterials.Length).ToArray();
            AddMinimapIcon(root.transform, iconCoin, 1.2f, 22f);
            Set(root.GetComponent<Coin>(), "data", coinData);
            SetLayerRecursive(root, CollectibleLayer);
            return root;
        }

        private static GameObject BuildGem()
        {
            GameObject root = PickupRoot<Coin>("Gem", 0.8f);
            Transform v = PickupVisual(root, 0.95f);
            GameObject jewel = SpawnModel(Platformer("jewel"), v, Vector3.zero, 0.8f);
            jewel.transform.localPosition -= Vector3.up * 0.4f;
            foreach (Renderer r in jewel.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = Enumerable.Repeat(mGem, r.sharedMaterials.Length).ToArray();
            Light glow = Empty("Glow", v, Vector3.zero).AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.3f, 1f, 1f);
            glow.range = 3f;
            glow.intensity = 2f;
            AddMinimapIcon(root.transform, iconCoin, 1.8f, 22f);
            Set(root.GetComponent<Coin>(), "data", gemData);
            SetLayerRecursive(root, CollectibleLayer);
            return root;
        }

        private static GameObject BuildHealthPickup()
        {
            GameObject root = PickupRoot<HealthPickup>("HealthPack", 0.85f);
            Transform v = PickupVisual(root);
            GameObject heart = SpawnModel(Platformer("heart"), v, Vector3.zero, 0.9f);
            heart.transform.localPosition -= Vector3.up * 0.45f;
            AddMinimapIcon(root.transform, iconPower, 2f, 22f);
            SetLayerRecursive(root, CollectibleLayer);
            return root;
        }

        private static GameObject BuildSpeedPickup()
        {
            GameObject root = PickupRoot<SpeedBoostPickup>("SpeedBoost", 0.85f);
            Transform v = PickupVisual(root);
            Primitive(PrimitiveType.Sphere, "Orb", v, Vector3.zero, Vector3.one * 0.55f, mSpeed);
            Primitive(PrimitiveType.Cube, "ChevronA", v, new Vector3(0f, 0f, 0.1f), new Vector3(0.12f, 0.12f, 0.55f), mPlayerAccent)
                .transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Primitive(PrimitiveType.Cube, "ChevronB", v, new Vector3(0f, 0f, 0.1f), new Vector3(0.12f, 0.12f, 0.55f), mPlayerAccent)
                .transform.localRotation = Quaternion.Euler(0f, -45f, 0f);
            AddMinimapIcon(root.transform, iconPower, 2f, 22f);
            SetLayerRecursive(root, CollectibleLayer);
            return root;
        }

        private static GameObject BuildMagnetPickup()
        {
            GameObject root = PickupRoot<MagnetPickup>("Magnet", 0.85f);
            Transform v = PickupVisual(root);
            Primitive(PrimitiveType.Cube, "ArmL", v, new Vector3(-0.22f, 0f, 0f), new Vector3(0.18f, 0.55f, 0.18f), mMagnet);
            Primitive(PrimitiveType.Cube, "ArmR", v, new Vector3(0.22f, 0f, 0f), new Vector3(0.18f, 0.55f, 0.18f), mMagnet);
            Primitive(PrimitiveType.Cube, "Bridge", v, new Vector3(0f, 0.28f, 0f), new Vector3(0.62f, 0.18f, 0.18f), mMagnet);
            Primitive(PrimitiveType.Cube, "TipL", v, new Vector3(-0.22f, -0.32f, 0f), new Vector3(0.19f, 0.12f, 0.19f), mMetal);
            Primitive(PrimitiveType.Cube, "TipR", v, new Vector3(0.22f, -0.32f, 0f), new Vector3(0.19f, 0.12f, 0.19f), mMetal);
            AddMinimapIcon(root.transform, iconPower, 2f, 22f);
            SetLayerRecursive(root, CollectibleLayer);
            return root;
        }

        private static GameObject BuildShieldPickup()
        {
            GameObject root = PickupRoot<ShieldPickup>("Shield", 0.85f);
            Transform v = PickupVisual(root);
            Primitive(PrimitiveType.Sphere, "Core", v, Vector3.zero, Vector3.one * 0.35f, mShieldOrb);
            GameObject bubble = Primitive(PrimitiveType.Sphere, "Bubble", v, Vector3.zero, Vector3.one * 0.75f, sgShield);
            bubble.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            AddMinimapIcon(root.transform, iconPower, 2f, 22f);
            SetLayerRecursive(root, CollectibleLayer);
            return root;
        }

        // ------------------------------------------------------------------ level pieces

        private static GameObject BuildPortal()
        {
            GameObject root = new GameObject("Portal");
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 2.2f, 0f);
            trigger.size = new Vector3(3.2f, 4.4f, 1.6f);
            Portal portal = root.AddComponent<Portal>();

            // Stone frame (solid, blocks sight).
            Transform frame = Empty("Frame", root.transform, Vector3.zero).transform;
            GameObject l = Primitive(PrimitiveType.Cube, "PillarL", frame, new Vector3(-2.1f, 2.4f, 0f), new Vector3(0.8f, 4.8f, 0.9f), mPlatform, true);
            GameObject r = Primitive(PrimitiveType.Cube, "PillarR", frame, new Vector3(2.1f, 2.4f, 0f), new Vector3(0.8f, 4.8f, 0.9f), mPlatform, true);
            GameObject top = Primitive(PrimitiveType.Cube, "Lintel", frame, new Vector3(0f, 5.1f, 0f), new Vector3(5f, 0.7f, 1f), mPlatform, true);
            Primitive(PrimitiveType.Cube, "Step", frame, new Vector3(0f, 0.1f, 0f), new Vector3(5.4f, 0.2f, 2.4f), mPadBase, true);
            Primitive(PrimitiveType.Sphere, "GemL", frame, new Vector3(-2.1f, 5.1f, 0.5f), Vector3.one * 0.5f, mGem);
            Primitive(PrimitiveType.Sphere, "GemR", frame, new Vector3(2.1f, 5.1f, 0.5f), Vector3.one * 0.5f, mGem);
            foreach (GameObject g in new[] { l, r, top }) g.layer = ObstacleLayer;

            // Swirl surface (SG_Portal) – quad facing both ways.
            GameObject swirl = Primitive(PrimitiveType.Quad, "Swirl", root.transform, new Vector3(0f, 2.3f, 0f), new Vector3(3.4f, 4.4f, 1f), sgPortal);
            swirl.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Closed barrier (SG_ForceField), disabled when the portal opens.
            GameObject barrier = Primitive(PrimitiveType.Cube, "ClosedBarrier", root.transform, new Vector3(0f, 2.3f, 0f), new Vector3(3.4f, 4.4f, 0.3f), sgPortalBarrier);
            barrier.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            ParticleSystem openFx = MakeParticles("OpenParticles", root.transform, new Vector3(0f, 2.3f, 0f), new Color(0.8f, 0.5f, 1f),
                true, 30f, 0, 1.5f, 1.5f, 0.25f, ParticleSystemShapeType.Circle, 1.8f, -0.1f);
            openFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule m = openFx.main;
            m.playOnAwake = false;

            Light light = Empty("PortalLight", root.transform, new Vector3(0f, 2.5f, 1.5f)).AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.75f, 0.45f, 1f);
            light.range = 10f;
            light.intensity = 0.5f;

            AddMinimapIcon(root.transform, iconPortal, 7f, 28f);

            Set(portal, "swirlRenderer", swirl.GetComponent<Renderer>());
            Set(portal, "closedBarrier", barrier);
            Set(portal, "openParticles", openFx);
            Set(portal, "portalLight", light);
            return root;
        }

        private static GameObject BuildJumpPad()
        {
            GameObject root = new GameObject("JumpPad");
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.9f, 0f);
            trigger.size = new Vector3(2.4f, 1.8f, 2.4f);
            JumpPad pad = root.AddComponent<JumpPad>();

            // Flat box collider: a squashed cylinder's default capsule collider becomes a 1.5 m dome that the
            // player would stand on ABOVE the launch trigger.
            GameObject padBase = Primitive(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0f, 0.12f, 0f), new Vector3(2.8f, 0.12f, 2.8f), mPadBase);
            padBase.AddComponent<BoxCollider>().size = new Vector3(0.9f, 2f, 0.9f);
            Transform top = Primitive(PrimitiveType.Cylinder, "Top", root.transform, new Vector3(0f, 0.3f, 0f), new Vector3(2.2f, 0.06f, 2.2f), sgJumpPad).transform;
            Primitive(PrimitiveType.Cylinder, "Glow", root.transform, new Vector3(0f, 1.2f, 0f), new Vector3(2f, 1f, 2f), sgJumpPad)
                .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Set(pad, "padTop", top);
            return root;
        }

        private static GameObject BuildMovingPlatform()
        {
            GameObject root = Primitive(PrimitiveType.Cube, "MovingPlatform", null, Vector3.zero, new Vector3(4f, 0.6f, 4f), mPlatform, true);
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            root.AddComponent<MovingPlatform>();
            Primitive(PrimitiveType.Cube, "Stripe", root.transform, new Vector3(0f, 0.51f, 0f), new Vector3(0.9f, 0.02f, 0.15f), mAlert);
            root.layer = ObstacleLayer;
            return root;
        }

        /// <summary>Lava / freezing water pool: Shader Graph surface + damaging trigger on the Hazard layer.</summary>
        private static GameObject BuildHazardPool(string name, Material surface, float damage)
        {
            GameObject root = new GameObject(name) { layer = HazardLayer };
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.5f, 0f);
            trigger.size = new Vector3(1f, 1f, 1f);
            DamageZone zone = root.AddComponent<DamageZone>();
            SetFloat(zone, "damagePerTick", damage);

            GameObject plane = Primitive(PrimitiveType.Cube, "Surface", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(1f, 0.05f, 1f), surface);
            plane.layer = HazardLayer;
            plane.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return root;
        }

        private static GameObject BuildIcePatch()
        {
            GameObject root = new GameObject("IcePatch");
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.5f, 0f);
            trigger.size = Vector3.one;
            root.AddComponent<SurfaceZone>();
            Primitive(PrimitiveType.Cube, "Surface", root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(1f, 0.04f, 1f), mIce);
            return root;
        }

        private static GameObject BuildSpinningBlade()
        {
            GameObject root = new GameObject("SpinningBlade");
            Primitive(PrimitiveType.Cylinder, "Hub", root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1f, 0.6f, 1f), mPadBase, true).layer = ObstacleLayer;

            GameObject rotor = Empty("Rotor", root.transform, new Vector3(0f, 0.9f, 0f));
            rotor.AddComponent<Rotator>();
            SetVector3(rotor.GetComponent<Rotator>(), "degreesPerSecond", new Vector3(0f, 110f, 0f));
            Rigidbody rb = rotor.AddComponent<Rigidbody>(); // moving trigger needs a kinematic body
            rb.isKinematic = true;

            GameObject bar = Primitive(PrimitiveType.Cube, "Blade", rotor.transform, Vector3.zero, new Vector3(9f, 0.35f, 0.35f), mMetal);
            BoxCollider hit = rotor.AddComponent<BoxCollider>();
            hit.isTrigger = true;
            hit.size = new Vector3(9f, 0.9f, 0.6f);
            DamageZone zone = rotor.AddComponent<DamageZone>();
            SetFloat(zone, "damagePerTick", 18f);
            SetFloat(zone, "knockback", 14f);
            SpawnModel(Platformer("saw"), rotor.transform, new Vector3(-4.5f, -0.6f, 0f), 1.4f).name = "SawL";
            SpawnModel(Platformer("saw"), rotor.transform, new Vector3(4.5f, -0.6f, 0f), 1.4f).name = "SawR";
            _ = bar;
            return root;
        }

        // ------------------------------------------------------------------ props (solid, Obstacle layer)

        private static GameObject Prop(string name)
        {
            GameObject root = new GameObject(name) { layer = ObstacleLayer, isStatic = true };
            return root;
        }

        private static GameObject Solid(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = Primitive(type, name, parent, pos, scale, mat, keepCollider: true);
            go.layer = ObstacleLayer;
            go.isStatic = true;
            return go;
        }

        private static GameObject BuildIceCrystal()
        {
            GameObject root = Prop("IceCrystal");
            Solid(PrimitiveType.Cube, "Shard", root.transform, new Vector3(0f, 1.6f, 0f), new Vector3(1f, 3.2f, 1f), mCrystal)
                .transform.localRotation = Quaternion.Euler(0f, 45f, 8f);
            Primitive(PrimitiveType.Cube, "ShardSmall", root.transform, new Vector3(0.8f, 0.8f, 0.3f), new Vector3(0.6f, 1.6f, 0.6f), mCrystal)
                .transform.localRotation = Quaternion.Euler(15f, 20f, -20f);
            return root;
        }

        private static GameObject BuildSnowman()
        {
            GameObject root = Prop("Snowman");
            Solid(PrimitiveType.Sphere, "Bottom", root.transform, new Vector3(0f, 0.8f, 0f), Vector3.one * 1.6f, mSnow);
            Primitive(PrimitiveType.Sphere, "Middle", root.transform, new Vector3(0f, 2f, 0f), Vector3.one * 1.15f, mSnow);
            Primitive(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0f, 2.9f, 0f), Vector3.one * 0.8f, mSnow);
            Primitive(PrimitiveType.Cylinder, "Nose", root.transform, new Vector3(0f, 2.9f, 0.45f), new Vector3(0.1f, 0.2f, 0.1f), mRoof)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Primitive(PrimitiveType.Cylinder, "Hat", root.transform, new Vector3(0f, 3.4f, 0f), new Vector3(0.6f, 0.3f, 0.6f), mEye);
            return root;
        }
    }
}
