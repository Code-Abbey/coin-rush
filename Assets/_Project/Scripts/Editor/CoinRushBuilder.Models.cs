using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoinRush.Data;
using CoinRush.Utilities;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CoinRush.EditorTools
{
    /// <summary>
    /// Third-party model pipeline (Kenney CC0 packs in Assets/_Project/ThirdParty/Kenney):
    /// FBX import settings, Animator Controllers, the hero database and model-based prop prefabs.
    /// </summary>
    public static partial class CoinRushBuilder
    {
        private const string KenneyDir = ArtDir + "/Models/Kenney";
        private const string HeroesDir = KenneyDir + "/MiniCharacters/Models";
        private const string NatureDir = KenneyDir + "/NatureKit/Models";
        private const string PlatformerDir = KenneyDir + "/PlatformerKit/Models";
        private const string AnimationDir = ArtDir + "/Animation";
        private const string HeroDataDir = DataDir + "/Heroes";
        private const int HeroStageLayer = 14;

        private static readonly HashSet<string> LoopingClips = new HashSet<string>
            { "idle", "walk", "sprint", "run", "fall", "static", "crouch", "holding-both", "sit" };

        private static AnimatorController heroController, orcController, petController;
        private static CharacterDatabase characterDatabase;

        // Prop prefab pools (picked at random when scattering).
        private static GameObject[] meadowTrees, pineTrees, rocks, desertPlants, ruinColumns, smallDecor, desertDecor, frozenDecor;
        private static GameObject campfirePrefab, tentPrefab, flagPrefab, logStackPrefab, signPrefab;

        // =====================================================================
        // Import settings
        // =====================================================================

        private static void ConfigureModelImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { KenneyDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;

                string file = Path.GetFileNameWithoutExtension(path);
                bool animated = file.StartsWith("character-") || file.StartsWith("animal-");
                bool changed = false;

                if (animated)
                {
                    if (importer.animationType != ModelImporterAnimationType.Generic)
                    {
                        importer.animationType = ModelImporterAnimationType.Generic;
                        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                        changed = true;
                    }
                    if (!importer.importAnimation) { importer.importAnimation = true; changed = true; }

                    // Mark locomotion clips as looping.
                    ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
                    bool clipChanged = importer.clipAnimations.Length == 0;
                    foreach (ModelImporterClipAnimation clip in clips)
                    {
                        bool loop = LoopingClips.Contains(clip.name);
                        if (clip.loopTime != loop) { clip.loopTime = loop; clipChanged = true; }
                    }
                    if (clipChanged) { importer.clipAnimations = clips; changed = true; }
                }
                else
                {
                    if (importer.animationType != ModelImporterAnimationType.None) { importer.animationType = ModelImporterAnimationType.None; changed = true; }
                    if (importer.importAnimation) { importer.importAnimation = false; changed = true; }
                }

                if (importer.importCameras || importer.importLights) { importer.importCameras = importer.importLights = false; changed = true; }
                if (changed) importer.SaveAndReimport();
            }
        }

        private static GameObject ModelAsset(string path)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) Debug.LogError($"[Coin Rush] Model not found: {path}");
            return go;
        }

        private static GameObject Hero(string id) => ModelAsset($"{HeroesDir}/character-{id}.fbx");
        private static GameObject Nature(string id) => ModelAsset($"{NatureDir}/{id}.fbx");
        private static GameObject Platformer(string id) => ModelAsset($"{PlatformerDir}/{id}.fbx");

        /// <summary>Instantiates an FBX model (prefab-linked) under parent and scales it to a height.</summary>
        private static GameObject SpawnModel(GameObject model, Transform parent, Vector3 localPos, float height, float yaw = 0f)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (height > 0f)
            {
                Vector3 keep = go.transform.localPosition;
                ModelUtility.FitToHeight(go, height);
                // FitToHeight centres on the parent; re-apply the requested local offset.
                go.transform.localPosition += keep;
            }
            return go;
        }

        // =====================================================================
        // Animator Controllers (built in code, open them under Assets/_Project/Animation)
        // =====================================================================

        private static List<AnimationClip> Clips(string fbxPath) =>
            AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToList();

        private static AnimationClip Clip(List<AnimationClip> clips, string name)
        {
            AnimationClip c = clips.FirstOrDefault(x => x.name == name);
            if (c == null) Debug.LogWarning($"[Coin Rush] Animation clip '{name}' not found.");
            return c;
        }

        private static void BuildAnimatorControllers()
        {
            Directory.CreateDirectory(AnimationDir);
            heroController = BuildHumanoidController("AC_Hero", Clips($"{HeroesDir}/character-male-a.fbx"), "attack-kick-right");
            orcController = BuildHumanoidController("AC_Orc", Clips($"{KenneyDir}/MiniDungeon/Models/character-orc.fbx"), "attack-melee-right");
            petController = BuildPetController("AC_Pet", Clips($"{KenneyDir}/CubePets/Models/animal-hog.fbx"));
        }

        private static AnimatorController NewController(string name)
        {
            string path = $"{AnimationDir}/{name}.controller";
            // Rebuild IN PLACE (same asset + GUID). Deleting and recreating the asset would leave prefabs that
            // were loaded in this editor session pointing at a destroyed object ("Missing" controller).
            AnimatorController ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ac == null) ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            else
            {
                foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (sub != null && sub != ac) Object.DestroyImmediate(sub, true);
                ac.parameters = new AnimatorControllerParameter[0];
                ac.layers = new AnimatorControllerLayer[0];
                ac.AddLayer("Base Layer");
            }
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Cheer", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ac.parameters = ac.parameters.Select(p => { if (p.name == "Grounded") p.defaultBool = true; return p; }).ToArray();
            return ac;
        }

        /// <summary>Locomotion blend tree (idle/walk/sprint) + Air (jump/fall) + Die / Cheer / Attack one-shots.</summary>
        private static AnimatorController BuildHumanoidController(string name, List<AnimationClip> clips, string attackClip)
        {
            AnimatorController ac = NewController(name);
            AnimatorStateMachine sm = ac.layers[0].stateMachine;

            AnimatorState loco = ac.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip(clips, "idle"), 0f);
            tree.AddChild(Clip(clips, "walk"), 2.2f);
            tree.AddChild(Clip(clips, "sprint"), 7f);
            sm.defaultState = loco;

            AnimatorState jump = sm.AddState("Jump", new Vector3(300, 120, 0));
            jump.motion = Clip(clips, "jump");
            AnimatorState fall = sm.AddState("Fall", new Vector3(520, 120, 0));
            fall.motion = Clip(clips, "fall");

            AnimatorStateTransition toJump = loco.AddTransition(jump);
            toJump.hasExitTime = false; toJump.duration = 0.08f;
            toJump.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            AnimatorStateTransition jumpToFall = jump.AddTransition(fall);
            jumpToFall.hasExitTime = true; jumpToFall.exitTime = 0.85f; jumpToFall.duration = 0.15f;
            foreach (AnimatorState air in new[] { jump, fall })
            {
                AnimatorStateTransition land = air.AddTransition(loco);
                land.hasExitTime = false; land.duration = 0.1f;
                land.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            }

            AnimatorState die = sm.AddState("Die", new Vector3(300, -120, 0));
            die.motion = Clip(clips, "die");
            AnimatorStateTransition toDie = sm.AddAnyStateTransition(die);
            toDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
            toDie.canTransitionToSelf = false; toDie.duration = 0.1f;

            OneShot(sm, loco, "Cheer", Clip(clips, "emote-yes"), new Vector3(520, -60, 0));
            OneShot(sm, loco, "Attack", Clip(clips, attackClip), new Vector3(520, -180, 0));

            EditorUtility.SetDirty(ac);
            return ac;
        }

        private static void OneShot(AnimatorStateMachine sm, AnimatorState back, string trigger, Motion motion, Vector3 pos)
        {
            AnimatorState s = sm.AddState(trigger, pos);
            s.motion = motion;
            AnimatorStateTransition into = sm.AddAnyStateTransition(s);
            into.AddCondition(AnimatorConditionMode.If, 0, trigger);
            into.canTransitionToSelf = false; into.duration = 0.08f;
            AnimatorStateTransition outT = s.AddTransition(back);
            outT.hasExitTime = true; outT.exitTime = 0.9f; outT.duration = 0.15f;
        }

        private static AnimatorController BuildPetController(string name, List<AnimationClip> clips)
        {
            AnimatorController ac = NewController(name);
            AnimatorState loco = ac.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip(clips, "static"), 0f);
            tree.AddChild(Clip(clips, "walk"), 1.8f);
            tree.AddChild(Clip(clips, "run"), 4.5f);
            ac.layers[0].stateMachine.defaultState = loco;
            EditorUtility.SetDirty(ac);
            return ac;
        }

        // =====================================================================
        // Hero database (5 Kenney Mini Characters)
        // =====================================================================

        private static void CreateCharacterDatabase()
        {
            Directory.CreateDirectory(HeroDataDir);
            AssetDatabase.Refresh();

            // (model id, name, role, description, accent, speed, jump, stamina)
            // Five heroes that each play clearly differently (stat totals are equal: 10).
            var heroes = new (string id, string name, string role, string desc, Color c, int s, int j, int st)[]
            {
                ("male-a", "Marcus", "Explorer", "Map in hand, ready for anything. A balanced all-rounder and the perfect first pick.", new Color(0.45f, 0.9f, 0.5f), 4, 3, 3),
                ("female-b", "Sunny", "Sprinter", "The fastest feet on Aurum. Blink and she has the coin, but her stamina runs out quickly.", new Color(1f, 0.8f, 0.25f), 5, 3, 2),
                ("female-a", "Zara", "Acrobat", "Walls? What walls? Her huge jumps reach coins nobody else can.", new Color(0.75f, 0.5f, 1f), 3, 5, 2),
                ("male-b", "Bruno", "Strongman", "Slow off the mark, but he can sprint all day without breaking a sweat.", new Color(1f, 0.5f, 0.3f), 2, 3, 5),
                ("male-d", "Agent Grey", "Spy", "Quick, springy and always cool. Perfect for daring shortcuts.", new Color(0.7f, 0.78f, 0.9f), 4, 4, 2),
            };

            // Remove hero assets and portraits that are no longer part of the roster.
            var keepAssets = new HashSet<string>();
            var keepPortraits = new HashSet<string>();
            foreach (var h in heroes)
            {
                keepAssets.Add($"Hero_{h.name.Replace(" ", "").Replace(".", "")}");
                keepPortraits.Add($"character-{h.id}");
            }
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterDefinition", new[] { HeroDataDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!keepAssets.Contains(Path.GetFileNameWithoutExtension(path))) AssetDatabase.DeleteAsset(path);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { HeroPortraitDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!keepPortraits.Contains(Path.GetFileNameWithoutExtension(path))) AssetDatabase.DeleteAsset(path);
            }

            var defs = new List<CharacterDefinition>();
            foreach (var h in heroes)
            {
                CharacterDefinition def = LoadOrCreate<CharacterDefinition>(HeroDataDir, $"Hero_{h.name.Replace(" ", "").Replace(".", "")}", out bool isNew);
                def.model = Hero(h.id);
                def.portrait = AssetDatabase.LoadAssetAtPath<Sprite>($"{HeroPortraitDir}/character-{h.id}.png");
                def.displayName = h.name; def.role = h.role; def.description = h.desc; def.accentColor = h.c;
                def.speed = h.s; def.jump = h.j; def.stamina = h.st;
                _ = isNew;
                EditorUtility.SetDirty(def);
                defs.Add(def);
            }

            characterDatabase = LoadOrCreate<CharacterDatabase>(DataDir, "CharacterDatabase", out _);
            characterDatabase.characters = defs.ToArray();
            characterDatabase.heroController = heroController;
            EditorUtility.SetDirty(characterDatabase);
        }

        // =====================================================================
        // Model-based prop prefabs
        // =====================================================================

        private enum PropCollider { None, Mesh, Trunk }

        /// <summary>Wraps a Kenney model in a prefab: scaled to height, collider fitted, Obstacle layer, static.</summary>
        private static GameObject ModelProp(string prefabName, GameObject model, float height, PropCollider collider, string folder = "Props")
        {
            GameObject root = new GameObject(prefabName) { isStatic = true };
            if (model == null) return SavePrefab(root, $"{PrefabDir}/{folder}/{prefabName}.prefab");
            GameObject visual = SpawnModel(model, root.transform, Vector3.zero, height);
            visual.name = "Model";

            Bounds b = ModelUtility.GetBounds(visual);
            switch (collider)
            {
                case PropCollider.Mesh:
                    foreach (MeshFilter mf in visual.GetComponentsInChildren<MeshFilter>())
                        mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    break;
                case PropCollider.Trunk:
                    CapsuleCollider cap = root.AddComponent<CapsuleCollider>();
                    cap.radius = Mathf.Max(0.25f, Mathf.Min(b.size.x, b.size.z) * 0.14f);
                    cap.height = b.size.y;
                    cap.center = new Vector3(0f, b.size.y / 2f, 0f);
                    break;
            }

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.isStatic = true;
                t.gameObject.layer = collider == PropCollider.None ? 0 : ObstacleLayer;
            }
            return SavePrefab(root, $"{PrefabDir}/{folder}/{prefabName}.prefab");
        }

        private static void CreateModelProps()
        {
            meadowTrees = new[]
            {
                ModelProp("Tree_Oak", Nature("tree_oak"), 6.5f, PropCollider.Trunk),
                ModelProp("Tree_Default", Nature("tree_default"), 6f, PropCollider.Trunk),
                ModelProp("Tree_Detailed", Nature("tree_detailed"), 7f, PropCollider.Trunk),
                ModelProp("Tree_Fat", Nature("tree_fat"), 5.5f, PropCollider.Trunk),
                ModelProp("Tree_Tall", Nature("tree_tall"), 8f, PropCollider.Trunk),
            };
            pineTrees = new[]
            {
                ModelProp("Pine_Snow", Platformer("treeSnow"), 7f, PropCollider.Trunk),
                ModelProp("Pine_TallA", Nature("tree_pineTallA_detailed"), 8f, PropCollider.Trunk),
                ModelProp("Pine_TallB", Nature("tree_pineTallB_detailed"), 7.5f, PropCollider.Trunk),
                ModelProp("Pine_Round", Nature("tree_pineRoundA"), 6f, PropCollider.Trunk),
            };
            rocks = new[]
            {
                ModelProp("Rock_LargeA", Nature("rock_largeA"), 2.2f, PropCollider.Mesh),
                ModelProp("Rock_LargeB", Nature("rock_largeB"), 2.6f, PropCollider.Mesh),
                ModelProp("Rock_LargeC", Nature("rock_largeC"), 2f, PropCollider.Mesh),
                ModelProp("Rock_TallA", Nature("rock_tallA"), 3.5f, PropCollider.Mesh),
                ModelProp("Stone_TallB", Nature("stone_tallB"), 3.2f, PropCollider.Mesh),
            };
            desertPlants = new[]
            {
                ModelProp("Cactus_Tall", Nature("cactus_tall"), 4f, PropCollider.Trunk),
                ModelProp("Cactus_Short", Nature("cactus_short"), 2.4f, PropCollider.Trunk),
                ModelProp("Palm_Tall", Nature("tree_palmDetailedTall"), 8f, PropCollider.Trunk),
                ModelProp("Palm_Bend", Nature("tree_palmBend"), 7f, PropCollider.Trunk),
            };
            ruinColumns = new[]
            {
                ModelProp("Ruin_Column", Nature("statue_column"), 5f, PropCollider.Mesh),
                ModelProp("Ruin_ColumnBroken", Nature("statue_columnDamaged"), 3.5f, PropCollider.Mesh),
                ModelProp("Ruin_Obelisk", Nature("statue_obelisk"), 6f, PropCollider.Mesh),
                ModelProp("Ruin_Head", Nature("statue_head"), 3f, PropCollider.Mesh),
            };
            smallDecor = new[]
            {
                ModelProp("Decor_FlowerRed", Nature("flower_redA"), 0.6f, PropCollider.None),
                ModelProp("Decor_FlowerYellow", Nature("flower_yellowA"), 0.6f, PropCollider.None),
                ModelProp("Decor_FlowerPurple", Nature("flower_purpleA"), 0.6f, PropCollider.None),
                ModelProp("Decor_Grass", Nature("grass_large"), 0.7f, PropCollider.None),
                ModelProp("Decor_GrassLeafs", Nature("grass_leafsLarge"), 0.8f, PropCollider.None),
                ModelProp("Decor_Bush", Nature("plant_bushLarge"), 1.3f, PropCollider.None),
                ModelProp("Decor_BushDetailed", Nature("plant_bushDetailed"), 1.1f, PropCollider.None),
                ModelProp("Decor_Mushrooms", Nature("mushroom_redGroup"), 0.6f, PropCollider.None),
                ModelProp("Decor_MushroomsTan", Nature("mushroom_tanGroup"), 0.6f, PropCollider.None),
                ModelProp("Decor_Pumpkin", Nature("crop_pumpkin"), 0.7f, PropCollider.None),
                ModelProp("Decor_Stump", Nature("stump_round"), 0.7f, PropCollider.None),
            };
            desertDecor = new[]
            {
                ModelProp("Decor_RockSmall", Nature("rock_largeD"), 0.9f, PropCollider.None),
                ModelProp("Decor_Pot", Nature("pot_large"), 1f, PropCollider.None),
                ModelProp("Decor_CactusSmall", Nature("cactus_short"), 1.2f, PropCollider.None),
                ModelProp("Decor_StatueBlock", Nature("statue_block"), 1f, PropCollider.None),
                ModelProp("Decor_Log", Nature("log_large"), 0.8f, PropCollider.None),
            };
            frozenDecor = new[]
            {
                ModelProp("Decor_StoneA", Nature("stone_largeA"), 1f, PropCollider.None),
                ModelProp("Decor_StoneB", Nature("stone_largeB"), 0.8f, PropCollider.None),
                ModelProp("Decor_PineSmall", Nature("tree_pineSmallA"), 1.8f, PropCollider.None),
            };
            campfirePrefab = ModelProp("Campfire", Nature("campfire_stones"), 0.8f, PropCollider.None);
            tentPrefab = ModelProp("Tent", Nature("tent_detailedOpen"), 2.6f, PropCollider.Mesh);
            logStackPrefab = ModelProp("LogStack", Nature("log_stack"), 1.2f, PropCollider.Mesh);
            signPrefab = ModelProp("Sign", Nature("sign"), 1.6f, PropCollider.None);
            flagPrefab = ModelProp("Flag", Platformer("flag"), 3.5f, PropCollider.None);

            // Keep the names the layouts already use.
            treePrefab = meadowTrees[0];
            rockPrefab = rocks[0];
            pillarPrefab = ruinColumns[0];
            cratePrefab = ModelProp("Crate", Platformer("crate"), 1.6f, PropCollider.Mesh);
        }

        private static GameObject Pick(GameObject[] pool) => pool[Random.Range(0, pool.Length)];

        /// <summary>Non-colliding decoration scatter (flowers, grass…) that avoids gameplay areas.</summary>
        private static void ScatterDecor(GameObject[] pool, int count, int seed, System.Func<Vector3, bool> allowed)
        {
            Transform parent = Empty("Decoration", layoutRoot, Vector3.zero).transform;
            Random.State state = Random.state;
            Random.InitState(seed);
            int placed = 0, attempts = 0;
            while (placed < count && attempts++ < count * 6)
            {
                Vector3 p = new Vector3(Random.Range(-47f, 47f), 0f, Random.Range(-47f, 47f));
                if (!allowed(p)) continue;
                GameObject d = Place(Pick(pool), parent, p, Random.Range(0f, 360f));
                d.transform.localScale = Vector3.one * Random.Range(0.8f, 1.35f);
                placed++;
            }
            Random.state = state;
        }
    }
}
