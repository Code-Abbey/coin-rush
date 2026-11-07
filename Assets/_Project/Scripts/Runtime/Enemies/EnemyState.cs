namespace CoinRush.Enemies
{
    /// <summary>Finite-state-machine states of <see cref="EnemyBehaviour"/>.</summary>
    public enum EnemyState
    {
        Idle,     // waiting for the round to start
        Patrol,   // wandering between random points
        Chase,    // player detected: run at the player and attack in range
        Search    // lost sight: check last known position, then go back to patrol
    }
}
