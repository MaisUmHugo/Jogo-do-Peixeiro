using UnityEngine;

public class FishingSpot : MonoBehaviour
{
    [SerializeField] private FishScriptableObject[] availableFish;

    private bool boatInRange;
    private ShipInventory currentShipInventory;

    private void Start()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed += TryStartFishing;

        //boatInRange = true;
        //currentShipInventory = Object.FindFirstObjectByType<ShipInventory>();
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed -= TryStartFishing;
    }

    private void TryStartFishing()
    {
        if (!boatInRange)
            return;

        if (currentShipInventory == null)
            return;

        if (GameManager.instance == null)
            return;

        if (FishingManager.instance == null)
            return;

        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
            return;

        if (FishingManager.instance.IsFishing)
            return;

        if (availableFish == null || availableFish.Length == 0)
        {
            Debug.LogWarning("Esse FishingSpot não tem peixes configurados.");
            return;
        }

        Debug.Log("Tentando pescar");
        GameManager.instance.SetState(GameManager.GameState.Fishing);
        FishingManager.instance.StartFishing(currentShipInventory, availableFish);
    }

    private void OnTriggerEnter(Collider _other)
    {
        if (!_other.CompareTag("Boat"))
            return;

        Debug.Log("Barco detectado");
        currentShipInventory = _other.GetComponent<ShipInventory>();
        boatInRange = currentShipInventory != null;
    }

    private void OnTriggerExit(Collider _other)
    {
        if (!_other.CompareTag("Boat"))
            return;

        boatInRange = false;
        currentShipInventory = null;
    }
}