# Coin Rush

**Coin Rush** is a colourful 3D collect-a-thon made with **Unity 6**. Pick a hero, race against the clock to collect enough coins to open each world's portal, avoid the guards and hazards, and escape through all three worlds: **Sunny Meadow → Desert Ruins → Frozen Night**.

All game code is written in C#.

**▶ Play it in your browser:** https://code-abbey.github.io/coin-rush/ (desktop Chrome, Edge, Firefox or Safari; keyboard or gamepad. The first load downloads about 25 MB.)

The browser version uses lighter graphics than the editor (no anti-aliasing or ambient occlusion, shorter shadows) and a capped resolution on high-DPI (Retina) screens. If the browser ever resets the game's graphics, a message offers to reload the page. Add `?fps` to the address to show the frame rate, or `?quality=high` for the full desktop graphics: https://code-abbey.github.io/coin-rush/?quality=high&fps

---

## Contents
1. [What you need](#what-you-need)
2. [Open and play the game](#open-and-play-the-game)
3. [How to play](#how-to-play)
4. [Controls](#controls)
5. [Change the game without coding (JSON settings)](#change-the-game-without-coding-json-settings)
6. [Where your progress is saved](#where-your-progress-is-saved)
7. [For developers](#for-developers)
8. [Project structure](#project-structure)
9. [Credits](#credits)

---

## What you need
- **Unity Hub**: free from [unity.com/download](https://unity.com/download).
- **Unity 6.3 LTS, version `6000.3.6f1`**: install it through Unity Hub (Installs → Install Editor). Other Unity 6 versions may work, but this is the version the project was made with.
- A Windows or macOS computer. A keyboard and mouse or a game controller both work.

## Open and play the game
1. Download or copy the `CoinRush` folder to your computer.
2. Open **Unity Hub** → **Projects** → **Add** → **Add project from disk**, then select the `CoinRush` folder.
3. Click the project to open it. *The first time, Unity needs a few minutes to import everything; this is normal.*
4. In the **Project** window, open `Assets/_Project/Scenes/Splash.unity` (double-click it).
5. Press the **▶ Play** button at the top of the editor.
   - Tip: in the Game view, set the play mode to **Play Maximized** for full screen.
6. Press any key on the title screen, watch (or skip) the short story, then choose **PLAY** in the main menu.

The game flows like this: **title screen → story → main menu → hero select → World 1 → World 2 → World 3 → results**.

## How to play
**Goal:** in each world, collect enough coins to open the **portal**, then run into it before the timer reaches zero. Clear all three worlds to win.

- **Coins** are worth 1 point, **gems** are worth 5.
- **Combos:** keep collecting quickly (within about 2 seconds of the last pickup) to build a combo. Every 5 pickups raise your score multiplier, up to **×4**.
- **Portal:** once you reach the world's coin target, the portal opens and an arrow on screen points you to it.
- **Time bonus:** seconds left on the clock when you enter the portal are turned into bonus points.
- **Health** carries over to the next world, so play carefully.
- You lose if your health or the timer runs out. You can then retry or go back to the menu.

### The worlds
| World | Coins needed | Time | What's special |
|---|---|---|---|
| 1. Sunny Meadow | 30 | 2:30 | Ramps up to a plateau, moving platforms, jump pads up to floating sky islands |
| 2. Desert Ruins | 45 | 2:50 | Lava rivers (use the bridges!), spinning blades, a temple you climb with jump pads |
| 3. Frozen Night | 60 | 3:05 | A freezing lake that hurts, slippery ice, two-step jump pads up to the cliff tops |

### Heroes
Each hero has different strengths (speed, jump height and stamina).

| Hero | Style |
|---|---|
| Marcus | Explorer, a balanced all-rounder |
| Sunny | Sprinter, the fastest, but tires quickly |
| Zara | Acrobat, jumps the highest |
| Bruno | Strongman, slower, but can sprint for a long time |
| Agent Grey | Spy, quick and springy |

### Power-ups
| Power-up | What it does |
|---|---|
| Speed boost | Run faster for about 6 seconds |
| Coin magnet | Pulls nearby coins towards you for about 8 seconds |
| Shield | Blocks the next hit (lasts up to 15 seconds) |
| Health pack | Restores some health |

### Dangers
- **Hogs** patrol and charge when they see you; **orc brutes** are slower but hit much harder. Enemies glow red when they spot you, so break their line of sight to escape.
- More enemies join as your score in a world goes up.
- Enemies follow simple hand-written rules (patrol, chase, search). There is no machine-learning AI in the game.
- **Lava**, **spinning blades** and the **freezing lake** hurt you. **Ice** is slippery.
- **Dash** makes you briefly invulnerable, so use it to escape tight spots.

**Tips:** glowing **jump pads** throw you exactly onto the high platform they face, where the gems usually are. Follow the coin trails: they lead up ramps, across bridges and up the temple stairs.

## Controls
| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | WASD or arrow keys | Left stick |
| Sprint | Left Shift | LT or L3 |
| Jump | Space | A (Cross) |
| Dash (brief invulnerability) | F or right mouse button | B (Circle) |
| Rotate camera | Mouse or Q / E | Right stick |
| Zoom | Mouse wheel | D-pad up / down |
| Pause | Esc or P | Start |

You can also see the controls in the game: open **CONTROLS** in the main menu or the pause menu.
*In the Unity editor, Esc may be taken by the editor itself; use **P** to pause instead.*

## Change the game without coding (JSON settings)
The file **`Assets/StreamingAssets/Config/coinrush_config.json`** holds the game's main settings. Open it in any text editor, change a number, save, and press Play. No rebuilding is needed.

You can change:
- **rules:** combo time window, pickups per multiplier step, maximum multiplier, time bonus, countdown length
- **player:** walk speed, sprint speed, jump height, gravity, dash speed and cooldown, stamina, health
- **worlds** (one entry per world): coins needed, time limit, number of coins on the map, gem chance, number of enemies, power-up frequency
- **forests** (one entry per world): the generated trees: how many, their shape, size and colours, and a "seed" that gives a different forest

Example: give yourself more time in the first world.
```json
{
  "sceneName": "World1_Meadow",
  "coinQuota": 30,
  "timeLimit": 200.0
}
```
If you delete a value or set it to 0, the game simply uses its built-in default. In Unity, **Coin Rush › Export Config to JSON** recreates the file with the default values.

> Unity copies everything in the `StreamingAssets` folder into the finished game unchanged. That's why the settings file lives there: it stays editable even after the game is built.

## Where your progress is saved
Your best score, fastest win and a history of your runs (hero, score, coins, best combo, time) are saved as JSON in:
- **macOS:** `~/Library/Application Support/Abiodun Adefusi/Coin Rush/coinrush_save.json`
- **Windows:** `%USERPROFILE%\AppData\LocalLow\Abiodun Adefusi\Coin Rush\coinrush_save.json`

Delete that file to reset your records.

In the browser version, progress is saved in the browser's own storage for that site instead.

## For developers
### Menu tools (the **Coin Rush** menu at the top of the Unity editor)
| Menu item | What it does |
|---|---|
| **Build Everything** | Regenerates all scenes, prefabs, materials and UI from code. ⚠️ Any manual changes to the scenes are replaced. Your JSON settings file is kept. |
| **Export Config to JSON** | Writes the settings file from the current default values |
| **Render Fractal Tree Previews** | Saves pictures of the generated trees to `Docs/Design/images` |
| **Build Web Version** | Builds the browser (WebGL) version into `Builds/WebGL`. Needs the Web module for Unity 6000.3.6f1 (Unity Hub → Installs → Add modules). |
| **Build Web Version (Debug)** | A development build of the browser version with full error messages and the F1–F3 testing shortcuts, written to `Builds/WebGL-Debug`. Use it to track down browser-only problems. |
| **Apply Recommended Project Settings** | Linear colour space, new Input System only, VS Code as the script editor |

### Testing shortcuts (only in the editor and development builds)
- **F1:** +10 coins
- **F2:** jump to the portal
- **F3:** lose the run (to test the defeat screen)

### Technical highlights
- **Procedural trees:** every world grows its own forest when it loads. The trees come from an L-system, a small set of growth rules applied repeatedly with a random seed (`FractalTree.cs`, `FractalForest.cs`).
- **Data-driven design:** gameplay values live in ScriptableObject assets (`Assets/_Project/Data`), and the JSON file can override them while the game runs.
- **Event-based architecture:** gameplay systems talk through a central event hub (`GameEvents`), so the HUD, audio and spawners stay independent.
- **LINQ** is used for the run statistics (best runs, rank, wins, average score, favourite hero), settings lookups, and the forest and level-generation queries.
- **Design documentation:** the `Docs/Design` folder is an [Obsidian](https://obsidian.md) vault. Open it with Obsidian ("Open folder as vault") to read linked notes with diagrams of the game loop, the game states, scoring, the player, enemy behaviour, jump pads, the procedural trees, the save data and the overall architecture. The notes are plain Markdown, so GitHub and most editors can show them too.

## Project structure
```
CoinRush/
├── Assets/
│   ├── _Project/               ← everything made for this game
│   │   ├── Art/                models, materials, shaders, fonts, sprites, animations
│   │   ├── Audio/              music and sound effects (+ licence files)
│   │   ├── Data/               game settings (ScriptableObjects): worlds, heroes, enemies, player
│   │   ├── Prefabs/            player, enemies, pickups, level pieces, props, effects
│   │   ├── Scenes/             Splash, Story, MainMenu, World1_Meadow, World2_Desert, World3_Frozen
│   │   ├── Scripts/
│   │   │   ├── Runtime/        the game code (Core, Player, Enemies, UI, World, …)
│   │   │   └── Editor/         the "Coin Rush" menu tools
│   │   └── Settings/           input actions and rendering (URP) settings
│   ├── StreamingAssets/Config/ coinrush_config.json (editable settings)
│   └── TextMesh Pro/           Unity's text system resources
├── Docs/Design/                design notes and diagrams (Obsidian vault)
├── Packages/                   Unity package list
└── ProjectSettings/            Unity project settings
```
Unity creates the `Library`, `Logs`, `Temp` and `UserSettings` folders automatically. You don't need to copy or share them; they are rebuilt when you open the project.

## Credits
All third-party assets are **CC0 (public domain)** unless noted. Credit isn't required for CC0 assets, but it's given here.
- **3D models:** [Kenney](https://www.kenney.nl) (Mini Characters, Mini Dungeon, Cube Pets, Nature Kit, Platformer Kit), via OpenGameArt.org.
- **Hero portraits:** from Kenney's Mini Characters pack.
- **Music:** Juhani Junkala (CC0, OpenGameArt.org). License note: `Assets/_Project/Audio/Music/`.
- **Sound effects:** Juhani Junkala, *The Essential Retro Video Game Sound Effects Collection*; Kenney, *Interface Sounds*. License notes: `Assets/_Project/Audio/SFX/`.
- **Fonts:** Fredoka and Inter (Google Fonts, SIL Open Font License 1.1; licences in `Assets/_Project/Art/Fonts/`). LiberationSans from TextMesh Pro is kept as a fallback.
- **Everything else** (level geometry, procedural trees, UI icons and sprites, shaders, code): made for this project.
- Made with **Unity 6**.
