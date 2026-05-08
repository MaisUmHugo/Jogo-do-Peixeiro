using UnityEngine;

public class FishMarketController : MonoBehaviour, IInteractable
{
    [SerializeField] private FishMarket fishMarket;
    [SerializeField] private DockOwnerUI dockOwnerUI;
    [SerializeField] private int interactionPriority = 45;
    [SerializeField] private string promptText = "Falar";
    [SerializeField] private bool sellDirectlyWhenNoUi = true;

    public string PromptText => promptText;
    public Transform PromptPoint => transform;

    private void Awake()
    {
        if (fishMarket == null)
            fishMarket = GetComponent<FishMarket>();

        if (dockOwnerUI == null)
            dockOwnerUI = FindFirstObjectByType<DockOwnerUI>(FindObjectsInactive.Include);
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

        if (dockOwnerUI != null)
        {
            dockOwnerUI.Open(fishMarket);
            return;
        }

        if (!sellDirectlyWhenNoUi)
        {
            HUDWarningUI.Instance?.ShowWarning("Painel do dono da doca nao encontrado.");
            return;
        }

        if (fishMarket.TrySellAllFish(out int earnedMoney))
        {
            HUDWarningUI.Instance?.ShowWarning($"Peixes vendidos: R$ {earnedMoney}");
            return;
        }

        HUDWarningUI.Instance?.ShowWarning("Nenhum peixe no barco.");
    }
}
