using System;
using UnityEngine;

public class LoadAsset : MonoBehaviour
{
    ResourceManager manager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        manager = GetComponentInParent<ResourceManager>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (manager.prefabs.Count == 0) manager.LoadAsset();
        }
    }
}
