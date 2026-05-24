using UnityEngine;

public class FishMarketController : MonoBehaviour, IInteractable
{
    [SerializeField] private FishMarket fishMarket;
    [SerializeField] private DockOwnerUI dockOwnerUI;
    [SerializeField] private int interactionPriority = 45;
    [SerializeField] private string promptText = "Falar";
    [SerializeField] private bool sellDirectlyWhenNoUi = true;

    [Header("Audio")]
    [SerializeField, InspectorName("Direct Sell SFX")] private AudioClip directSellSfx;
    [SerializeField, Range(0f, 1f), InspectorName("Direct Sell SFX Volume")] private float directSellSfxVolume = 1f;

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
            PlayDirectSellSfx();
            HUDWarningUI.Instance?.ShowWarning($"Peixes vendidos: R$ {earnedMoney}");
            return;
        }

        HUDWarningUI.Instance?.ShowWarning("Nenhum peixe no barco.");
    }

    private void PlayDirectSellSfx()
    {
        if (AudioManager.Instance == null || directSellSfx == null)
            return;

        AudioManager.Instance.PlaySfx(directSellSfx, directSellSfxVolume);
    }
}
