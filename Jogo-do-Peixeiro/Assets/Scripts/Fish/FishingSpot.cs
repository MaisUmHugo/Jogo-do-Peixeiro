using UnityEngine;

public class FishingSpot : MonoBehaviour
{
    [SerializeField] private FishScriptableObject[] availableFish;
    [SerializeField] private float minHorizontalDistance = 4f;

    [Header("Fish Escape")]
    [SerializeField] private bool ignoreEscapeForDebug;
    [SerializeField] private string boatTag = "Boat";
    [SerializeField] private string escapeWarningMessage = "Os peixes fugiram";

    public bool TryStartFishingFromRod(ShipInventory _inventory)
    {
        if (FishingManager.instance == null)
            return false;

        if (_inventory == null)
            return false;

        if (GameManager.instance == null)
            return false;

        Transform player = _inventory.transform.root;

        Vector3 a = player.position;
        Vector3 b = transform.position;

        a.y = 0f;
        b.y = 0f;

        float horizontalDistance = Vector3.Distance(a, b);

        if (horizontalDistance < minHorizontalDistance)
            return false;

        return FishingManager.instance.StartFishing(_inventory, availableFish);
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
        if (ignoreEscapeForDebug)
            return;

        if (!IsBoatCollider(_other))
            return;

        EscapeFish();
    }

    private bool IsBoatCollider(Collider _collider)
    {
        Transform current = _collider.transform;

        while (current != null)
        {
            if (current.CompareTag(boatTag))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void EscapeFish()
    {
        if (HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(escapeWarningMessage);

        gameObject.SetActive(false);
    }
}
