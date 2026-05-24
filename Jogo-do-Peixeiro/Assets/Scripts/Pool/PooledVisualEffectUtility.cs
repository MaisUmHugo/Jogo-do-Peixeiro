using UnityEngine;
using UnityEngine.VFX;

public static class PooledVisualEffectUtility
{
    public static void EnsurePool(
        string _poolKey,
        VisualEffect _prefab,
        int _initialSize,
        bool _usePool = true,
        bool _canExpand = true)
    {
        if (!_usePool ||
            _prefab == null ||
            string.IsNullOrWhiteSpace(_poolKey))
        {
            return;
        }

        PoolManager.GetOrCreate().EnsurePool(_poolKey, _prefab.gameObject, Mathf.Max(0, _initialSize), _canExpand);
    }

    public static VisualEffect Spawn(
        VisualEffect _prefab,
        string _poolKey,
        Vector3 _position,
        Quaternion _rotation,
        Transform _parent,
        bool _usePool,
        int _poolSize,
        float _lifetime,
        bool _autoReturn,
        out GameObject _rootObject)
    {
        _rootObject = null;

        if (_prefab == null)
            return null;

        if (_usePool && !string.IsNullOrWhiteSpace(_poolKey))
        {
            EnsurePool(_poolKey, _prefab, _poolSize, true, true);

            GameObject pooledObject = PoolManager.Instance != null
                ? PoolManager.Instance.GetObject(_poolKey, _position, _rotation, _parent)
                : null;

            if (pooledObject != null)
            {
                VisualEffect pooledEffect = ResolveVisualEffect(pooledObject);

                if (pooledEffect != null)
                {
                    _rootObject = pooledObject;
                    ConfigureAutoReturn(_rootObject, _lifetime, _autoReturn);
                    return pooledEffect;
                }

                PoolManager.TryReturn(pooledObject);
            }
        }

        VisualEffect instance = Object.Instantiate(_prefab, _position, _rotation, _parent);
        _rootObject = instance.gameObject;
        ConfigureAutoReturn(_rootObject, _lifetime, _autoReturn);
        return instance;
    }

    public static void Release(GameObject _rootObject)
    {
        if (_rootObject == null)
            return;

        if (PoolManager.TryReturn(_rootObject))
            return;

        Object.Destroy(_rootObject);
    }

    private static VisualEffect ResolveVisualEffect(GameObject _rootObject)
    {
        VisualEffect visualEffect = _rootObject.GetComponent<VisualEffect>();

        if (visualEffect != null)
            return visualEffect;

        return _rootObject.GetComponentInChildren<VisualEffect>(true);
    }

    private static void ConfigureAutoReturn(GameObject _rootObject, float _lifetime, bool _autoReturn)
    {
        PooledAutoReturn autoReturn = _rootObject.GetComponent<PooledAutoReturn>();

        if (autoReturn == null && _autoReturn)
            autoReturn = _rootObject.AddComponent<PooledAutoReturn>();

        if (autoReturn != null)
            autoReturn.Configure(Mathf.Max(0.01f, _lifetime), _autoReturn);
    }
}
