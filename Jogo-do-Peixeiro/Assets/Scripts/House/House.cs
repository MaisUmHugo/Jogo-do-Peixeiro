using UnityEngine;

public class House : MonoBehaviour, IInteractable
{
    [Header("Referências")]
    [SerializeField] private DayCycle dayCycle;
    [SerializeField] private GameObject sleepUI;

    private void OnEnable()
    {
        ResolveReferences();

        if (dayCycle != null)
            dayCycle.ForcedSleepRequested += HandleForcedSleepRequested;
    }

    private void OnDisable()
    {
        if (dayCycle != null)
            dayCycle.ForcedSleepRequested -= HandleForcedSleepRequested;
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

    // ── Sono forçado (fim do dia) ─────────────────────────────────────────────

    private void HandleForcedSleepRequested()
    {
        if (sleepUI != null)
            sleepUI.SetActive(false);

        // Cancela pescaria se estiver pescando
        if (FishingManager.instance != null && FishingManager.instance.IsFishing)
            FishingManager.instance.CancelFishing();

        // Salva o estado atual antes de avançar o dia
        // (pode ser OnFoot, OnBoat, etc.)
        GameManager.GameState stateToRestore = GameManager.GameState.OnFoot;

        if (GameManager.instance != null)
        {
            GameManager.GameState current = GameManager.instance.currentState;

            // Preserva OnFoot e OnBoat; qualquer outro estado (InUI, Fishing, Paused)
            // volta para OnFoot como fallback seguro
            if (current == GameManager.GameState.OnFoot ||
                current == GameManager.GameState.OnBoat)
            {
                stateToRestore = current;
            }
        }

        // Avança o dia — o DayCycle reposiciona o horário para wakeUpHour
        if (dayCycle != null)
            dayCycle.NextDay();

        // Restaura o estado correto para o player continuar se movendo
        if (GameManager.instance != null)
            GameManager.instance.SetState(stateToRestore);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ResolveReferences()
    {
        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>(FindObjectsInactive.Include);
    }
}
