using UnityEngine;

public class FishMarketController : MonoBehaviour, IInteractable
{
    [SerializeField] private FishMarket fishMarket;
    [SerializeField] private DockOwnerUI dockOwnerUI;
    [SerializeField] private int interactionPriority = 45;
    [SerializeField] private string promptText = "Falar";
    [SerializeField] private bool sellDirectlyWhenNoUi = true;

    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform playerRoot;
    [SerializeField, Min(0.1f)] private float interactionRange = 4f;
    [SerializeField] private bool requireOnFootState = true;

    [Header("Audio")]
    [SerializeField, InspectorName("Direct Sell SFX")] private AudioClip directSellSfx;
    [SerializeField, Range(0f, 1f), InspectorName("Direct Sell SFX Volume")] private float directSellSfxVolume = 1f;

    public string PromptText => promptText;
    public Transform PromptPoint => interactionPoint != null ? interactionPoint : transform;

    private void Awake()
    {
        if (fishMarket == null)
            fishMarket = GetComponent<FishMarket>();

        if (dockOwnerUI == null)
            dockOwnerUI = FindFirstObjectByType<DockOwnerUI>(FindObjectsInactive.Include);
    }

    public bool CanInteract()
    {
        if (fishMarket == null)
            return false;

        if (requireOnFootState &&
            GameManager.instance != null &&
            GameManager.instance.currentState != GameManager.GameState.OnFoot)
        {
            return false;
        }

        return IsPlayerInRange();
    }

    public int GetInteractionPriority()
    {
        return interactionPriority;
    }

    public void Interact()
    {
        if (!CanInteract())
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

    private bool IsPlayerInRange()
    {
        Transform playerTransform = ResolvePlayerRoot();

        if (playerTransform == null)
            return false;

        Transform referencePoint = PromptPoint;
        return Vector3.Distance(playerTransform.position, referencePoint.position) <= interactionRange;
    }

    private Transform ResolvePlayerRoot()
    {
        if (playerRoot != null)
            return playerRoot;

        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Exclude);

        if (playerMove != null)
        {
            playerRoot = playerMove.transform;
            return playerRoot;
        }

        CharacterController characterController = FindFirstObjectByType<CharacterController>(FindObjectsInactive.Exclude);

        if (characterController != null)
            playerRoot = characterController.transform;

        return playerRoot;
    }

    private void PlayDirectSellSfx()
    {
        if (AudioManager.Instance == null || directSellSfx == null)
            return;

        AudioManager.Instance.PlaySfx(directSellSfx, directSellSfxVolume);
    }
}
