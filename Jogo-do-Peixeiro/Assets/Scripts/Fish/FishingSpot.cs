using UnityEngine;

public class FishingSpot : MonoBehaviour
{
    [SerializeField] private FishScriptableObject[] availableFish;
    [SerializeField] private float minHorizontalDistance = 4f;

    public void StartFishingFromRod(ShipInventory _inventory)
    {
        if (FishingManager.instance == null)
            return;

        if (availableFish == null || availableFish.Length == 0)
            return;

        if (_inventory == null)
            return;

        if (FishingManager.instance.IsFishing)
            return;

        if (GameManager.instance == null)
            return;

        if (_inventory.IsFull)
            return;

        Transform player = _inventory.transform.root;

        Vector3 a = player.position;
        Vector3 b = transform.position;

        a.y = 0f;
        b.y = 0f;

        float horizontalDistance = Vector3.Distance(a, b);

        if (horizontalDistance < minHorizontalDistance)
            return;

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