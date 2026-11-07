using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
public class ResourceManager : MonoBehaviour
{
    public AssetReference assetReference;
    public List<GameObject> prefabs = new List<GameObject>();
    AsyncOperationHandle<GameObject> handle;

    public void LoadAsset()
    {
        handle = assetReference.InstantiateAsync(transform.position, transform.rotation);
        handle.Completed += handle => {prefabs.Add(handle.Result);};
    }

    public void UnloadAsset()
    {
        foreach (var prefab in prefabs)
        {
            Addressables.ReleaseInstance(prefab);
        }
        prefabs.Clear();
    }

}
