---
tags: [coin-rush, design, architecture]
---
# Architecture
Back to [[00 Coin Rush Design Index]]

Systems are decoupled through a static event hub, `Core/GameEvents.cs`: producers raise events and the UI, audio and spawners subscribe.

```mermaid
flowchart TB
    subgraph Data
        SO[(ScriptableObjects)]
        JSON[(coinrush_config.json)]
        SAVE[(coinrush_save.json)]
    end
    subgraph Core
        GM[GameManager<br/>state, timer, score]
        RS[RunSession<br/>score across worlds]
        HS[HighScoreService]
        EV{{GameEvents}}
    end
    subgraph Gameplay
        PC[PlayerController / Health / PowerUps]
        EN[EnemyBehaviour + EnemySpawner]
        PK[Coins, power-ups + spawners]
        LV[JumpPads · Ramps · Bridges · Portal · Hazards]
        FF[FractalForest]
    end
    subgraph Presentation
        UI[HUD · Results · Menus]
        AU[AudioManager]
    end
    SO --> GM & PC & EN
    JSON --> GM & PC & FF
    PK -->|CollectCoin| GM
    LV -->|EnterPortal| GM
    GM --> RS --> HS --> SAVE
    GM & PC & EN -->|raise| EV
    EV -->|subscribe| UI & AU & EN & PK
```

Key events: `StateChanged`, `CountdownTick`, `TimeChanged`, `WorldStarted`, `WorldCompleted`, `PortalOpened`, `DifficultyIncreased`, `ScoreChanged`, `QuotaProgress`, `ComboChanged`, `PointsAwarded`, `HealthChanged`, `StaminaChanged`, `PowerUpChanged`, `PlayerDied`, `Notification`.
