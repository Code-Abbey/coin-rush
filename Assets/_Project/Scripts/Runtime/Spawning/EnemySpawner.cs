using System.Collections.Generic;
using CoinRush.Core;
using CoinRush.Data;
using CoinRush.Enemies;
using UnityEngine;

namespace CoinRush.Spawning
{
    /// <summary>
    /// Spawns the world's starting enemies and adds another one (and speeds everyone up)
    /// every time the GameManager raises a difficulty increase. Can mix two archetypes
    /// (Blob / Brute) using WorldConfig.bruteChance.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private SpawnArea spawnArea;
        [SerializeField] private EnemyBehaviour enemyPrefab;
        [Tooltip("Optional heavy variant.")]
        [SerializeField] private EnemyBehaviour brutePrefab;
        [SerializeField] private Transform player;
        [SerializeField] private Transform container;

        private readonly List<EnemyBehaviour> enemies = new List<EnemyBehaviour>();
        private float speedMultiplier = 1f;

        public IReadOnlyList<EnemyBehaviour> Enemies => enemies;

        private void OnEnable() => GameEvents.DifficultyIncreased += HandleDifficultyIncreased;
        private void OnDisable() => GameEvents.DifficultyIncreased -= HandleDifficultyIncreased;

        private void Start()
        {
            if (container == null) container = transform;
            int count = GameManager.Instance != null ? GameManager.Instance.World.startingEnemies : 2;
            for (int i = 0; i < count; i++) SpawnEnemy();
        }

        private void HandleDifficultyIncreased(int level)
        {
            WorldConfig world = GameManager.Instance.World;
            speedMultiplier = 1f + level * world.speedIncreasePerLevel;
            foreach (EnemyBehaviour enemy in enemies) enemy.SpeedMultiplier = speedMultiplier;

            if (enemies.Count < world.maxEnemies) SpawnEnemy();
        }

        private void SpawnEnemy()
        {
            if (enemyPrefab == null || spawnArea == null) return;

            GameManager gm = GameManager.Instance;
            float minDistance = gm != null ? gm.Config.minEnemySpawnDistance : 15f;
            float bruteChance = gm != null ? gm.World.bruteChance : 0f;
            Vector3? avoid = player != null ? player.position : (Vector3?)null;
            if (!spawnArea.TryGetRandomPoint(out Vector3 point, avoid, minDistance)) return;

            EnemyBehaviour prefab = brutePrefab != null && Random.value < bruteChance ? brutePrefab : enemyPrefab;
            EnemyBehaviour enemy = Instantiate(prefab, point, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), container);
            enemy.name = $"{prefab.name}_{enemies.Count + 1:00}";
            enemy.Initialize(player, spawnArea);
            enemy.SpeedMultiplier = speedMultiplier;
            enemies.Add(enemy);
        }
    }
}
