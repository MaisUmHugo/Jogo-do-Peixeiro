using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Persistent Prefabs")]
    [SerializeField] private GameManager _gameManagerPrefab;
    [SerializeField] private InputHandler _inputHandlerPrefab;
    [SerializeField] private AudioManager _audioManagerPrefab;
    //[SerializeField] private PoolManager _poolManagerPrefab;
    [SerializeField] private InputDeviceDetector _inputDeviceDetectorPrefab;

    private void Awake()
    {
        CreateIfMissing(GameManager.instance, _gameManagerPrefab);
        CreateIfMissing(InputHandler.instance, _inputHandlerPrefab);
        CreateIfMissing(AudioManager.Instance, _audioManagerPrefab);
        //CreateIfMissing(PoolManager.Instance, _poolManagerPrefab);

        if (FindFirstObjectByType<InputDeviceDetector>() == null && _inputDeviceDetectorPrefab != null)
        {
            Instantiate(_inputDeviceDetectorPrefab);
        }
    }

    private void CreateIfMissing<T>(T _instance, T _prefab) where T : MonoBehaviour
    {
        if (_instance != null || _prefab == null)
            return;

        Instantiate(_prefab);
    }
}