using UnityEngine;
using UnityEngine.UI;

public class House : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private DayCycle dayCycle;
    [SerializeField] private ForcedSleepController forcedSleepController;
    [SerializeField] private GameObject sleepUI;
    [SerializeField] private Button confirmSleepButton;
    [SerializeField] private Button cancelSleepButton;

    [Header("Modal")]
    [SerializeField] private bool pauseTimeWhileOpen = true;
    [SerializeField] private bool hideHudWhileOpen = true;
    [SerializeField] private bool blockPauseWhileOpen = true;

    private int modalToken = UIModalManager.InvalidToken;
    private bool hasBoundSleepButtons;

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        UIModalManager.PopModal(ref modalToken);
    }

    public void Interact()
    {
        ResolveReferences();

        if (sleepUI == null)
        {
            Debug.LogWarning("[House] SleepPanel não encontrado na cena.", this);
            return;
        }

        BindSleepButtons();
        sleepUI.SetActive(true);
        PushModalState();
        UISelectionHelper.Select(confirmSleepButton, sleepUI);

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.InUI);
    }

    public bool CanInteract() => true;

    public int GetInteractionPriority() => 0;

    public void ConfirmSleep()
    {
        if (sleepUI != null)
            sleepUI.SetActive(false);

        UIModalManager.PopModal(ref modalToken);

        if (forcedSleepController != null && forcedSleepController.StartRegularSleep(dayCycle))
            return;

        if (dayCycle != null)
            dayCycle.NextDay();

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

    private void ResolveReferences()
    {
        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>(FindObjectsInactive.Include);

        if (forcedSleepController == null)
            forcedSleepController = FindFirstObjectByType<ForcedSleepController>(FindObjectsInactive.Include);

        if (sleepUI == null)
            sleepUI = FindInactiveSceneObjectByName("SleepPanel");

        ResolveSleepButtons();
        BindSleepButtons();
    }

    private void ResolveSleepButtons()
    {
        if (sleepUI == null)
            return;

        Button[] buttons = sleepUI.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            string buttonName = button.gameObject.name.ToLowerInvariant();

            if (confirmSleepButton == null &&
                (buttonName.Contains("confirm") || buttonName.Contains("yes") || buttonName.Contains("sim")))
            {
                confirmSleepButton = button;
                continue;
            }

            if (cancelSleepButton == null &&
                (buttonName.Contains("cancel") || buttonName.Contains("no") || buttonName.Contains("nao")))
            {
                cancelSleepButton = button;
            }
        }

        if (confirmSleepButton == null && buttons.Length > 0)
            confirmSleepButton = buttons[0];

        if (cancelSleepButton == null && buttons.Length > 1)
            cancelSleepButton = buttons[1] == confirmSleepButton ? buttons[0] : buttons[1];
    }

    private void BindSleepButtons()
    {
        if (hasBoundSleepButtons)
            return;

        if (confirmSleepButton != null)
        {
            confirmSleepButton.onClick.RemoveListener(ConfirmSleep);
            confirmSleepButton.onClick.AddListener(ConfirmSleep);
        }

        if (cancelSleepButton != null)
        {
            cancelSleepButton.onClick.RemoveListener(CancelSleep);
            cancelSleepButton.onClick.AddListener(CancelSleep);
        }

        hasBoundSleepButtons = confirmSleepButton != null || cancelSleepButton != null;
    }

    private GameObject FindInactiveSceneObjectByName(string _name)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate == null ||
                !candidate.gameObject.scene.IsValid() ||
                candidate.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            if (candidate.name == _name || candidate.name.StartsWith(_name + " "))
                return candidate.gameObject;
        }

        return null;
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
