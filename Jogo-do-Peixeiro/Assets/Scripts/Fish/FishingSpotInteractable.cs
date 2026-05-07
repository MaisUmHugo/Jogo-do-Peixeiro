using UnityEngine;

public class FishingSpotInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private FishingSpot fishingSpot;
    [SerializeField] private FishingRod fishingRod;
    [SerializeField] private int interactionPriority = 40;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        interactionPriority = Mathf.Max(0, interactionPriority);
    }

    public void Interact()
    {
        ResolveReferences();

        if (fishingRod == null || fishingSpot == null)
            return;

        fishingRod.TryCastToSpot(fishingSpot);
    }

    public int GetInteractionPriority()
    {
        return interactionPriority;
    }

    public bool CanInteract()
    {
        ResolveReferences();

        return fishingRod != null &&
               fishingSpot != null &&
               fishingRod.CanCastToSpot(fishingSpot);
    }

    private void ResolveReferences()
    {
        if (fishingSpot == null)
            fishingSpot = GetComponentInParent<FishingSpot>();

        if (fishingRod == null)
            fishingRod = FindFirstObjectByType<FishingRod>(FindObjectsInactive.Include);
    }
}
