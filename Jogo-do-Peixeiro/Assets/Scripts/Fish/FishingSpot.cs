using UnityEngine;
using UnityEngine.InputSystem;

public class FishingSpot : MonoBehaviour
{
    [SerializeField] private FishScriptableObject[] availableFish;

    [Header("Temporary Test")]
    [SerializeField] private bool allowSpaceTest = true;
    [SerializeField] private bool ignoreAllChecksForTest = true;
    [SerializeField] private ShipInventory debugShipInventory;

    private bool boatInRange;
    private ShipInventory currentShipInventory;

    private void Start()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed += TryStartFishing;
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed -= TryStartFishing;
    }

    private void Update()
    {
        if (allowSpaceTest &&
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryStartFishing();
        }
    }

    private void TryStartFishing()
    {
        if (FishingManager.instance == null)
            return;

        if (availableFish == null || availableFish.Length == 0)
        {
            Debug.LogWarning("Esse FishingSpot não tem peixes configurados.");
            return;
        }

        if (ignoreAllChecksForTest)
        {
            ShipInventory inventoryToUse = debugShipInventory != null
                ? debugShipInventory
                : Object.FindFirstObjectByType<ShipInventory>();

            if (inventoryToUse == null)
            {
                Debug.LogWarning("Nenhum ShipInventory encontrado para teste.");
                return;
            }

            Debug.Log("Tentando pescar (modo teste)");
            FishingManager.instance.StartFishing(inventoryToUse, availableFish);
            return;
        }

        if (!boatInRange)
            return;

        if (currentShipInventory == null)
            return;

        if (GameManager.instance == null)
            return;

        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
            return;

        if (FishingManager.instance.IsFishing)
            return;

        Debug.Log("Tentando pescar");
        GameManager.instance.SetState(GameManager.GameState.Fishing);
        FishingManager.instance.StartFishing(currentShipInventory, availableFish);
    }

    public void StartFishingFromRod()
    {
        if (FishingManager.instance == null)
            return;

        if (availableFish == null || availableFish.Length == 0)
        {
            Debug.LogWarning("Esse FishingSpot não tem peixes configurados.");
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
            return;
        }

        ShipInventory inventoryToUse = null;

        if (currentShipInventory != null)
            inventoryToUse = currentShipInventory;
        else
            inventoryToUse = Object.FindFirstObjectByType<ShipInventory>();

        if (inventoryToUse == null)
        {
            Debug.LogWarning("Nenhum ShipInventory encontrado.");
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
            return;
        }

        Debug.Log("Iniciando pesca pela vara");
        FishingManager.instance.StartFishing(inventoryToUse, availableFish);
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

    public int GetInteractionPriority()
    {
        return 0;
    }
}