using UnityEngine;
using UnityEngine.InputSystem;

public class FishingSpot : MonoBehaviour
{
    [SerializeField] private FishScriptableObject[] availableFish;

    [Header("Debug Test")]
    [SerializeField] private bool allowSpaceTest = false;
    [SerializeField] private ShipInventory debugShipInventory;

    private bool boatInRange;
    private ShipInventory currentShipInventory;

    private void Update()
    {
        if (!allowSpaceTest)
            return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartFishingFromRod();
        }
    }

    public void StartFishingFromRod()
    {
        if (FishingManager.instance == null)
            return;

        if (availableFish == null || availableFish.Length == 0)
        {
            Debug.LogWarning("Esse FishingSpot não tem peixes configurados.");
            return;
        }

        if (FishingManager.instance.IsFishing)
            return;

        ShipInventory inventoryToUse = null;

        if (allowSpaceTest && debugShipInventory != null)
        {
            inventoryToUse = debugShipInventory;
        }
        else
        {
            if (!boatInRange)
            {
                Debug.Log("O barco não está na área de pesca.");
                return;
            }

            inventoryToUse = currentShipInventory;
        }

        if (inventoryToUse == null)
        {
            Debug.LogWarning("Nenhum ShipInventory encontrado.");
            return;
        }

        if (GameManager.instance == null)
            return;

        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
            return;

        if (inventoryToUse.IsFull)
        {
            Debug.Log("Inventário cheio, não é possível pescar.");
            return;
        }

        Debug.Log("Iniciando pesca pela vara");
        FishingManager.instance.StartFishing(inventoryToUse, availableFish);
    }

    public FishScriptableObject[] GetAvailableFish()
    {
        return availableFish;
    }

    public bool HasFishAvailable()
    {
        return availableFish != null && availableFish.Length > 0;
    }

    private void OnTriggerEnter(Collider _other)
    {
        if (!_other.CompareTag("Boat"))
            return;

        currentShipInventory = _other.GetComponent<ShipInventory>();
        boatInRange = currentShipInventory != null;

        if (boatInRange)
            Debug.Log("Barco detectado no FishingSpot.");
    }

    private void OnTriggerExit(Collider _other)
    {
        if (!_other.CompareTag("Boat"))
            return;

        boatInRange = false;
        currentShipInventory = null;
    }
}