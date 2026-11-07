---
tags: [coin-rush, design, index]
---
# Coin Rush: Game Design Vault

Open this folder (`Docs/Design`) as an **Obsidian vault**. Notes link to each other with `[[wikilinks]]`, and every diagram is Mermaid, which Obsidian renders natively. The notes describe the game logic as it is implemented in `Assets/_Project/Scripts`.

## Map of content
- [[Game Loop]]: from the splash screen to victory
- [[Game State Machine]]: GameManager states and transitions
- [[Scoring and Combo]]: points, multiplier, portal quota, time bonus
- [[Player Controller]]: movement, jump, dash, stamina, pad flights
- [[Enemy Behaviour]]: patrol, chase and search finite-state machine
- [[Jump Pads and Navigation]]: aimed pads, chain bounces, ramps and bridges
- [[Procedural Fractal Trees]]: the L-system tree generator
- [[Config and Save Data]]: the external JSON config and the JSON save file
- [[Architecture]]: systems and how they talk (events)

## Core pillars
1. **Collect under pressure:** reach each world's coin quota before the timer runs out.
2. **Flow:** combos reward chaining pickups, and pads, ramps and bridges reward route planning.
3. **Escalation:** every score step adds enemies; the worlds get harder (Meadow → Desert → Frozen).
