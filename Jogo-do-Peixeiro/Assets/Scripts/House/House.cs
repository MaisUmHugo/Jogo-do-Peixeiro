using UnityEngine;

public class House : MonoBehaviour, IInteractable
{
    [Header("Referências")]
    [SerializeField] private DayCycle dayCycle;
    [SerializeField] private GameObject sleepUI;

    [Header("Modal")]
    [SerializeField] private bool pauseTimeWhileOpen = true;
    [SerializeField] private bool hideHudWhileOpen = true;
    [SerializeField] private bool blockPauseWhileOpen = true;

    private int modalToken = UIModalManager.InvalidToken;

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        UIModalManager.PopModal(ref modalToken);
    }

    // ── IInteractable ────────────────────────────────────────────────────────

    public void Interact()
    {
        ResolveReferences();

        if (sleepUI != null)
            sleepUI.SetActive(true);

        PushModalState();

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.InUI);
    }

    public bool CanInteract() => true;

    public int GetInteractionPriority() => 0;

    // ── Botões do sleepUI ────────────────────────────────────────────────────

    public void ConfirmSleep()
    {
        if (sleepUI != null)
            sleepUI.SetActive(false);

        UIModalManager.PopModal(ref modalToken);

        if (dayCycle != null)
            dayCycle.NextDay();

        // Sono voluntário: player sempre está em pé perto da casa
        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }

    public void CancelSleep()
    {
        if (sleepUI != null)
            sleepUI.SetActive(false);

        UIModalManager.PopModal(ref modalToken);

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ResolveReferences()
    {
        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>(FindObjectsInactive.Include);
    }

    private void PushModalState()
    {
        if (modalToken != UIModalManager.InvalidToken)
            return;

        UIModalRequest request = UIModalRequest.Create(
            this,
            pauseTimeWhileOpen,
            hideHudWhileOpen,
            blockPauseWhileOpen,
            false,
            CancelSleep
        );

        modalToken = UIModalManager.PushModal(request);
    }
}
