using System.Collections.Generic;
using CoinRush.Collectibles;
using CoinRush.Core;
using UnityEngine;

namespace CoinRush.Spawning
{
    /// <summary>Periodically places a random power-up (health / speed) while fewer than the max are on the field.</summary>
    public class PowerUpSpawner : MonoBehaviour
    {
        [SerializeField] private SpawnArea spawnArea;
        [SerializeField] private List<Pickup> powerUpPrefabs = new List<Pickup>();
        [SerializeField] private Transform container;

        private readonly List<Pickup> active = new List<Pickup>();
        private float timer;

        private int MaxOnField => GameManager.Instance != null ? GameManager.Instance.World.maxPowerUpsOnField : 2;
        private float Interval => GameManager.Instance != null ? GameManager.Instance.World.powerUpSpawnInterval : 12f;

        private void Awake()
        {
            if (container == null) container = transform;
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            timer += Time.deltaTime;
            if (timer < Interval) return;
            timer = 0f;

            active.RemoveAll(p => p == null); // destroyed (collected) pickups become null
            if (active.Count >= MaxOnField || powerUpPrefabs.Count == 0) return;
            if (!spawnArea.TryGetRandomPoint(out Vector3 point)) return;

            Pickup prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Count)];
            active.Add(Instantiate(prefab, point, Quaternion.identity, container));
        }
    }
}
