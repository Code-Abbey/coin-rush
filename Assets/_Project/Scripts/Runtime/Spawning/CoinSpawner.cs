using CoinRush.Collectibles;
using CoinRush.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace CoinRush.Spawning
{
    /// <summary>
    /// Keeps a fixed number of coins/gems on the field using Unity's built-in ObjectPool,
    /// so collecting a coin recycles it instead of Instantiate/Destroy every time.
    /// </summary>
    public class CoinSpawner : MonoBehaviour
    {
        [SerializeField] private SpawnArea spawnArea;
        [SerializeField] private Coin coinPrefab;
        [SerializeField] private Coin gemPrefab;
        [Tooltip("Parent for spawned objects to keep the Hierarchy tidy.")]
        [SerializeField] private Transform container;
        [SerializeField, Min(0f)] private float respawnDelay = 0.5f;

        private ObjectPool<Pickup> coinPool;
        private ObjectPool<Pickup> gemPool;
        private int activeCount;
        private float nextSpawnTime;

        private int MaxActive => GameManager.Instance != null ? GameManager.Instance.World.maxActiveCoins : 12;
        private float GemChance => GameManager.Instance != null ? GameManager.Instance.World.gemChance : 0.15f;

        private void Awake()
        {
            if (container == null) container = transform;
            coinPool = CreatePool(coinPrefab);
            gemPool = CreatePool(gemPrefab);
        }

        private void Start()
        {
            // Fill the arena immediately so the player sees coins during the countdown.
            for (int i = 0; i < MaxActive; i++) SpawnOne();
        }

        private void Update()
        {
            if (activeCount >= MaxActive || Time.time < nextSpawnTime) return;
            SpawnOne();
        }

        private ObjectPool<Pickup> CreatePool(Coin prefab)
        {
            if (prefab == null) return null;
            ObjectPool<Pickup> pool = null;
            pool = new ObjectPool<Pickup>(
                createFunc: () =>
                {
                    Coin instance = Instantiate(prefab, container);
                    instance.ReleaseToPool = p => pool.Release(p);
                    return instance;
                },
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p =>
                {
                    p.gameObject.SetActive(false);
                    activeCount--;
                    nextSpawnTime = Time.time + respawnDelay;
                },
                actionOnDestroy: p => Destroy(p.gameObject),
                collectionCheck: true,
                defaultCapacity: 16,
                maxSize: 64);
            return pool;
        }

        private void SpawnOne()
        {
            if (spawnArea == null || !spawnArea.TryGetRandomPoint(out Vector3 point)) return;

            ObjectPool<Pickup> pool = (gemPool != null && Random.value < GemChance) ? gemPool : coinPool;
            if (pool == null) return;

            Pickup pickup = pool.Get();
            pickup.transform.SetPositionAndRotation(point, Quaternion.identity);
            activeCount++;
        }
    }
}
