using UnityEngine;

[System.Serializable]
public class PoolConfig
{
    public string poolKey;
    public GameObject prefab;
    [Min(0)] public int initialSize;
    public bool canExpand = true;
}
