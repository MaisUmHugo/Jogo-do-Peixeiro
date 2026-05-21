using UnityEngine;
using UnityEngine.InputSystem;

public class BuildCheatController : MonoBehaviour
{
    private const string CheatObjectName = "BuildCheatController";

    private static BuildCheatController instance;

    [Header("Settings")]
    [SerializeField] private bool _enableCheats = true;
    [SerializeField] private bool _requireModifier = true;
    [SerializeField] private bool _resetCampaignStateOnSaveReset = true;

    [Header("Keyboard Bindings")]
    [SerializeField] private string _cycleFishingAreaKeyboardBinding = "<Keyboard>/f1";
    [SerializeField] private string _advanceDayKeyboardBinding = "<Keyboard>/f2";
    [SerializeField] private string _completeTaskKeyboardBinding = "<Keyboard>/f3";
    [SerializeField] private string _resetSaveKeyboardBinding = "<Keyboard>/f4";

    [Header("Gamepad Bindings")]
    [SerializeField] private string _cycleFishingAreaGamepadBinding = "<Gamepad>/dpad/up";
    [SerializeField] private string _advanceDayGamepadBinding = "<Gamepad>/dpad/right";
    [SerializeField] private string _completeTaskGamepadBinding = "<Gamepad>/dpad/down";
    [SerializeField] private string _resetSaveGamepadBinding = "<Gamepad>/dpad/left";

    private InputAction _cycleFishingAreaAction;
    private InputAction _advanceDayAction;
    private InputAction _completeTaskAction;
    private InputAction _resetSaveAction;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        BuildCheatController existingController = FindFirstObjectByType<BuildCheatController>(FindObjectsInactive.Include);

        if (existingController != null)
        {
            instance = existingController;
            return;
        }

