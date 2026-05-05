using UnityEngine;

public class House : MonoBehaviour, IInteractable
{
    [Header("Referências")]
    [SerializeField] private DayCycle dayCycle;
    [SerializeField] private GameObject sleepUI;

    private void OnEnable()
    {
        ResolveReferences();
    }

    // ── IInteractable ────────────────────────────────────────────────────────

    public void Interact()
    {
        ResolveReferences();

        if (sleepUI != null)
            sleepUI.SetActive(true);

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

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ResolveReferences()
    {
        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>(FindObjectsInactive.Include);
    }
}
