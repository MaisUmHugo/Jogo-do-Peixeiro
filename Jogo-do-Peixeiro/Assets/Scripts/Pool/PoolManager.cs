using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [SerializeField] private List<PoolConfig> poolConfigs = new();

    private readonly Dictionary<string, Queue<GameObject>> pools = new();
    private readonly Dictionary<string, PoolConfig> configLookup = new();
    private readonly Dictionary<string, int> createdCounts = new();
    private readonly Dictionary<string, Transform> poolContainers = new();

    public static PoolManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        PoolManager existing = FindFirstObjectByType<PoolManager>(FindObjectsInactive.Include);

        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject managerObject = new GameObject("PoolManager");
        return managerObject.AddComponent<PoolManager>();
    }

    public static bool TryReturn(GameObject _object)
    {
        return Instance != null && Instance.ReturnObject(_object);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeConfiguredPools();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void EnsurePool(string _poolKey, GameObject _prefab, int _initialSize, bool _canExpand = true)
    {
        if (string.IsNullOrWhiteSpace(_poolKey) || _prefab == null)
            return;

        if (!pools.ContainsKey(_poolKey))
        {
            PoolConfig runtimeConfig = new PoolConfig
            {
                poolKey = _poolKey,
                prefab = _prefab,
                initialSize = Mathf.Max(0, _initialSize),
                canExpand = _canExpand
            };

            RegisterPool(runtimeConfig);
            return;
        }

        PoolConfig config = configLookup[_poolKey];

        if (config.prefab == null)
            config.prefab = _prefab;

        config.initialSize = Mathf.Max(config.initialSize, _initialSize);
        config.canExpand |= _canExpand;

        PrewarmPool(config, config.initialSize);
    }

    public GameObject GetObject(string _poolKey, Vector3 _position, Quaternion _rotation)
    {
        return GetObject(_poolKey, _position, _rotation, null);
    }

    public GameObject GetObject(string _poolKey, Vector3 _position, Quaternion _rotation, Transform _parent)
    {
        if (string.IsNullOrWhiteSpace(_poolKey) || !pools.TryGetValue(_poolKey, out Queue<GameObject> pool))
            return null;

        PoolConfig config = configLookup[_poolKey];
        GameObject pooledObject = GetInactiveObjectFromPool(pool);

        if (pooledObject == null)
        {
            if (!config.canExpand)
                return null;

            pooledObject = CreateNewObject(config);
        }

        Transform pooledTransform = pooledObject.transform;
        pooledTransform.SetParent(_parent, true);
        pooledTransform.localScale = config.prefab.transform.localScale;
        pooledTransform.SetPositionAndRotation(_position, _rotation);
        pooledObject.SetActive(true);

        NotifySpawn(pooledObject);
        return pooledObject;
    }

    public bool ReturnObject(GameObject _object)
    {
        if (_object == null)
            return false;

        PoolObject poolObject = _object.GetComponent<PoolObject>();

        if (poolObject == null || string.IsNullOrWhiteSpace(poolObject.PoolKey))
            return false;

        if (!pools.TryGetValue(poolObject.PoolKey, out Queue<GameObject> pool))
            return false;

        if (!_object.activeSelf)
            return true;

        NotifyReturn(_object);
        _object.SetActive(false);
        _object.transform.SetParent(GetPoolContainer(poolObject.PoolKey), false);
        pool.Enqueue(_object);
        return true;
    }

    public bool IsFromPool(GameObject _object)
    {
        if (_object == null)
            return false;

        PoolObject poolObject = _object.GetComponent<PoolObject>();
        return poolObject != null && pools.ContainsKey(poolObject.PoolKey);
    }

    private void InitializeConfiguredPools()
    {
        for (int i = 0; i < poolConfigs.Count; i++)
            RegisterPool(poolConfigs[i]);
    }

    private void RegisterPool(PoolConfig _config)
    {
        if (_config == null ||
            string.IsNullOrWhiteSpace(_config.poolKey) ||
            _config.prefab == null ||
            pools.ContainsKey(_config.poolKey))
        {
            return;
        }

        pools[_config.poolKey] = new Queue<GameObject>();
        configLookup[_config.poolKey] = _config;
        createdCounts[_config.poolKey] = 0;
        PrewarmPool(_config, _config.initialSize);
    }

    private void PrewarmPool(PoolConfig _config, int _targetSize)
    {
        if (_config == null)
            return;

        if (!_config.canExpand && _targetSize > _config.initialSize)
            _targetSize = _config.initialSize;

        while (createdCounts.TryGetValue(_config.poolKey, out int count) && count < _targetSize)
        {
            GameObject pooledObject = CreateNewObject(_config);
            pools[_config.poolKey].Enqueue(pooledObject);
        }
    }

    private GameObject GetInactiveObjectFromPool(Queue<GameObject> _pool)
    {
        while (_pool.Count > 0)
        {
            GameObject pooledObject = _pool.Dequeue();

            if (pooledObject != null && !pooledObject.activeSelf)
                return pooledObject;
        }

        return null;
    }

    private GameObject CreateNewObject(PoolConfig _config)
    {
        GameObject pooledObject = Instantiate(
            _config.prefab,
            GetPoolContainer(_config.poolKey)
        );

        pooledObject.name = $"{_config.prefab.name} (Pooled)";
        pooledObject.SetActive(false);

        PoolObject poolObject = pooledObject.GetComponent<PoolObject>();

        if (poolObject == null)
            poolObject = pooledObject.AddComponent<PoolObject>();

        poolObject.SetPoolKey(_config.poolKey);
        createdCounts[_config.poolKey] = createdCounts.TryGetValue(_config.poolKey, out int count) ? count + 1 : 1;

        return pooledObject;
    }

    private Transform GetPoolContainer(string _poolKey)
    {
        if (poolContainers.TryGetValue(_poolKey, out Transform container) && container != null)
            return container;

        GameObject containerObject = new GameObject($"{_poolKey} Pool");
        containerObject.transform.SetParent(transform, false);
        poolContainers[_poolKey] = containerObject.transform;
        return containerObject.transform;
    }

    private static void NotifySpawn(GameObject _object)
    {
        IPoolable[] poolables = _object.GetComponentsInChildren<IPoolable>(true);

        for (int i = 0; i < poolables.Length; i++)
            poolables[i].OnSpawnFromPool();
    }

    private static void NotifyReturn(GameObject _object)
    {
        IPoolable[] poolables = _object.GetComponentsInChildren<IPoolable>(true);

        for (int i = 0; i < poolables.Length; i++)
            poolables[i].OnReturnToPool();
    }
}