        GameObject cheatObject = new GameObject(CheatObjectName);
        DontDestroyOnLoad(cheatObject);
        cheatObject.AddComponent<BuildCheatController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        CreateActions();
        RegisterActionCallbacks();
        EnableActions();
    }

    private void OnDisable()
    {
        UnregisterActionCallbacks();
        DisposeActions();
    }

    private void CreateActions()
    {
        _cycleFishingAreaAction = CreateButtonAction(
            "Cheat Cycle Fishing Area",
            _cycleFishingAreaKeyboardBinding,
            _cycleFishingAreaGamepadBinding);

        _advanceDayAction = CreateButtonAction(
            "Cheat Advance Day",
            _advanceDayKeyboardBinding,
            _advanceDayGamepadBinding);

        _completeTaskAction = CreateButtonAction(
            "Cheat Complete Task",
            _completeTaskKeyboardBinding,
            _completeTaskGamepadBinding);

        _resetSaveAction = CreateButtonAction(
            "Cheat Reset Save",
            _resetSaveKeyboardBinding,
            _resetSaveGamepadBinding);
    }

    private InputAction CreateButtonAction(string _actionName, string _keyboardBinding, string _gamepadBinding)
    {
        InputAction inputAction = new InputAction(_actionName, InputActionType.Button);
        AddBindingIfValid(inputAction, _keyboardBinding);
        AddBindingIfValid(inputAction, _gamepadBinding);
        return inputAction;
    }

    private void AddBindingIfValid(InputAction _inputAction, string _binding)
    {
        if (_inputAction == null || string.IsNullOrWhiteSpace(_binding))
            return;

        _inputAction.AddBinding(_binding);
    }

    private void RegisterActionCallbacks()
    {
        if (_cycleFishingAreaAction != null)
            _cycleFishingAreaAction.performed += HandleCycleFishingArea;

        if (_advanceDayAction != null)
            _advanceDayAction.performed += HandleAdvanceDay;

        if (_completeTaskAction != null)
            _completeTaskAction.performed += HandleCompleteTask;

        if (_resetSaveAction != null)
            _resetSaveAction.performed += HandleResetSave;
    }

    private void UnregisterActionCallbacks()
    {
        if (_cycleFishingAreaAction != null)
            _cycleFishingAreaAction.performed -= HandleCycleFishingArea;

        if (_advanceDayAction != null)
            _advanceDayAction.performed -= HandleAdvanceDay;

        if (_completeTaskAction != null)
            _completeTaskAction.performed -= HandleCompleteTask;

        if (_resetSaveAction != null)
            _resetSaveAction.performed -= HandleResetSave;
    }

    private void EnableActions()
    {
        _cycleFishingAreaAction?.Enable();
        _advanceDayAction?.Enable();
        _completeTaskAction?.Enable();
        _resetSaveAction?.Enable();
    }

    private void DisposeActions()
    {
        DisposeAction(ref _cycleFishingAreaAction);
        DisposeAction(ref _advanceDayAction);
        DisposeAction(ref _completeTaskAction);
        DisposeAction(ref _resetSaveAction);
    }

    private void DisposeAction(ref InputAction _inputAction)
    {
        if (_inputAction == null)
            return;

        _inputAction.Disable();
        _inputAction.Dispose();
        _inputAction = null;
    }

    private void HandleCycleFishingArea(InputAction.CallbackContext _context)
    {
        TryRunCheat(CycleFishingArea);
    }

    private void HandleAdvanceDay(InputAction.CallbackContext _context)
    {
        TryRunCheat(AdvanceDay);
    }

    private void HandleCompleteTask(InputAction.CallbackContext _context)
    {
        TryRunCheat(CompleteTask);
    }

    private void HandleResetSave(InputAction.CallbackContext _context)
    {
        TryRunCheat(ResetSave);
    }

    private void TryRunCheat(System.Action _cheatAction)
    {
        if (!_enableCheats || !IsModifierPressed())
            return;

        _cheatAction?.Invoke();
    }

    private bool IsModifierPressed()
    {
        if (!_requireModifier)
            return true;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed))
            return true;

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && (gamepad.leftStickButton.isPressed || gamepad.rightStickButton.isPressed);
    }

    private void CycleFishingArea()
    {
        FishingSpotSpawner[] spawners = FindObjectsByType<FishingSpotSpawner>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (spawners == null || spawners.Length == 0)
        {
            ShowCheatFeedback("Cheat: nenhum spawner de pesca encontrado.");
            return;
        }

        string activeAreaName = string.Empty;

        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] == null)
                continue;

            activeAreaName = spawners[i].CycleDebugFishingArea();
        }

        ShowCheatFeedback($"Cheat: area de pesca -> {activeAreaName}");
    }

    private void AdvanceDay()
    {
        DayCycle dayCycle = FindFirstObjectByType<DayCycle>(FindObjectsInactive.Include);

        if (dayCycle == null)
        {
            ShowCheatFeedback("Cheat: DayCycle nao encontrado.");
            return;
        }

        dayCycle.DebugAdvanceDay();
        ShowCheatFeedback($"Cheat: dia avancado -> {dayCycle.ElapsedDays}");
    }

    private void CompleteTask()
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null || !campaignProgress.IsCampaignQuestRunning)
        {
            ShowCheatFeedback("Cheat: nenhuma task de campanha ativa.");
            return;
        }

        string questName = campaignProgress.CurrentQuestName;
        bool completed = true;

        if (campaignProgress.CurrentQuestRequiresSpecialDelivery)
            completed = campaignProgress.CompleteSpecialDeliveryQuest();
        else
            campaignProgress.AdvanceQuest(0);

        ShowCheatFeedback(completed
            ? $"Cheat: task concluida -> {questName}"
            : "Cheat: nao foi possivel concluir a task atual.");
    }

    private void ResetSave()
    {
        GameSaveManager saveManager = GameSaveManager.GetOrCreate();

        if (saveManager == null)
        {
            ShowCheatFeedback("Cheat: GameSaveManager nao encontrado.");
            return;
        }

        saveManager.DeleteSave();

        if (_resetCampaignStateOnSaveReset)
            CampaignProgressSystem.GetOrCreate()?.StartNewCampaign();

        ShowCheatFeedback("Cheat: save resetado.");
    }

    private void ShowCheatFeedback(string _message)
    {
        Debug.Log(_message);

        if (HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(_message);
    }
}
