using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{   
    ResourceManager manager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        manager = GetComponentInParent<ResourceManager>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (manager.prefabs.Count > 0) manager.UnloadAsset();
        }
    }
}
