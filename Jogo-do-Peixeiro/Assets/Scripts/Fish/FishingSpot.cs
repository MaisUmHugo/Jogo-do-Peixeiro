using UnityEngine;
using UnityEngine.InputSystem;

public class FishingSpot : MonoBehaviour
{
    [SerializeField] private FishScriptableObject[] availableFish;

    //[Header("Debug Test")]
    //[SerializeField] private ShipInventory debugShipInventory;

    public void StartFishingFromRod(ShipInventory _inventory)
    {
        if (FishingManager.instance == null)
            return;

        if (availableFish == null || availableFish.Length == 0)
        {
            Debug.LogWarning("Esse FishingSpot não tem peixes configurados.");
            return;
        }

        if (_inventory == null)
        {
            Debug.LogWarning("Nenhum ShipInventory foi enviado.");
            return;
        }

        if (FishingManager.instance.IsFishing)
            return;

        if (GameManager.instance == null)
            return;

        if (GameManager.instance.currentState == GameManager.GameState.Fishing &&
            FishingManager.instance.IsFishing == false)
        {
        }

        if (_inventory.IsFull)
        {
            Debug.Log("Inventário cheio, não é possível pescar.");
            return;
        }

        Debug.Log("Iniciando pesca pela vara");
        FishingManager.instance.StartFishing(_inventory, availableFish);
    }

    public FishScriptableObject[] GetAvailableFish()
    {
        return availableFish;
    }

    public bool HasFishAvailable()
    {
        return availableFish != null && availableFish.Length > 0;
    }
}