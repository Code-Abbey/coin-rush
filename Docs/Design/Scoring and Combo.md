---
tags: [coin-rush, design, scoring]
---
# Scoring and Combo
Back to [[00 Coin Rush Design Index]] · Implemented in `GameManager.CollectCoin`

## Rules
- Every pickup within `comboWindow` (2.2 s) of the previous one continues the **combo**.
- **Multiplier** = `min(maxMultiplier, 1 + (combo − 1) / pickupsPerMultiplierStep)`, so +1 every 5 pickups, up to ×4.
- **Points** = `baseValue × multiplier` (coin = 1, gem = 5).
- The **portal opens** when the world's points reach `coinQuota` (Meadow 30, Desert 45, Frozen 60).
- **Difficulty** rises by 1 every `scorePerNewEnemy` points, and each step spawns another enemy.
- **Time bonus** on entering the portal = `TimeRemaining × timeBonusPerSecond`.

```mermaid
flowchart LR
    P[Pickup] --> W{Within combo window?}
    W -->|yes| C1[combo + 1]
    W -->|no| C0[combo = 1]
    C1 & C0 --> M[multiplier = min 4, 1 + combo-1 div 5]
    M --> S[points = base × multiplier]
    S --> Q{world points ≥ quota?}
    Q -->|yes| O[Open portal]
    S --> D{crossed a difficulty step?}
    D -->|yes| E[Spawn enemy, speed up]
```

All of these numbers can be tuned in `coinrush_config.json` (see [[Config and Save Data]]).
