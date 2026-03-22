using System.Collections;
using UnityEngine;

public class FishingManager : MonoBehaviour
{
    public static FishingManager instance;

    [SerializeField] private float fishingTime = 2f;

    public bool IsFishing { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public void StartFishing(ShipInventory _shipInventory, FishScriptableObject[] _availableFish)
    {
        if (IsFishing)
            return;

        if (_shipInventory == null)
        {
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
            return;
        }

        if (_availableFish == null || _availableFish.Length == 0)
        {
            Debug.LogWarning("Nenhum peixe configurado nesse spot.");
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
            return;
        }

        StartCoroutine(FishingRoutine(_shipInventory, _availableFish));
    }

    private IEnumerator FishingRoutine(ShipInventory _shipInventory, FishScriptableObject[] _availableFish)
    {
        IsFishing = true;

        Debug.Log("Pescando...");

        yield return new WaitForSeconds(fishingTime);

        int randomIndex = Random.Range(0, _availableFish.Length);
        FishData fish = new FishData(_availableFish[randomIndex]);

        bool addedSuccessfully = _shipInventory.TryAddFish(fish);

        if (addedSuccessfully)
            Debug.Log($"Peixe capturado: {fish.typeOfFish.fishName} - {fish.weight}kg");
        else
            Debug.Log("Inventário cheio.");

        IsFishing = false;
        GameManager.instance.SetState(GameManager.GameState.OnBoat);
    }
}