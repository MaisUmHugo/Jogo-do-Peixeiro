using UnityEngine;

public class FishMarketController : MonoBehaviour, IInteractable
{
    [SerializeField] private FishMarket fishMarket;
    [SerializeField] private int interactionPriority = 45;
    [SerializeField] private string promptText = "Vender peixes";

    public string PromptText => promptText;
    public Transform PromptPoint => transform;

    private void Awake()
    {
        if (fishMarket == null)
            fishMarket = GetComponent<FishMarket>();
    }

    public bool CanInteract()
    {
        return fishMarket != null;
    }

    public int GetInteractionPriority()
    {
        return interactionPriority;
    }

    public void Interact()
    {
        if (fishMarket == null)
            return;

        if (fishMarket.TrySellAllFish(out int earnedMoney))
        {
            HUDWarningUI.Instance?.ShowWarning($"Peixes vendidos: R$ {earnedMoney}");
            return;
        }

        HUDWarningUI.Instance?.ShowWarning("Nenhum peixe no barco.");
    }
}
