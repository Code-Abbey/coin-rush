---
tags: [coin-rush, design, state-machine]
---
# Game State Machine
Back to [[00 Coin Rush Design Index]] · Implemented in `Core/GameManager.cs` and `Core/GameState.cs`

The `GameManager` is the only system that changes `GameState`; everything else reacts to `GameEvents.StateChanged`.

```mermaid
stateDiagram-v2
    [*] --> Countdown : world scene starts
    Countdown --> Playing : countdown reaches 0
    Playing --> Paused : Pause pressed
    Paused --> Playing : Pause pressed / Resume
    Playing --> WorldComplete : portal entered (not the final world)
    Playing --> Won : final portal entered
    Playing --> Lost : TimeRemaining <= 0
    Playing --> Lost : PlayerDied
    WorldComplete --> [*] : next world scene loads
    Won --> [*] : results screen
    Lost --> [*] : results screen (retry / menu)
```

| State | Time scale | Cursor | Notes |
|---|---|---|---|
| Countdown | 1 | locked | 0.6 s fade-in, then `countdownSeconds` ticks |
| Playing | 1 | locked | timer runs, combo decays |
| Paused | **0** | free | pause menu |
| WorldComplete | 1 | free | summary card, then next world |
| Won / Lost | 1 | free | high score and run history saved |

Related: [[Game Loop]], [[Scoring and Combo]], [[Config and Save Data]]
