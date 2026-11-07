using CoinRush.Core;
using CoinRush.Data;
using CoinRush.UI;
using CoinRush.Utilities;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CoinRush.EditorTools
{
    /// <summary>TextMeshPro-based UI generation (HUD, menus, panels) with rounded 9-sliced sprites.</summary>
    public static partial class CoinRushBuilder
    {
        // ---- Palette "Dusk & Gold": deep ink surfaces, warm amber primary, coral / teal / sky accents ----
        private static readonly Color Ink = new Color(0.063f, 0.075f, 0.122f);            // #10131F
        private static readonly Color CardColor = new Color(0.094f, 0.114f, 0.18f, 0.95f); // #181D2E
        private static readonly Color Raised = new Color(0.137f, 0.165f, 0.25f, 1f);      // #232A40
        private static readonly Color CardBorder = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color Primary = new Color(1f, 0.71f, 0.204f);             // #FFB534 amber
        private static readonly Color Coral = new Color(1f, 0.435f, 0.38f);               // #FF6F61
        private static readonly Color Teal = new Color(0.263f, 0.851f, 0.741f);           // #43D9BD
        private static readonly Color Sky = new Color(0.431f, 0.784f, 1f);                // #6EC8FF
        private static readonly Color Secondary = new Color(0.165f, 0.2f, 0.314f, 1f);    // #2A3350
        private static readonly Color Gold = new Color(1f, 0.835f, 0.29f);                // coins & score
        private static readonly Color TextColor = new Color(0.965f, 0.949f, 0.918f);      // #F6F2EA
        private static readonly Color ButtonText = Ink;
        private static readonly Color Muted = new Color(0.604f, 0.639f, 0.722f, 1f);      // #9AA3B8

        // ---- Typography: Fredoka (rounded, playful) for display + headings, Inter (clean, professional) for UI + body ----
        private const string FontsDir = ArtDir + "/Fonts";
        private const string Charset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·–—’“”…←→↑↓×•©";

        private static TMP_FontAsset fontDisplay, fontHeading, fontUI, fontBody;
        private static Material fontOutline, fontTitle; // "role markers" passed to Text(); resolved per font
        private static readonly System.Collections.Generic.Dictionary<string, Material> presetCache =
            new System.Collections.Generic.Dictionary<string, Material>();

        // =====================================================================
        // Fonts
        // =====================================================================

        private static void EnsureFontMaterials()
        {
            presetCache.Clear();
            TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            fontUI = FontAssetFor("Inter-Bold", "FA_Inter_Bold");
            fontBody = FontAssetFor("Inter-Medium", "FA_Inter_Medium");
            fontHeading = FontAssetFor("Fredoka-SemiBold", "FA_Fredoka_SemiBold");
            fontDisplay = FontAssetFor("Fredoka-Bold", "FA_Fredoka_Bold");

            // Fallback chain so any missing glyph still renders: Fredoka -> Inter -> LiberationSans.
            SetFallbacks(fontDisplay, fontUI, liberation);
            SetFallbacks(fontHeading, fontUI, liberation);
            SetFallbacks(fontUI, liberation);
            SetFallbacks(fontBody, fontUI, liberation);

            // Text created at runtime (loading tips) uses the TMP default font.
            SerializedObject settings = new SerializedObject(TMP_Settings.instance);
            SerializedProperty def = settings.FindProperty("m_defaultFontAsset");
            if (def != null) { def.objectReferenceValue = fontUI; settings.ApplyModifiedPropertiesWithoutUndo(); }

            fontOutline = Preset(fontUI, "Outline");
            fontTitle = Preset(fontDisplay, "Title");
        }

        private static TMP_FontAsset FontAssetFor(string ttfName, string assetName)
        {
            string path = $"{FontsDir}/{assetName}.asset";
            TMP_FontAsset fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fa != null) return fa;

            Font source = AssetDatabase.LoadAssetAtPath<Font>($"{FontsDir}/{ttfName}.ttf");
            if (source == null)
            {
                Debug.LogError($"[Coin Rush] Font file missing: {FontsDir}/{ttfName}.ttf");
                return TMP_Settings.defaultFontAsset;
            }
            fa = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, false);
            fa.name = assetName;
            AssetDatabase.CreateAsset(fa, path);
            fa.atlasTextures[0].name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.TryAddCharacters(Charset, out _);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        private static void SetFallbacks(TMP_FontAsset fa, params TMP_FontAsset[] fallbacks)
        {
            if (fa == null) return;
            fa.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            foreach (TMP_FontAsset f in fallbacks) if (f != null && f != fa) fa.fallbackFontAssetTable.Add(f);
            EditorUtility.SetDirty(fa);
        }

        /// <summary>Material preset per font: "Outline" (thin ink outline + soft shadow) or "Title" (bold outline + drop shadow).</summary>
        private static Material Preset(TMP_FontAsset fa, string kind)
        {
            string key = fa.name + kind;
            if (presetCache.TryGetValue(key, out Material cached)) return cached;
            bool title = kind == "Title";
            string path = $"{MaterialsDir}/UI/TMP_{fa.name}_{kind}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(fa.material) { name = $"TMP_{fa.name}_{kind}" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = fa.material.shader;
            mat.SetTexture(ShaderUtilities.ID_MainTex, fa.atlasTexture);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, title ? 0.22f : 0.12f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, title ? new Color(0.24f, 0.11f, 0.02f, 1f) : new Color(Ink.r, Ink.g, Ink.b, 0.9f));
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, title ? 0.55f : 0.45f));
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, title ? 0.5f : 0.3f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, title ? -0.9f : -0.5f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, title ? 0.25f : 0.4f);
            EditorUtility.SetDirty(mat);
            presetCache[key] = mat;
            return mat;
        }

        /// <summary>Switches a text to the long-form body font (story, descriptions, tips).</summary>
        private static TMP_Text Body(TMP_Text t, bool outline = true)
        {
            t.font = fontBody;
            t.fontSharedMaterial = outline ? Preset(fontBody, "Outline") : fontBody.material;
            t.lineSpacing = 8f;
            return t;
        }

        // =====================================================================
        // GAME UI
        // =====================================================================

        private static void BuildGameUI(WorldConfig world)
        {
            EnsureFontMaterials();
            Transform uiRoot = new GameObject("[UI]").transform;
            Canvas canvas = CreateCanvas(uiRoot, "GameCanvas");
            Transform c = canvas.transform;

            // Bottom-most layers first (draw order = hierarchy order).
            Image vignette = Stretch(Rect("DamageVignette", c)).gameObject.AddComponent<Image>();
            vignette.sprite = SpriteAsset("ui_vignette");
            vignette.color = new Color(0.9f, 0.05f, 0.1f, 0f);
            vignette.raycastTarget = false;
            vignette.gameObject.AddComponent<DamageVignette>();

            RectTransform floating = Stretch(Rect("FloatingText", c));
            TMP_Text floatTemplate = Text(floating, "PopupTemplate", "+1", 44, TextAlignmentOptions.Center, Gold,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 60), fontOutline);
            FloatingTextSpawner spawner = floating.gameObject.AddComponent<FloatingTextSpawner>();
            Set(spawner, "template", floatTemplate);

            // ---------- HUD ----------
            RectTransform hud = Stretch(Rect("HUD", c));

            // World card (top-left)
            RectTransform worldCard = Card(hud, "WorldCard", new Vector2(0, 1), new Vector2(30, -30), new Vector2(560, 150));
            TMP_Text worldName = Text(worldCard, "WorldName", world.worldName.ToUpper(), 30, TextAlignmentOptions.Left, TextColor,
                new Vector2(0, 1), new Vector2(24, -18), new Vector2(520, 40), fontOutline);
            Icon(worldCard, "CoinIcon", "icon_coin", new Vector2(0, 1), new Vector2(24, -66), 40);
            Image quotaFill = Bar(worldCard, "QuotaBar", new Vector2(0, 1), new Vector2(74, -74), new Vector2(460, 24), world.themeColor);
            TMP_Text quotaText = Text(worldCard, "QuotaText", $"COINS 0 / {world.coinQuota}", 20, TextAlignmentOptions.Center, TextColor,
                new Vector2(0, 1), new Vector2(74, -72), new Vector2(460, 28), fontOutline);
            TMP_Text levelText = Text(worldCard, "ThreatLevel", "THREAT LV 1", 20, TextAlignmentOptions.Left, new Color(1f, 0.55f, 0.5f),
                new Vector2(0, 1), new Vector2(24, -112), new Vector2(300, 28), fontOutline);

            // Timer pill (top-centre) + combo badge
            RectTransform timerPill = Rect("TimerPill", hud);
            Place(timerPill, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(250, 84));
            Image pillImg = SlicedImage(timerPill, "ui_pill", CardColor);
            Icon(timerPill, "ClockIcon", "icon_clock", new Vector2(0, 0.5f), new Vector2(26, 0), 44);
            TMP_Text timer = Text(timerPill, "TimerText", $"{(int)world.timeLimit / 60}:{(int)world.timeLimit % 60:00}", 52, TextAlignmentOptions.Center, TextColor,
                new Vector2(0.5f, 0.5f), new Vector2(22, 0), new Vector2(180, 70), fontOutline);

            RectTransform combo = Rect("ComboBadge", hud);
            Place(combo, new Vector2(0.5f, 1), new Vector2(0, -126), new Vector2(300, 64));
            SlicedImage(combo, "ui_pill", Gold);
            TMP_Text comboText = Text(combo, "ComboText", "x2  COMBO 6", 34, TextAlignmentOptions.Center, ButtonText,
                new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(290, 50), null);
            Image comboTimer = Bar(combo, "ComboTimer", new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(220, 8), new Color(0.3f, 0.15f, 0f), centred: true);

            // Minimap + score (top-right)
            RectTransform minimap = Rect("Minimap", hud);
            Place(minimap, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(270, 270));
            Image mapMask = minimap.gameObject.AddComponent<Image>();
            mapMask.sprite = SpriteAsset("ui_circle");
            minimap.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RawImage mapImage = Stretch(Rect("MapView", minimap)).gameObject.AddComponent<RawImage>();
            mapImage.texture = minimapTexture;
            mapImage.raycastTarget = false;
            RectTransform ring = Rect("MapRing", hud);
            Place(ring, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(282, 282));
            Image ringImg = ring.gameObject.AddComponent<Image>();
            ringImg.sprite = SpriteAsset("ui_ring");
            ringImg.color = new Color(1f, 1f, 1f, 0.9f);
            ringImg.raycastTarget = false;

            RectTransform scoreCard = Card(hud, "ScoreCard", new Vector2(1, 1), new Vector2(-30, -318), new Vector2(270, 74));
            Icon(scoreCard, "CoinIcon", "icon_coin", new Vector2(0, 0.5f), new Vector2(18, 0), 46);
            TMP_Text score = Text(scoreCard, "ScoreText", "0", 46, TextAlignmentOptions.Right, Gold,
                new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(190, 60), fontOutline);

            // Vitals (bottom-left)
            RectTransform vitals = Card(hud, "VitalsCard", new Vector2(0, 0), new Vector2(30, 30), new Vector2(640, 136));
            // Hero portrait (circle mask) + name – shows which hero was picked on the home page.
            RectTransform portraitFrame = Rect("HeroPortrait", vitals);
            Place(portraitFrame, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(108, 108));
            Image frameImg = portraitFrame.gameObject.AddComponent<Image>();
            frameImg.sprite = SpriteAsset("ui_circle");
            frameImg.color = Raised;
            frameImg.raycastTarget = false;
            portraitFrame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RectTransform portraitImgRt = Stretch(Rect("Portrait", portraitFrame));
            portraitImgRt.offsetMin = new Vector2(-6, -18); portraitImgRt.offsetMax = new Vector2(6, 6);
            Image heroPortrait = portraitImgRt.gameObject.AddComponent<Image>();
            heroPortrait.preserveAspect = true;
            heroPortrait.raycastTarget = false;
            Image portraitRing = Icon(vitals, "PortraitRing", "ui_ring", new Vector2(0, 0.5f), new Vector2(10, 0), 116);
            portraitRing.color = Primary;
            TMP_Text heroName = Text(vitals, "HeroName", "HERO", 20, TextAlignmentOptions.Left, Primary,
                new Vector2(0, 1), new Vector2(136, -10), new Vector2(300, 26), fontOutline);
            heroName.characterSpacing = 4f;
            Icon(vitals, "HeartIcon", "icon_heart", new Vector2(0, 1), new Vector2(134, -40), 40).color = new Color(1f, 0.35f, 0.45f);
            Image healthFill = Bar(vitals, "HealthBar", new Vector2(0, 1), new Vector2(184, -46), new Vector2(360, 28), new Color(0.35f, 0.95f, 0.45f));
            TMP_Text healthText = Text(vitals, "HealthText", "100", 30, TextAlignmentOptions.Right, TextColor,
                new Vector2(1, 1), new Vector2(-18, -42), new Vector2(80, 40), fontOutline);
            Icon(vitals, "BoltIcon", "icon_bolt", new Vector2(0, 1), new Vector2(140, -90), 30).color = Gold;
            Image staminaFill = Bar(vitals, "StaminaBar", new Vector2(0, 1), new Vector2(184, -98), new Vector2(360, 16), Gold);

            // Abilities (bottom-centre): dash + power-up slots
            RectTransform abilities = Rect("Abilities", hud);
            Place(abilities, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(520, 130));
            Image dashFill = RadialSlot(abilities, "DashSlot", "icon_dash", new Vector2(-170, 0), 110, new Color(0.45f, 0.85f, 1f), out GameObject _);
            Image speedFill = RadialSlot(abilities, "SpeedSlot", "icon_bolt", new Vector2(-40, 10), 84, new Color(0.4f, 0.75f, 1f), out GameObject speedRoot);
            Image magnetFill = RadialSlot(abilities, "MagnetSlot", "icon_magnet", new Vector2(60, 10), 84, new Color(1f, 0.35f, 0.4f), out GameObject magnetRoot);
            Image shieldFill = RadialSlot(abilities, "ShieldSlot", "icon_shield", new Vector2(160, 10), 84, new Color(0.4f, 1f, 0.85f), out GameObject shieldRoot);

            // Controls are listed on the dedicated CONTROLS page (menu + pause), not on the gameplay HUD.

            HUDController hudController = hud.gameObject.AddComponent<HUDController>();
            Set(hudController, "worldNameText", worldName);
            Set(hudController, "quotaText", quotaText);
            Set(hudController, "quotaFill", quotaFill);
            Set(hudController, "levelText", levelText);
            Set(hudController, "scoreText", score);
            Set(hudController, "timerText", timer);
            Set(hudController, "timerPill", pillImg);
            Set(hudController, "comboRoot", combo.gameObject);
            Set(hudController, "comboText", comboText);
            Set(hudController, "comboTimerFill", comboTimer);
            Set(hudController, "healthFill", healthFill);
            Set(hudController, "healthText", healthText);
            Set(hudController, "staminaFill", staminaFill);
            Set(hudController, "dashFill", dashFill);
            Set(hudController, "heroDatabase", characterDatabase);
            Set(hudController, "heroPortrait", heroPortrait);
            Set(hudController, "heroNameText", heroName);
            SerializedObject hudSo = new SerializedObject(hudController);
            SerializedProperty slots = hudSo.FindProperty("powerUpSlots");
            slots.arraySize = 3;
            void Slot(int i, PowerUpType type, GameObject root, Image fill)
            {
                SerializedProperty s = slots.GetArrayElementAtIndex(i);
                s.FindPropertyRelative("type").enumValueIndex = (int)type;
                s.FindPropertyRelative("root").objectReferenceValue = root;
                s.FindPropertyRelative("radialFill").objectReferenceValue = fill;
            }
            Slot(0, PowerUpType.SpeedBoost, speedRoot, speedFill);
            Slot(1, PowerUpType.Magnet, magnetRoot, magnetFill);
            Slot(2, PowerUpType.Shield, shieldRoot, shieldFill);
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // ---------- Portal compass ----------
            RectTransform compass = Stretch(Rect("PortalCompass", hud)); // part of the HUD: hidden on menus / results
            CanvasGroup compassGroup = compass.gameObject.AddComponent<CanvasGroup>();
            compassGroup.blocksRaycasts = false;
            RectTransform arrow = Rect("Arrow", compass);
            Place(arrow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
            Image arrowImg = arrow.gameObject.AddComponent<Image>();
            arrowImg.sprite = SpriteAsset("icon_arrow");
            arrowImg.color = new Color(0.8f, 0.55f, 1f);
            arrowImg.raycastTarget = false;
            TMP_Text compassText = Text(compass, "Distance", "PORTAL 40m", 22, TextAlignmentOptions.Center, new Color(0.85f, 0.7f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 30), fontOutline);
            PortalCompass compassComp = compass.gameObject.AddComponent<PortalCompass>();
            Set(compassComp, "arrow", arrow);
            Set(compassComp, "distanceText", compassText);
            Set(compassComp, "group", compassGroup);

            // ---------- Toast ----------
            RectTransform toast = Rect("Toast", c);
            Place(toast, new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1200, 100));
            CanvasGroup toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            TMP_Text toastText = Text(toast, "Message", "", 64, TextAlignmentOptions.Center, TextColor,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 100), fontTitle);
            NotificationToast toastComp = toast.gameObject.AddComponent<NotificationToast>();
            Set(toastComp, "group", toastGroup);
            Set(toastComp, "messageText", toastText);

            // ---------- Countdown ----------
            TMP_Text countdown = Text(c, "CountdownText", "3", 220, TextAlignmentOptions.Center, TextColor,
                new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(700, 300), fontTitle);

            // ---------- World intro banner ----------
            RectTransform banner = Rect("WorldIntro", c);
            Place(banner, new Vector2(0, 0.5f), new Vector2(0, 60), new Vector2(900, 260));
            CanvasGroup bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            RectTransform bannerContent = Rect("Content", banner);
            Place(bannerContent, new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(820, 240));
            SlicedImage(bannerContent, "ui_rounded", CardColor);
            Image accent = Rect("Accent", bannerContent).gameObject.AddComponent<Image>();
            Place((RectTransform)accent.transform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(12, 240));
            accent.color = world.themeColor;
            TMP_Text bNum = Text(bannerContent, "WorldNumber", "WORLD 1 OF 3", 26, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.7f),
                new Vector2(0, 1), new Vector2(40, -20), new Vector2(760, 34), fontOutline);
            TMP_Text bName = Text(bannerContent, "WorldName", world.worldName.ToUpper(), 76, TextAlignmentOptions.Left, world.themeColor,
                new Vector2(0, 1), new Vector2(36, -52), new Vector2(780, 90), fontTitle);
            TMP_Text bGoal = Text(bannerContent, "Goal", $"Collect {world.coinQuota} coins to open the portal", 26, TextAlignmentOptions.Left, TextColor,
                new Vector2(0, 1), new Vector2(40, -150), new Vector2(760, 34), fontOutline);
            Body(bGoal);
            WorldIntroBanner bannerComp = banner.gameObject.AddComponent<WorldIntroBanner>();
            Set(bannerComp, "group", bannerGroup);
            Set(bannerComp, "content", bannerContent);
            Set(bannerComp, "worldNumberText", bNum);
            Set(bannerComp, "worldNameText", bName);
            Set(bannerComp, "goalText", bGoal);
            Set(bannerComp, "accentBar", accent);

            // ---------- Pause ----------
            RectTransform pause = Overlay(c, "PausePanel");
            RectTransform pauseCard = Card(pause, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 720), centred: true);
            Text(pauseCard, "Title", "PAUSED", 80, TextAlignmentOptions.Center, Primary, new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(520, 100), fontTitle);
            Button resume = Btn(pauseCard, "ResumeButton", "RESUME", new Vector2(0, 160));
            Button restartWorld = Btn(pauseCard, "RestartButton", "RESTART WORLD", new Vector2(0, 60), secondary: true);
            Button pauseControls = Btn(pauseCard, "ControlsButton", "CONTROLS", new Vector2(0, -40), secondary: true);
            Button pauseSettings = Btn(pauseCard, "SettingsButton", "SETTINGS", new Vector2(0, -140), secondary: true);
            Button pauseMenu = Btn(pauseCard, "MainMenuButton", "MAIN MENU", new Vector2(0, -240), secondary: true);

            SettingsPanel settings = BuildSettingsPanel(c, pause.gameObject);
            MenuPanel controls = BuildControlsPanel(c, pause.gameObject);

            // ---------- World complete ----------
            RectTransform complete = Overlay(c, "WorldCompletePanel");
            RectTransform completeCard = Card(complete, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 520), centred: true);
            Icon(completeCard, "PortalIcon", "icon_portal", new Vector2(0.5f, 1), new Vector2(0, -30), 90);
            TMP_Text completeTitle = Text(completeCard, "Title", "MEADOW CLEARED!", 62, TextAlignmentOptions.Center, Gold,
                new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(720, 80), fontTitle);
            TMP_Text completeStats = Text(completeCard, "Stats", "World points 30", 36, TextAlignmentOptions.Center, TextColor,
                new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(680, 260), fontOutline);
            completeStats.alignment = TextAlignmentOptions.Top;

            // ---------- End: results screen (win / lose) ----------
            RectTransform end = BuildResultsScreen(c, out ResultsScreen results, out Button retryWorld, out Button newRun, out Button endMenu);

            GameUIController ui = canvas.gameObject.AddComponent<GameUIController>();
            Set(ui, "hudPanel", hud.gameObject);
            Set(ui, "pausePanel", pause.gameObject);
            Set(ui, "settingsPanel", settings);
            Set(ui, "controlsPanel", controls);
            Set(ui, "worldCompletePanel", complete.gameObject);
            Set(ui, "endPanel", end.gameObject);
            Set(ui, "countdownText", countdown);
            Set(ui, "completeTitleText", completeTitle);
            Set(ui, "completeStatsText", completeStats);
            Set(ui, "results", results);
            Set(ui, "resumeButton", resume);
            Set(ui, "pauseRestartButton", restartWorld);
            Set(ui, "pauseSettingsButton", pauseSettings);
            Set(ui, "pauseControlsButton", pauseControls);
            Set(ui, "pauseMenuButton", pauseMenu);
            Set(ui, "endRetryWorldButton", retryWorld);
            Set(ui, "endNewRunButton", newRun);
            Set(ui, "endMenuButton", endMenu);

            pause.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            controls.gameObject.SetActive(false);
            complete.gameObject.SetActive(false);
            end.gameObject.SetActive(false);
            countdown.gameObject.SetActive(false);
            combo.gameObject.SetActive(false);

            CreateEventSystem(uiRoot);
        }

        private static SettingsPanel BuildSettingsPanel(Transform canvas, GameObject returnTo)
        {
            RectTransform panel = Overlay(canvas, "SettingsPanel");
            RectTransform card = Card(panel, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 560), centred: true);
            Text(card, "Title", "SETTINGS", 70, TextAlignmentOptions.Center, Primary, new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(660, 90), fontTitle);

            Slider music = SliderRow(card, "Music", "MUSIC", new Vector2(0, 120), out TMP_Text musicValue);
            Slider sfx = SliderRow(card, "Sfx", "SOUND FX", new Vector2(0, 30), out TMP_Text sfxValue);
            Slider sens = SliderRow(card, "Sensitivity", "MOUSE SENSITIVITY", new Vector2(0, -60), out TMP_Text sensValue);
            Button back = Btn(card, "BackButton", "BACK", new Vector2(0, -190));

            SettingsPanel sp = panel.gameObject.AddComponent<SettingsPanel>();
            Set(sp, "musicSlider", music);
            Set(sp, "sfxSlider", sfx);
            Set(sp, "sensitivitySlider", sens);
            Set(sp, "musicValue", musicValue);
            Set(sp, "sfxValue", sfxValue);
            Set(sp, "sensitivityValue", sensValue);
            Set(sp, "backButton", back);
            Set(sp, "returnTo", returnTo);
            return sp;
        }

        // =====================================================================
        // MAIN MENU UI  (home page + character select screen + controls page)
        // =====================================================================

        private static void BuildMenuUI(Transform turntable, RenderTexture heroTexture)
        {
            EnsureFontMaterials();
            Transform uiRoot = new GameObject("[UI]").transform;
            Canvas canvas = CreateCanvas(uiRoot, "MenuCanvas");
            Transform c = canvas.transform;

            // Preview stage logic lives on the 3D stage itself; the UI only references it.
            HeroPreviewStage stage = turntable.parent.gameObject.AddComponent<HeroPreviewStage>();
            Set(stage, "database", characterDatabase);
            Set(stage, "turntable", turntable);
            SetInt(stage, "stageLayer", HeroStageLayer);

            // Soft navy→violet gradient so text reads over the 3D diorama.
            Image shade = Rect("LeftShade", c).gameObject.AddComponent<Image>();
            RectTransform shadeRt = (RectTransform)shade.transform;
            shadeRt.anchorMin = Vector2.zero; shadeRt.anchorMax = new Vector2(0.55f, 1f);
            shadeRt.offsetMin = shadeRt.offsetMax = Vector2.zero;
            shade.sprite = SpriteAsset("ui_gradient_h");
            shade.color = new Color(Ink.r, Ink.g, Ink.b, 0.92f);
            shade.raycastTarget = false;

            // ---------------- Home: main panel (left) ----------------
            RectTransform main = Rect("MainPanel", c);
            Place(main, new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(760, 980));
            main.gameObject.AddComponent<CanvasGroup>();
            RectTransform content = Stretch(Rect("Content", main));

            Image titleGlow = Icon(content, "TitleGlow", "ui_glow", new Vector2(0, 1), new Vector2(-120, 60), 520);
            titleGlow.color = new Color(1f, 0.6f, 0.15f, 0.22f);
            ((RectTransform)titleGlow.transform).sizeDelta = new Vector2(1100, 420);
            TMP_Text title = Text(content, "Title", "COIN RUSH", 160, TextAlignmentOptions.Left, TextColor,
                new Vector2(0, 1), new Vector2(0, -50), new Vector2(950, 180), fontTitle);
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(new Color(1f, 0.87f, 0.45f), new Color(1f, 0.87f, 0.45f), new Color(1f, 0.62f, 0.13f), new Color(1f, 0.58f, 0.12f));
            title.characterSpacing = 4f;

            Vector2 left = new Vector2(0, 0.5f);
            Button play = Btn(content, "PlayButton", "PLAY", new Vector2(0, 168), wide: true, anchor: left);
            Icon(play.transform, "PlayIcon", "icon_arrow", new Vector2(1, 0.5f), new Vector2(-34, 0), 34).transform.localRotation = Quaternion.Euler(0, 0, -90);
            Button characters = Btn(content, "CharactersButton", "CHARACTERS", new Vector2(0, 74), wide: true, anchor: left, secondary: true);
            Button controlsBtn = Btn(content, "ControlsButton", "CONTROLS", new Vector2(0, -20), wide: true, anchor: left, secondary: true);
            Button settingsBtn = Btn(content, "SettingsButton", "SETTINGS", new Vector2(0, -114), wide: true, anchor: left, secondary: true);
            Button creditsBtn = Btn(content, "CreditsButton", "CREDITS", new Vector2(0, -208), wide: true, anchor: left, secondary: true);
            Button quit = Btn(content, "QuitButton", "QUIT", new Vector2(0, -302), wide: true, anchor: left, secondary: true);

            RectTransform recordCard = Card(c, "RecordsCard", new Vector2(1, 0), new Vector2(-60, 50), new Vector2(640, 70));
            Icon(recordCard, "Trophy", "icon_coin", new Vector2(0, 0.5f), new Vector2(18, 0), 40);
            TMP_Text records = Text(recordCard, "Records", "BEST SCORE 0", 26, TextAlignmentOptions.Left, TextColor,
                new Vector2(0, 0.5f), new Vector2(72, 0), new Vector2(560, 40), fontOutline);

            // ---------------- Home: replay-story pill (top right) ----------------
            Button story = Btn(c, "StoryButton", "WATCH STORY", new Vector2(-60, -50), secondary: true, anchor: new Vector2(1, 1));
            ((RectTransform)story.transform).sizeDelta = new Vector2(300, 64);
            story.GetComponentInChildren<TMP_Text>().fontSize = 26;
            ((RectTransform)story.transform).pivot = new Vector2(1, 1);

            // ---------------- Character select screen (opened by CHARACTERS) ----------------
            CharacterSelectScreen select = BuildCharacterSelect(c, heroTexture, stage, main.gameObject);

            // ---------------- Controls & settings ----------------
            MenuPanel controls = BuildControlsPanel(c, main.gameObject);
            MenuPanel credits = BuildCreditsPanel(c, main.gameObject);
            SettingsPanel settings = BuildSettingsPanel(c, main.gameObject);

            MainMenuController menu = canvas.gameObject.AddComponent<MainMenuController>();
            Set(menu, "gameConfig", gameConfig);
            Set(menu, "mainPanel", main.gameObject);
            Set(menu, "controlsPanel", controls);
            Set(menu, "settingsPanel", settings);
            Set(menu, "characterScreen", select);
            Set(menu, "playButton", play);
            Set(menu, "charactersButton", characters);
            Set(menu, "controlsButton", controlsBtn);
            Set(menu, "settingsButton", settingsBtn);
            Set(menu, "creditsButton", creditsBtn);
            Set(menu, "creditsPanel", credits);
            Set(menu, "quitButton", quit);
            Set(menu, "storyButton", story);
            Set(menu, "recordsText", records);
            Set(menu, "titleTransform", title.rectTransform);

            controls.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            select.gameObject.SetActive(false);
            credits.gameObject.SetActive(false);
            CreateEventSystem(uiRoot);
        }

        /// <summary>Rounded window that shows the hero preview RenderTexture.</summary>
        private static RawImage PreviewWindow(Transform parent, string name, RenderTexture rt, Vector2 pos, Vector2 size)
        {
            RectTransform mask = Rect(name + "Mask", parent);
            Place(mask, new Vector2(0.5f, 1), pos, size);
            SlicedImage(mask, "ui_rounded", Raised);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RawImage preview = Stretch(Rect(name, mask)).gameObject.AddComponent<RawImage>();
            preview.texture = rt;
            preview.raycastTarget = false;
            return preview;
        }

        /// <summary>Full-screen hero selection: big 3D preview + stats (left), portrait grid (right).</summary>
        private static CharacterSelectScreen BuildCharacterSelect(Transform c, RenderTexture heroTexture, HeroPreviewStage stage,
            GameObject homePanel)
        {
            RectTransform screen = Overlay(c, "CharacterSelectScreen");
            screen.GetComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 0.95f);

            Text(screen, "Title", "SELECT YOUR HERO", 72, TextAlignmentOptions.Center, Primary,
                new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(1200, 90), fontTitle);
            Text(screen, "Hint", "Click a portrait  ·  A / D or ← / → to browse  ·  ENTER select  ·  ESC back", 24,
                TextAlignmentOptions.Center, Muted, new Vector2(0.5f, 1), new Vector2(0, -116), new Vector2(1400, 32), fontOutline);

            // ----- Left: preview + details -----
            RectTransform detail = Card(screen, "DetailCard", new Vector2(0, 0.5f), new Vector2(100, -50), new Vector2(640, 840));
            Image glow = Icon(detail, "Glow", "ui_glow", new Vector2(0.5f, 1), new Vector2(0, -20), 600);
            glow.color = new Color(1f, 0.71f, 0.2f, 0.18f);
            PreviewWindow(detail, "HeroPreview", heroTexture, new Vector2(0, -30), new Vector2(560, 500));
            TMP_Text heroName = Text(detail, "HeroName", "HERO", 60, TextAlignmentOptions.Center, Primary,
                new Vector2(0.5f, 1), new Vector2(0, -540), new Vector2(600, 70), fontTitle);
            TMP_Text role = Text(detail, "HeroRole", "Explorer", 26, TextAlignmentOptions.Center, TextColor,
                new Vector2(0.5f, 1), new Vector2(0, -608), new Vector2(600, 32), fontOutline);
            TMP_Text desc = Text(detail, "HeroDescription", "", 21, TextAlignmentOptions.Center, Muted,
                new Vector2(0.5f, 1), new Vector2(0, -644), new Vector2(560, 56), null);
            Body(desc);
            desc.textWrappingMode = TextWrappingModes.Normal;
            Image speed = StatRow(detail, "Speed", "SPEED", -716, new Color(0.45f, 0.85f, 1f));
            Image jump = StatRow(detail, "Jump", "JUMP", -758, Primary);
            Image stamina = StatRow(detail, "Stamina", "STAMINA", -800, Gold);

            // ----- Right: portrait grid -----
            RectTransform gridCard = Card(screen, "GridCard", new Vector2(1, 0.5f), new Vector2(-100, -50), new Vector2(960, 840));
            int count = characterDatabase != null ? characterDatabase.Count : 0;
            // Rows of 3, the last row centred (5 heroes -> 3 + 2).
            const int cols = 3;
            const float tile = 250f, gap = 26f;
            Button[] buttons = new Button[count];
            Image[] frames = new Image[count];
            for (int i = 0; i < count; i++)
            {
                CharacterDefinition hero = characterDatabase.Get(i);
                int col = i % cols, row = i / cols;
                int inRow = Mathf.Min(cols, count - row * cols);
                float startX = (960f - inRow * tile - (inRow - 1) * gap) / 2f;
                RectTransform slot = Rect($"Slot_{i + 1:00}_{hero.displayName}", gridCard);
                Place(slot, new Vector2(0, 1), new Vector2(startX + col * (tile + gap), -50 - row * (tile + gap)), new Vector2(tile, tile));

                // Selection frame sits behind the tile and peeks out as a glowing border.
                RectTransform frameRt = Stretch(Rect("SelectionFrame", slot));
                frameRt.offsetMin = new Vector2(-7, -7); frameRt.offsetMax = new Vector2(7, 7);
                frames[i] = SlicedImage(frameRt, "ui_rounded", Primary);
                frames[i].raycastTarget = false;
                frames[i].enabled = false;

                RectTransform tileRt = Stretch(Rect("Tile", slot));
                Image bg = SlicedImage(tileRt, "ui_rounded", Raised);
                Button b = tileRt.gameObject.AddComponent<Button>();
                b.targetGraphic = bg;
                ColorBlock cb = b.colors;
                cb.highlightedColor = new Color(1.35f, 1.35f, 1.5f, 1f);
                cb.selectedColor = cb.highlightedColor;
                cb.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
                cb.fadeDuration = 0.08f;
                b.colors = cb;
                tileRt.gameObject.AddComponent<UIButtonSounds>();
                buttons[i] = b;

                Image accent = Rect("Accent", tileRt).gameObject.AddComponent<Image>();
                Place((RectTransform)accent.transform, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(180, 6));
                accent.sprite = SpriteAsset("ui_pill"); accent.type = Image.Type.Sliced;
                accent.color = hero.accentColor;
                accent.raycastTarget = false;

                RectTransform portraitRt = Stretch(Rect("Portrait", tileRt));
                portraitRt.offsetMin = new Vector2(18, 50); portraitRt.offsetMax = new Vector2(-18, -34);
                Image portrait = portraitRt.gameObject.AddComponent<Image>();
                portrait.sprite = hero.portrait;
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                if (hero.portrait == null) portrait.color = new Color(1f, 1f, 1f, 0f);

                Text(tileRt, "Name", hero.displayName.ToUpper(), 24, TextAlignmentOptions.Center, TextColor,
                    new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(230, 30), fontOutline);
                Text(tileRt, "Role", hero.role.ToUpper(), 16, TextAlignmentOptions.Center, hero.accentColor,
                    new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(230, 22), fontOutline).characterSpacing = 4f;
            }

            Button back = Btn(gridCard, "BackButton", "BACK", new Vector2(-230, 70), secondary: true, anchor: new Vector2(0.5f, 0));
            ((RectTransform)back.transform).pivot = new Vector2(0.5f, 0.5f);
            Button confirm = Btn(gridCard, "SelectButton", "SELECT", new Vector2(230, 70), anchor: new Vector2(0.5f, 0));
            ((RectTransform)confirm.transform).pivot = new Vector2(0.5f, 0.5f);

            CharacterSelectScreen select = screen.gameObject.AddComponent<CharacterSelectScreen>();
            Set(select, "database", characterDatabase);
            Set(select, "stage", stage);
            Set(select, "homePanel", homePanel);
            Set(select, "nameText", heroName);
            Set(select, "roleText", role);
            Set(select, "descriptionText", desc);
            Set(select, "speedBar", speed);
            Set(select, "jumpBar", jump);
            Set(select, "staminaBar", stamina);
            Set(select, "selectButton", confirm);
            Set(select, "backButton", back);
            SetObjectArray(select, "heroButtons", buttons);
            SetObjectArray(select, "selectionFrames", frames);
            return select;
        }

        /// <summary>Dedicated controls page (opened from the home page and the pause menu).</summary>
        private static MenuPanel BuildControlsPanel(Transform canvas, GameObject returnTo)
        {
            RectTransform panel = Overlay(canvas, "ControlsPanel");
            RectTransform card = Card(panel, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 920), centred: true);
            Text(card, "Title", "CONTROLS", 72, TextAlignmentOptions.Center, Primary, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1200, 90), fontTitle);

            string[,] keyboard =
            {
                { "W A S D / ARROWS", "Move" },
                { "LEFT SHIFT", "Sprint (uses stamina)" },
                { "SPACE", "Jump" },
                { "F / RIGHT MOUSE", "Dash – invulnerable" },
                { "MOUSE / Q · E", "Rotate camera" },
                { "MOUSE WHEEL", "Zoom" },
                { "ESC / P", "Pause" },
            };
            string[,] gamepad =
            {
                { "LEFT STICK", "Move" },
                { "LEFT TRIGGER / L3", "Sprint" },
                { "A  (CROSS)", "Jump" },
                { "B  (CIRCLE)", "Dash" },
                { "RIGHT STICK", "Rotate camera" },
                { "D-PAD UP / DOWN", "Zoom" },
                { "START", "Pause" },
            };
            ControlsColumn(card, "Keyboard", "KEYBOARD & MOUSE", -370f, keyboard);
            ControlsColumn(card, "Gamepad", "GAMEPAD", 370f, gamepad);

            Image divider = Rect("Divider", card).gameObject.AddComponent<Image>();
            Place((RectTransform)divider.transform, new Vector2(0.5f, 1), new Vector2(0, -660), new Vector2(1360, 3));
            divider.color = new Color(1f, 1f, 1f, 0.1f);

            TMP_Text tips = Text(card, "Tips",
                "<color=#FFB534>GOAL</color>  Fill the coin quota to relight the <color=#43D9BD>portal</color>, then jump in. Clear all three worlds to bring the gold home.\n" +
                "<color=#FFD54A>Coin</color> 1 pt  ·  <color=#6EC8FF>Gem</color> 5 pts  ·  quick pickups build a COMBO (up to x4)  ·  " +
                "<color=#FF6F61>Heart</color> heals  ·  <color=#6EC8FF>Speed</color>  ·  <color=#FF6F61>Magnet</color>  ·  <color=#43D9BD>Shield</color> blocks one hit",
                24, TextAlignmentOptions.Center, TextColor, new Vector2(0.5f, 1), new Vector2(0, -682), new Vector2(1380, 90), fontOutline);
            Body(tips);
            tips.textWrappingMode = TextWrappingModes.Normal;

            Button back = Btn(card, "BackButton", "BACK", new Vector2(0, 66), anchor: new Vector2(0.5f, 0));
            ((RectTransform)back.transform).pivot = new Vector2(0.5f, 0.5f);

            MenuPanel mp = panel.gameObject.AddComponent<MenuPanel>();
            Set(mp, "backButton", back);
            Set(mp, "returnTo", returnTo);
            return mp;
        }

        /// <summary>Rolling credits: engine, models, music, sound, fonts. All third-party assets are CC0 or OFL.</summary>
        private static MenuPanel BuildCreditsPanel(Transform canvas, GameObject returnTo)
        {
            RectTransform panel = Overlay(canvas, "CreditsPanel");
            RectTransform card = Card(panel, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 940), centred: true);
            Text(card, "Title", "CREDITS", 72, TextAlignmentOptions.Center, Primary, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1000, 90), fontTitle);

            RectTransform viewport = Rect("Viewport", card);
            Place(viewport, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1080, 640));
            viewport.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 60);
            RectTransform content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.spacing = 6f;
            vl.childControlWidth = true; vl.childControlHeight = true;
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            void Line(string text, float size, Color color, bool heading, float spaceAfter = 0f)
            {
                TMP_Text t = Text(content, heading ? "Heading" : "Line", text, size, TextAlignmentOptions.Center, color,
                    new Vector2(0.5f, 1), Vector2.zero, new Vector2(1000, size * 1.4f), heading ? fontOutline : null);
                if (!heading) Body(t, outline: false);
                else t.characterSpacing = 8f;
                t.textWrappingMode = TextWrappingModes.Normal;
                LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = size * 1.45f + spaceAfter;
            }

            Line("COIN RUSH", 64, Primary, heading: true);
            Line("The Stolen Coins of Aurum", 28, Muted, heading: false, spaceAfter: 50f);

            Line("MADE WITH", 26, Teal, heading: true);
            Line("Unity 6  (6000.3 LTS)", 30, TextColor, false);
            Line("Universal Render Pipeline  ·  Shader Graph  ·  Input System  ·  TextMeshPro", 24, Muted, false);
            Line("Gameplay, UI and editor tools written in C#", 24, Muted, false, 40f);

            Line("3D MODELS", 26, Teal, heading: true);
            Line("Kenney  ·  kenney.nl", 30, TextColor, false);
            Line("Mini Characters  ·  Mini Dungeon  ·  Cube Pets  ·  Nature Kit  ·  Platformer Kit", 24, Muted, false);
            Line("Licensed CC0 1.0 (public domain)", 22, Muted, false, 40f);

            Line("MUSIC", 26, Teal, heading: true);
            Line("Juhani Junkala", 30, TextColor, false);
            Line("Chiptune soundtrack", 24, Muted, false);
            Line("Licensed CC0 1.0 (public domain)", 22, Muted, false, 40f);

            Line("SOUND EFFECTS", 26, Teal, heading: true);
            Line("Juhani Junkala", 30, TextColor, false);
            Line("The Essential Retro Video Game Sound Effects Collection (512 sounds)", 24, Muted, false);
            Line("Kenney", 30, TextColor, false);
            Line("Interface Sounds", 24, Muted, false);
            Line("Licensed CC0 1.0 (public domain)", 22, Muted, false, 40f);

            Line("FONTS", 26, Teal, heading: true);
            Line("Fredoka  ·  Inter", 30, TextColor, false);
            Line("Google Fonts  ·  SIL Open Font License 1.1", 22, Muted, false, 40f);

            Line("ASSET SOURCES", 26, Teal, heading: true);
            Line("OpenGameArt.org  ·  kenney.nl  ·  fonts.google.com", 24, Muted, false, 60f);

            Line("THANKS FOR PLAYING!", 40, Gold, heading: true, spaceAfter: 120f);

            CreditsRoll roll = viewport.gameObject.AddComponent<CreditsRoll>();
            Set(roll, "viewport", viewport);
            Set(roll, "content", content);

            Text(card, "Hint", "MOUSE WHEEL / ↑ ↓  scroll", 18, TextAlignmentOptions.Center, Muted,
                new Vector2(0.5f, 0), new Vector2(0, 128), new Vector2(600, 24), null).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Button back = Btn(card, "BackButton", "BACK", new Vector2(0, 66), anchor: new Vector2(0.5f, 0));
            ((RectTransform)back.transform).pivot = new Vector2(0.5f, 0.5f);

            MenuPanel mp = panel.gameObject.AddComponent<MenuPanel>();
            Set(mp, "backButton", back);
            Set(mp, "returnTo", returnTo);
            return mp;
        }

        private static void ControlsColumn(Transform card, string name, string header, float x, string[,] rows)
        {
            RectTransform col = Rect(name + "Column", card);
            Place(col, new Vector2(0.5f, 1), new Vector2(x, -140), new Vector2(660, 500));
            Text(col, "Header", header, 30, TextAlignmentOptions.Center, Teal + new Color(0.2f, 0.2f, 0.1f, 0f),
                new Vector2(0.5f, 1), Vector2.zero, new Vector2(660, 40), fontOutline).characterSpacing = 6f;
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                float y = -62 - i * 66;
                RectTransform chip = Rect("Key_" + rows[i, 1].Split(' ')[0], col);
                Place(chip, new Vector2(0, 1), new Vector2(20, y), new Vector2(290, 52));
                SlicedImage(chip, "ui_pill", Secondary).raycastTarget = false;
                Text(chip, "Key", rows[i, 0], 22, TextAlignmentOptions.Center, TextColor,
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 40), null).rectTransform.pivot = new Vector2(0.5f, 0.5f);
                Text(col, "Action_" + i, rows[i, 1], 26, TextAlignmentOptions.Left, TextColor,
                    new Vector2(0, 1), new Vector2(340, y - 8), new Vector2(320, 36), fontOutline);
            }
        }

        /// <summary>Full-screen victory / defeat results with stars, hero portrait, stat tiles and confetti.</summary>
        private static RectTransform BuildResultsScreen(Transform c, out ResultsScreen results, out Button retryWorld, out Button newRun, out Button menu)
        {
            RectTransform end = Overlay(c, "EndPanel");
            Image backdrop = end.GetComponent<Image>();
            RectTransform content = Stretch(Rect("Content", end)); // first child: popped in by ShowPanel

            Image rays = Icon(content, "Rays", "ui_rays", new Vector2(0.5f, 1), new Vector2(0, 420), 1400);
            ((RectTransform)rays.transform).pivot = new Vector2(0.5f, 0.5f);
            ((RectTransform)rays.transform).anchoredPosition = new Vector2(0, -170);
            Image glow = Icon(content, "Glow", "ui_glow", new Vector2(0.5f, 1), new Vector2(0, 60), 900);
            glow.color = new Color(1f, 1f, 1f, 0.12f);

            Image icon = Icon(content, "BannerIcon", "icon_trophy", new Vector2(0.5f, 1), new Vector2(0, -40), 150);
            ((RectTransform)icon.transform).pivot = new Vector2(0.5f, 0.5f);
            ((RectTransform)icon.transform).anchoredPosition = new Vector2(0, -110);
            TMP_Text title = Text(content, "Title", "VICTORY!", 116, TextAlignmentOptions.Center, Gold,
                new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(1600, 130), fontTitle);
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            title.rectTransform.anchoredPosition = new Vector2(0, -255);
            title.characterSpacing = 6f;

            Image[] stars = new Image[3];
            float[] sx = { -150f, 0f, 150f };
            float[] sy = { -420f, -400f, -420f };
            float[] ss = { 96f, 124f, 96f };
            for (int i = 0; i < 3; i++)
            {
                stars[i] = Icon(content, $"Star{i + 1}", "icon_star", new Vector2(0.5f, 1), Vector2.zero, ss[i]);
                RectTransform srt = (RectTransform)stars[i].transform;
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(sx[i], sy[i]);
            }

            // ----- Card: hero + stat tiles -----
            RectTransform card = Card(content, "ResultsCard", new Vector2(0.5f, 1), new Vector2(0, -500), new Vector2(1340, 340));
            card.pivot = new Vector2(0.5f, 1);
            card.anchoredPosition = new Vector2(0, -500);

            RectTransform frame = Rect("HeroPortrait", card);
            Place(frame, new Vector2(0, 1), new Vector2(60, -24), new Vector2(200, 200));
            Image frameImg = frame.gameObject.AddComponent<Image>();
            frameImg.sprite = SpriteAsset("ui_circle");
            frameImg.color = Raised;
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RectTransform pr = Stretch(Rect("Portrait", frame));
            pr.offsetMin = new Vector2(-10, -40); pr.offsetMax = new Vector2(10, 10);
            Image portrait = pr.gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            Image ring = Icon(card, "PortraitRing", "ui_ring", new Vector2(0, 1), new Vector2(52, -16), 216);
            TMP_Text heroName = Text(card, "HeroName", "HERO", 34, TextAlignmentOptions.Center, Primary,
                new Vector2(0, 1), new Vector2(20, -252), new Vector2(280, 40), fontTitle);

            TMP_Text StatTile(string name, string label, string iconName, Color color, int col, int row)
            {
                RectTransform t = Rect(name, card);
                Place(t, new Vector2(0, 1), new Vector2(350 + col * 325, -30 - row * 145), new Vector2(305, 130));
                SlicedImage(t, "ui_rounded", Raised).raycastTarget = false;
                Image ic = Icon(t, "Icon", iconName, new Vector2(0, 0.5f), new Vector2(20, 0), 54);
                ic.color = color;
                Text(t, "Label", label, 18, TextAlignmentOptions.Left, Muted, new Vector2(0, 1), new Vector2(92, -20), new Vector2(200, 24), null).characterSpacing = 4f;
                return Text(t, "Value", "0", 46, TextAlignmentOptions.Left, TextColor, new Vector2(0, 1), new Vector2(90, -50), new Vector2(210, 60), fontOutline);
            }
            TMP_Text score = StatTile("ScoreTile", "SCORE", "icon_star", Gold, 0, 0);
            TMP_Text coins = StatTile("CoinsTile", "COINS", "icon_coin", Gold, 1, 0);
            TMP_Text combo = StatTile("ComboTile", "BEST COMBO", "icon_bolt", Teal + new Color(0.2f, 0.2f, 0.1f, 0f), 2, 0);
            TMP_Text time = StatTile("TimeTile", "RUN TIME", "icon_clock", new Color(0.45f, 0.85f, 1f), 0, 1);
            TMP_Text world = StatTile("WorldTile", "WORLDS", "icon_portal", Primary, 1, 1);
            TMP_Text best = StatTile("BestTile", "HIGH SCORE", "icon_trophy", Gold, 2, 1);
            world.enableAutoSizing = true; // world names vary in length
            world.fontSizeMin = 18f;
            world.fontSizeMax = 34f;

            RectTransform badge = Rect("NewBestBadge", card);
            Place(badge, new Vector2(1, 1), new Vector2(10, 26), new Vector2(230, 60));
            badge.pivot = new Vector2(0.5f, 0.5f);
            badge.anchoredPosition = new Vector2(-90, 10);
            badge.localRotation = Quaternion.Euler(0, 0, -8f);
            SlicedImage(badge, "ui_pill", Primary).raycastTarget = false;
            Text(badge, "Label", "NEW BEST!", 30, TextAlignmentOptions.Center, ButtonText, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 50), null)
                .rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // ----- Buttons (centred row; RETRY WORLD hides on victory) -----
            RectTransform row2 = Rect("Buttons", content);
            Place(row2, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(1500, 90));
            row2.pivot = new Vector2(0.5f, 0.5f);
            row2.anchoredPosition = new Vector2(0, 110);
            HorizontalLayoutGroup layout = row2.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 40f;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            retryWorld = Btn(row2, "RetryWorldButton", "RETRY WORLD", Vector2.zero, secondary: true);
            newRun = Btn(row2, "NewRunButton", "NEW RUN", Vector2.zero);
            menu = Btn(row2, "MainMenuButton", "MAIN MENU", Vector2.zero, secondary: true);

            RectTransform confettiRt = Stretch(Rect("Confetti", content));
            UIConfetti confetti = confettiRt.gameObject.AddComponent<UIConfetti>();
            Set(confetti, "pieceSprite", SpriteAsset("ui_confetti"));

            results = end.gameObject.AddComponent<ResultsScreen>();
            Set(results, "titleText", title);
            Set(results, "bannerIcon", icon);
            Set(results, "winIcon", SpriteAsset("icon_trophy"));
            Set(results, "loseIcon", SpriteAsset("icon_skull"));
            Set(results, "rays", rays);
            Set(results, "backdrop", backdrop);
            SetObjectArray(results, "stars", stars);
            Set(results, "heroDatabase", characterDatabase);
            Set(results, "heroPortrait", portrait);
            Set(results, "heroRing", ring);
            Set(results, "heroNameText", heroName);
            Set(results, "scoreValue", score);
            Set(results, "coinsValue", coins);
            Set(results, "comboValue", combo);
            Set(results, "timeValue", time);
            Set(results, "worldValue", world);
            Set(results, "bestValue", best);
            Set(results, "newBestBadge", badge.gameObject);
            Set(results, "retryWorldButton", retryWorld);
            Set(results, "confetti", confetti);
            return end;
        }

        private static Image StatRow(Transform parent, string name, string label, float y, Color color)
        {
            Text(parent, name + "Label", label, 22, TextAlignmentOptions.Left, TextColor, new Vector2(0, 1), new Vector2(50, y), new Vector2(160, 28), fontOutline);
            return Bar(parent, name + "Bar", new Vector2(0, 1), new Vector2(200, y - 4), new Vector2(390, 20), color);
        }

        private static void SetObjectArray<T>(Object target, string property, T[] values) where T : Object
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // UI helpers
        // =====================================================================

        private static Canvas CreateCanvas(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>EventSystem driven by the new Input System (InputSystemUIInputModule).</summary>
        private static void CreateEventSystem(Transform parent)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(parent, false);
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static Image SlicedImage(RectTransform rt, string sprite, Color color)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteAsset(sprite);
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        private static RectTransform Card(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, bool centred = false)
        {
            RectTransform rt = Rect(name, parent);
            Place(rt, anchor, pos, size);
            if (centred) rt.pivot = new Vector2(0.5f, 0.5f);
            SlicedImage(rt, "ui_rounded", CardColor).raycastTarget = centred;
            Outline o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = CardBorder;
            o.effectDistance = new Vector2(2, -2);
            return rt;
        }

        /// <summary>Full-screen dim overlay with a CanvasGroup (for fade-in) – first child is the content card.</summary>
        private static RectTransform Overlay(Transform parent, string name)
        {
            RectTransform rt = Stretch(Rect(name, parent));
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(Ink.r, Ink.g, Ink.b, 0.78f);
            rt.gameObject.AddComponent<CanvasGroup>();
            return rt;
        }

        private static TMP_Text Text(Transform parent, string name, string content, float size, TextAlignmentOptions align, Color color,
            Vector2 anchor, Vector2 pos, Vector2 box, Material preset)
        {
            RectTransform rt = Rect(name, parent);
            Place(rt, anchor, pos, box);
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            // Role-based typography: titles = Fredoka Bold, large text = Fredoka SemiBold, small UI text = Inter Bold.
            TMP_FontAsset f = preset == fontTitle ? fontDisplay : (size >= 30f ? fontHeading : fontUI);
            t.font = f;
            if (preset == fontTitle) t.fontSharedMaterial = Preset(f, "Title");
            else if (preset == fontOutline) t.fontSharedMaterial = Preset(f, "Outline");
            t.text = content;
            t.fontSize = size;
            t.fontStyle = FontStyles.Normal; // weights are baked into the font files
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
            return t;
        }

        private static Image Icon(Transform parent, string name, string sprite, Vector2 anchor, Vector2 pos, float size)
        {
            RectTransform rt = Rect(name, parent);
            Place(rt, anchor, pos, new Vector2(size, size));
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteAsset(sprite);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Rounded background + horizontal Filled fill. Returns the fill image.</summary>
        private static Image Bar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor, bool centred = false)
        {
            RectTransform bg = Rect(name, parent);
            Place(bg, anchor, pos, size);
            if (centred) bg.pivot = new Vector2(0.5f, bg.pivot.y);
            Image bgImg = SlicedImage(bg, "ui_pill", new Color(0f, 0f, 0f, 0.5f));
            bgImg.raycastTarget = false;

            RectTransform fill = Stretch(Rect("Fill", bg));
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);
            Image f = fill.gameObject.AddComponent<Image>();
            f.sprite = SpriteAsset("ui_rounded_small");
            f.type = Image.Type.Filled;
            f.fillMethod = Image.FillMethod.Horizontal;
            f.fillOrigin = (int)Image.OriginHorizontal.Left;
            f.fillAmount = 1f;
            f.color = fillColor;
            f.raycastTarget = false;
            return f;
        }

        /// <summary>Circular ability slot: dark disc, radial fill ring, icon. Returns the radial fill.</summary>
        private static Image RadialSlot(Transform parent, string name, string icon, Vector2 pos, float size, Color color, out GameObject root)
        {
            RectTransform rt = Rect(name, parent);
            Place(rt, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            rt.pivot = new Vector2(0.5f, 0.5f);
            root = rt.gameObject;
            Image disc = rt.gameObject.AddComponent<Image>();
            disc.sprite = SpriteAsset("ui_circle");
            disc.color = CardColor;
            disc.raycastTarget = false;

            Image ring = Stretch(Rect("Ring", rt)).gameObject.AddComponent<Image>();
            ring.sprite = SpriteAsset("ui_ring");
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = false;
            ring.fillAmount = 1f;
            ring.color = color;
            ring.raycastTarget = false;

            Image ic = Icon(rt, "Icon", icon, new Vector2(0.5f, 0.5f), Vector2.zero, size * 0.5f);
            ((RectTransform)ic.transform).pivot = new Vector2(0.5f, 0.5f);
            ic.color = color;
            return ring;
        }

        private static Button Btn(Transform parent, string name, string label, Vector2 pos, bool secondary = false, bool wide = false, Vector2? anchor = null)
        {
            RectTransform rt = Rect(name, parent);
            Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
            Place(rt, a, pos, new Vector2(wide ? 460 : 420, 84));
            if (anchor == null) rt.pivot = new Vector2(0.5f, 0.5f);
            else rt.pivot = new Vector2(a.x, 0.5f);

            Image img = SlicedImage(rt, "ui_pill", secondary ? Secondary : Primary);
            Button button = rt.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = TextColor;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.8f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = img;
            rt.gameObject.AddComponent<UIButtonSounds>(); // hover / click feedback on every button

            TMP_Text text = Text(rt, "Label", label, 36, TextAlignmentOptions.Center, secondary ? TextColor : ButtonText,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(wide ? 440 : 400, 70), null);
            ((RectTransform)text.transform).pivot = new Vector2(0.5f, 0.5f);
            return button;
        }

        private static Slider SliderRow(Transform parent, string name, string label, Vector2 pos, out TMP_Text valueText)
        {
            RectTransform row = Rect(name + "Row", parent);
            Place(row, new Vector2(0.5f, 0.5f), pos, new Vector2(600, 70));
            row.pivot = new Vector2(0.5f, 0.5f);
            Text(row, "Label", label, 26, TextAlignmentOptions.Left, TextColor, new Vector2(0, 1), Vector2.zero, new Vector2(400, 30), fontOutline);
            valueText = Text(row, "Value", "100%", 26, TextAlignmentOptions.Right, Primary, new Vector2(1, 1), Vector2.zero, new Vector2(150, 30), fontOutline);

            RectTransform sliderRt = Rect(name + "Slider", row);
            Place(sliderRt, new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(600, 26));
            sliderRt.pivot = new Vector2(0.5f, 0f);
            Slider slider = sliderRt.gameObject.AddComponent<Slider>();

            RectTransform bg = Stretch(Rect("Background", sliderRt));
            SlicedImage(bg, "ui_pill", new Color(0f, 0f, 0f, 0.5f));

            RectTransform fillArea = Stretch(Rect("Fill Area", sliderRt));
            fillArea.offsetMin = new Vector2(6, 4);
            fillArea.offsetMax = new Vector2(-6, -4);
            RectTransform fill = Rect("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0, 1);
            fill.sizeDelta = Vector2.zero;
            SlicedImage(fill, "ui_pill", Primary);

            RectTransform handleArea = Stretch(Rect("Handle Slide Area", sliderRt));
            handleArea.offsetMin = new Vector2(14, 0);
            handleArea.offsetMax = new Vector2(-14, 0);
            RectTransform handle = Rect("Handle", handleArea);
            handle.sizeDelta = new Vector2(36, 0);
            handle.anchorMin = new Vector2(0, 0);
            handle.anchorMax = new Vector2(0, 1);
            handle.offsetMin = new Vector2(handle.offsetMin.x, -6);
            handle.offsetMax = new Vector2(handle.offsetMax.x, 6);
            Image handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = SpriteAsset("ui_circle");
            handleImg.color = TextColor;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }
    }
}
