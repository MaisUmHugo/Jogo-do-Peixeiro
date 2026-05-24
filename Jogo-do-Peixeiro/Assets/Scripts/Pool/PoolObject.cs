using UnityEngine;

public class PoolObject : MonoBehaviour
{
    [SerializeField] private string poolKey;

    public string PoolKey => poolKey;

    public void SetPoolKey(string _poolKey)
    {
        poolKey = _poolKey;
    }
}
