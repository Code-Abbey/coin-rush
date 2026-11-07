namespace CoinRush.Core
{
    /// <summary>High-level states of a world. Driven exclusively by <see cref="GameManager"/>.</summary>
    public enum GameState
    {
        Countdown,
        Playing,
        Paused,
        WorldComplete, // portal entered, summary shown, next world loading
        Won,           // final world cleared
        Lost
    }
}
