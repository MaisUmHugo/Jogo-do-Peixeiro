using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BuildCheatController : MonoBehaviour
{
    private const string CheatObjectName = "BuildCheatController";
    private const string MainMenuSceneName = "Main Menu";

    private static BuildCheatController instance;

    [Header("Settings")]
    [SerializeField] private bool _enableCheats = true;
    [SerializeField] private bool _requireModifier = true;
    [SerializeField] private bool _resetCampaignStateOnSaveReset = true;
    [SerializeField, Min(1f)] private float _moneyCheatAmount = 1000f;

    [Header("Keyboard Bindings")]
    [SerializeField] private string _cycleFishingAreaKeyboardBinding = "<Keyboard>/f1";
    [SerializeField] private string _advanceDayKeyboardBinding = "<Keyboard>/f2";
    [SerializeField] private string _completeTaskKeyboardBinding = "<Keyboard>/f3";
    [SerializeField] private string _resetSaveKeyboardBinding = "<Keyboard>/f4";
    [SerializeField] private string _addMoneyKeyboardBinding = "<Keyboard>/f5";
    [SerializeField] private string _playOpeningCutsceneKeyboardBinding = "<Keyboard>/f6";
    [SerializeField] private string _playMoneyLenderCutsceneKeyboardBinding = "<Keyboard>/f7";
    [SerializeField] private string _playFinalCutsceneKeyboardBinding = "<Keyboard>/f8";
    [SerializeField] private string _completeCampaignKeyboardBinding = "<Keyboard>/f9";
    [SerializeField] private string _forceEndlessSpecialDeliveryKeyboardBinding = "<Keyboard>/f10";
    [SerializeField] private string _unlockEndlessModeKeyboardBinding = "<Keyboard>/f11";
    [SerializeField] private string _disableTutorialKeyboardBinding = "<Keyboard>/f12";

    [Header("Shift Keyboard Bindings")]
    [SerializeField] private string _forceQuestFailureKeyboardBinding = "<Keyboard>/f1";
    [SerializeField] private string _addSpecialDeliveryFishKeyboardBinding = "<Keyboard>/f2";
    [SerializeField] private string _teleportToBoatKeyboardBinding = "<Keyboard>/f3";
    [SerializeField] private string _teleportToDockKeyboardBinding = "<Keyboard>/f4";
    [SerializeField] private string _spawnFishPullVfxKeyboardBinding = "<Keyboard>/f5";
    [SerializeField] private string _teleportToArrivalPointKeyboardBinding = "<Keyboard>/f6";
    [SerializeField] private string _resetTutorialSlidesKeyboardBinding = "<Keyboard>/f7";

    [Header("Teleport")]
    [SerializeField, Min(0f)] private float _teleportHeightOffset = 0.05f;
    [SerializeField] private string _debugArrivalPointId;

    [Header("VFX Debug")]
    [SerializeField, Min(0f)] private float _fishPullVfxSpawnDistance = 5f;

    [Header("Gamepad Bindings")]
    [SerializeField] private string _cycleFishingAreaGamepadBinding = "<Gamepad>/dpad/up";
    [SerializeField] private string _advanceDayGamepadBinding = "<Gamepad>/dpad/right";
    [SerializeField] private string _completeTaskGamepadBinding = "<Gamepad>/dpad/down";
    [SerializeField] private string _resetSaveGamepadBinding = "<Gamepad>/dpad/left";
    [SerializeField] private string _addMoneyGamepadBinding;
    [SerializeField] private string _completeCampaignGamepadBinding;
    [SerializeField] private string _forceEndlessSpecialDeliveryGamepadBinding;
    [SerializeField] private string _unlockEndlessModeGamepadBinding;
    [SerializeField] private string _disableTutorialGamepadBinding;

    private InputAction _cycleFishingAreaAction;
    private InputAction _advanceDayAction;
    private InputAction _completeTaskAction;
    private InputAction _resetSaveAction;
    private InputAction _addMoneyAction;
    private InputAction _playOpeningCutsceneAction;
    private InputAction _playMoneyLenderCutsceneAction;
    private InputAction _playFinalCutsceneAction;
    private InputAction _completeCampaignAction;
    private InputAction _forceEndlessSpecialDeliveryAction;
    private InputAction _unlockEndlessModeAction;
    private InputAction _disableTutorialAction;
    private InputAction _forceQuestFailureAction;
    private InputAction _addSpecialDeliveryFishAction;
    private InputAction _teleportToBoatAction;
    private InputAction _teleportToDockAction;
    private InputAction _spawnFishPullVfxAction;
    private InputAction _teleportToArrivalPointAction;
    private InputAction _resetTutorialSlidesAction;

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

        _addMoneyAction = CreateButtonAction(
            "Cheat Add Money",
            _addMoneyKeyboardBinding,
            _addMoneyGamepadBinding);

        _playOpeningCutsceneAction = CreateButtonAction(
            "Cheat Play Opening Cutscene",
            _playOpeningCutsceneKeyboardBinding,
            null);

        _playMoneyLenderCutsceneAction = CreateButtonAction(
            "Cheat Play Money Lender Cutscene",
            _playMoneyLenderCutsceneKeyboardBinding,
            null);

        _playFinalCutsceneAction = CreateButtonAction(
            "Cheat Play Final Cutscene",
            _playFinalCutsceneKeyboardBinding,
            null);

        _completeCampaignAction = CreateButtonAction(
            "Cheat Complete Campaign",
            _completeCampaignKeyboardBinding,
            _completeCampaignGamepadBinding);

        _forceEndlessSpecialDeliveryAction = CreateButtonAction(
            "Cheat Force Endless Special Delivery",
            _forceEndlessSpecialDeliveryKeyboardBinding,
            _forceEndlessSpecialDeliveryGamepadBinding);

        _unlockEndlessModeAction = CreateButtonAction(
            "Cheat Unlock Endless Mode",
            _unlockEndlessModeKeyboardBinding,
            _unlockEndlessModeGamepadBinding);

        _disableTutorialAction = CreateButtonAction(
            "Cheat Disable Tutorial",
            _disableTutorialKeyboardBinding,
            _disableTutorialGamepadBinding);

        _forceQuestFailureAction = CreateButtonAction(
            "Cheat Force Quest Failure",
            _forceQuestFailureKeyboardBinding,
            null);

        _addSpecialDeliveryFishAction = CreateButtonAction(
            "Cheat Add Special Delivery Fish",
            _addSpecialDeliveryFishKeyboardBinding,
            null);

        _teleportToBoatAction = CreateButtonAction(
            "Cheat Teleport To Boat",
            _teleportToBoatKeyboardBinding,
            null);

        _teleportToDockAction = CreateButtonAction(
            "Cheat Teleport To Dock",
            _teleportToDockKeyboardBinding,
            null);

        _spawnFishPullVfxAction = CreateButtonAction(
            "Cheat Spawn Fish Pull VFX",
            _spawnFishPullVfxKeyboardBinding,
            null);

        _teleportToArrivalPointAction = CreateButtonAction(
            "Cheat Teleport To Arrival Point",
            _teleportToArrivalPointKeyboardBinding,
            null);

        _resetTutorialSlidesAction = CreateButtonAction(
            "Cheat Reset Tutorial Slides",
            _resetTutorialSlidesKeyboardBinding,
            null);
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

        if (_addMoneyAction != null)
            _addMoneyAction.performed += HandleAddMoney;

        if (_playOpeningCutsceneAction != null)
            _playOpeningCutsceneAction.performed += HandlePlayOpeningCutscene;

        if (_playMoneyLenderCutsceneAction != null)
            _playMoneyLenderCutsceneAction.performed += HandlePlayMoneyLenderCutscene;

        if (_playFinalCutsceneAction != null)
            _playFinalCutsceneAction.performed += HandlePlayFinalCutscene;

        if (_completeCampaignAction != null)
            _completeCampaignAction.performed += HandleCompleteCampaign;

        if (_forceEndlessSpecialDeliveryAction != null)
            _forceEndlessSpecialDeliveryAction.performed += HandleForceEndlessSpecialDelivery;

        if (_unlockEndlessModeAction != null)
            _unlockEndlessModeAction.performed += HandleUnlockEndlessMode;

        if (_disableTutorialAction != null)
            _disableTutorialAction.performed += HandleDisableTutorial;

        if (_forceQuestFailureAction != null)
            _forceQuestFailureAction.performed += HandleForceQuestFailure;

        if (_addSpecialDeliveryFishAction != null)
            _addSpecialDeliveryFishAction.performed += HandleAddSpecialDeliveryFish;

        if (_teleportToBoatAction != null)
            _teleportToBoatAction.performed += HandleTeleportToBoat;

        if (_teleportToDockAction != null)
            _teleportToDockAction.performed += HandleTeleportToDock;

        if (_spawnFishPullVfxAction != null)
            _spawnFishPullVfxAction.performed += HandleSpawnFishPullVfx;

        if (_teleportToArrivalPointAction != null)
            _teleportToArrivalPointAction.performed += HandleTeleportToArrivalPoint;

        if (_resetTutorialSlidesAction != null)
            _resetTutorialSlidesAction.performed += HandleResetTutorialSlides;
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

        if (_addMoneyAction != null)
            _addMoneyAction.performed -= HandleAddMoney;

        if (_playOpeningCutsceneAction != null)
            _playOpeningCutsceneAction.performed -= HandlePlayOpeningCutscene;

        if (_playMoneyLenderCutsceneAction != null)
            _playMoneyLenderCutsceneAction.performed -= HandlePlayMoneyLenderCutscene;

        if (_playFinalCutsceneAction != null)
            _playFinalCutsceneAction.performed -= HandlePlayFinalCutscene;

        if (_completeCampaignAction != null)
            _completeCampaignAction.performed -= HandleCompleteCampaign;

        if (_forceEndlessSpecialDeliveryAction != null)
            _forceEndlessSpecialDeliveryAction.performed -= HandleForceEndlessSpecialDelivery;

        if (_unlockEndlessModeAction != null)
            _unlockEndlessModeAction.performed -= HandleUnlockEndlessMode;

        if (_disableTutorialAction != null)
            _disableTutorialAction.performed -= HandleDisableTutorial;

        if (_forceQuestFailureAction != null)
            _forceQuestFailureAction.performed -= HandleForceQuestFailure;

        if (_addSpecialDeliveryFishAction != null)
            _addSpecialDeliveryFishAction.performed -= HandleAddSpecialDeliveryFish;

        if (_teleportToBoatAction != null)
            _teleportToBoatAction.performed -= HandleTeleportToBoat;

        if (_teleportToDockAction != null)
            _teleportToDockAction.performed -= HandleTeleportToDock;

        if (_spawnFishPullVfxAction != null)
            _spawnFishPullVfxAction.performed -= HandleSpawnFishPullVfx;

        if (_teleportToArrivalPointAction != null)
            _teleportToArrivalPointAction.performed -= HandleTeleportToArrivalPoint;

        if (_resetTutorialSlidesAction != null)
            _resetTutorialSlidesAction.performed -= HandleResetTutorialSlides;
    }

    private void EnableActions()
    {
        _cycleFishingAreaAction?.Enable();
        _advanceDayAction?.Enable();
        _completeTaskAction?.Enable();
        _resetSaveAction?.Enable();
        _addMoneyAction?.Enable();
        _playOpeningCutsceneAction?.Enable();
        _playMoneyLenderCutsceneAction?.Enable();
        _playFinalCutsceneAction?.Enable();
        _completeCampaignAction?.Enable();
        _forceEndlessSpecialDeliveryAction?.Enable();
        _unlockEndlessModeAction?.Enable();
        _disableTutorialAction?.Enable();
        _forceQuestFailureAction?.Enable();
        _addSpecialDeliveryFishAction?.Enable();
        _teleportToBoatAction?.Enable();
        _teleportToDockAction?.Enable();
        _spawnFishPullVfxAction?.Enable();
        _teleportToArrivalPointAction?.Enable();
        _resetTutorialSlidesAction?.Enable();
    }

    private void DisposeActions()
    {
        DisposeAction(ref _cycleFishingAreaAction);
        DisposeAction(ref _advanceDayAction);
        DisposeAction(ref _completeTaskAction);
        DisposeAction(ref _resetSaveAction);
        DisposeAction(ref _addMoneyAction);
        DisposeAction(ref _playOpeningCutsceneAction);
        DisposeAction(ref _playMoneyLenderCutsceneAction);
        DisposeAction(ref _playFinalCutsceneAction);
        DisposeAction(ref _completeCampaignAction);
        DisposeAction(ref _forceEndlessSpecialDeliveryAction);
        DisposeAction(ref _unlockEndlessModeAction);
        DisposeAction(ref _disableTutorialAction);
        DisposeAction(ref _forceQuestFailureAction);
        DisposeAction(ref _addSpecialDeliveryFishAction);
        DisposeAction(ref _teleportToBoatAction);
        DisposeAction(ref _teleportToDockAction);
        DisposeAction(ref _spawnFishPullVfxAction);
        DisposeAction(ref _teleportToArrivalPointAction);
        DisposeAction(ref _resetTutorialSlidesAction);
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
        TryRunCheat(ResetSave, true);
    }

    private void HandleAddMoney(InputAction.CallbackContext _context)
    {
        TryRunCheat(AddMoney);
    }

    private void HandlePlayOpeningCutscene(InputAction.CallbackContext _context)
    {
        TryRunCheat(PlayOpeningCutsceneCheat);
    }

    private void HandlePlayMoneyLenderCutscene(InputAction.CallbackContext _context)
    {
        TryRunCheat(PlayMoneyLenderCutsceneCheat);
    }

    private void HandlePlayFinalCutscene(InputAction.CallbackContext _context)
    {
        TryRunCheat(PlayFinalCutsceneCheat);
    }

    private void HandleCompleteCampaign(InputAction.CallbackContext _context)
    {
        TryRunCheat(CompleteCampaign);
    }

    private void HandleForceEndlessSpecialDelivery(InputAction.CallbackContext _context)
    {
        TryRunCheat(ForceEndlessSpecialDelivery);
    }

    private void HandleUnlockEndlessMode(InputAction.CallbackContext _context)
    {
        TryRunCheat(UnlockEndlessMode, true);
    }

    private void HandleDisableTutorial(InputAction.CallbackContext _context)
    {
        TryRunCheat(DisableTutorial);
    }

    private void HandleForceQuestFailure(InputAction.CallbackContext _context)
    {
        TryRunShiftCheat(ForceQuestFailure);
    }

    private void HandleAddSpecialDeliveryFish(InputAction.CallbackContext _context)
    {
        TryRunShiftCheat(AddSpecialDeliveryFish);
    }

    private void HandleTeleportToBoat(InputAction.CallbackContext _context)
    {
        TryRunShiftCheat(TeleportToBoatCheat);
    }

    private void HandleTeleportToDock(InputAction.CallbackContext _context)
    {
        TryRunShiftCheat(TeleportToDockCheat);
    }

    private void HandleSpawnFishPullVfx(InputAction.CallbackContext _context)
    {
        TryRunShiftCheat(SpawnFishPullVfxCheat);
    }

    private void HandleTeleportToArrivalPoint(InputAction.CallbackContext _context)
    {
        TryRunShiftCheat(TeleportToArrivalPointCheat);
    }

    private void HandleResetTutorialSlides(InputAction.CallbackContext _context)
    {
        TryRunShiftCheat(ResetTutorialSlidesCheat);
    }

    private void TryRunCheat(System.Action _cheatAction, bool _allowInMainMenu = false)
    {
        if (!_enableCheats || !IsModifierPressed() || !CanRunInCurrentScene(_allowInMainMenu))
            return;

        _cheatAction?.Invoke();
    }

    private void TryRunShiftCheat(System.Action _cheatAction)
    {
        if (!_enableCheats || !IsShiftModifierPressed() || !CanRunInCurrentScene(false))
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

    private bool IsShiftModifierPressed()
    {
        if (!_requireModifier)
            return true;

        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }

    private bool CanRunInCurrentScene(bool _allowInMainMenu)
    {
        if (!IsMainMenuScene())
            return true;

        if (_allowInMainMenu)
            return true;

        ShowCheatFeedback("Cheat indisponivel no menu.");
        return false;
    }

    private bool IsMainMenuScene()
    {
        return SceneManager.GetActiveScene().name == MainMenuSceneName;
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

        ShowCheatFeedback($"Cheat: área de pesca -> {activeAreaName}");
    }

    private void AdvanceDay()
    {
        DayCycle dayCycle = FindFirstObjectByType<DayCycle>(FindObjectsInactive.Include);

        if (dayCycle == null)
        {
            ShowCheatFeedback("Cheat: DayCycle não encontrado.");
            return;
        }

        dayCycle.DebugAdvanceDay();
        ShowCheatFeedback($"Cheat: dia avançado -> {dayCycle.ElapsedDays}");
    }

    private void CompleteTask()
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null || !campaignProgress.IsDebtQuestRunning)
        {
            ShowCheatFeedback("Cheat: nenhuma quest ativa.");
            return;
        }

        string questName = campaignProgress.CurrentQuestName;
        bool completed = true;

        if (campaignProgress.CurrentQuestRequiresSpecialDelivery)
            completed = campaignProgress.CompleteSpecialDeliveryQuest();
        else
            campaignProgress.AdvanceQuest(0);

        ShowCheatFeedback(completed
            ? $"Cheat: quest concluída -> {questName}"
            : "Cheat: não foi possível concluir a quest atual.");
    }

    private void AddMoney()
    {
        PlayerMoneyManager moneyManager = FindFirstObjectByType<PlayerMoneyManager>(FindObjectsInactive.Include);

        if (moneyManager == null)
        {
            ShowCheatFeedback("Cheat: PlayerMoneyManager não encontrado.");
            return;
        }

        moneyManager.ReceiveMoney(_moneyCheatAmount);
        SaveGameIfPossible();
        ShowCheatFeedback($"Cheat: +{_moneyCheatAmount:0} moedas.");
    }

    private void PlayOpeningCutsceneCheat()
    {
        PlayCutsceneCheat(
            _controller => _controller.ForcePlayOpeningCutscene(),
            _library => new[] { _library.IntroMarinaLoja },
            "inicial da loja");
    }

    private void PlayMoneyLenderCutsceneCheat()
    {
        PlayCutsceneCheat(
            _controller => _controller.ForcePlayMoneyLenderIntroCutscene(),
            _library => new[] { _library.IntroCobradorCabana },
            "cobrador");
    }

    private void PlayFinalCutsceneCheat()
    {
        CampaignOutcomeController outcomeController = FindFirstObjectByType<CampaignOutcomeController>(
            FindObjectsInactive.Include);

        if (outcomeController != null && outcomeController.DebugPlayCampaignCompletionFlow(false))
        {
            ShowCheatFeedback("Cheat: final da campanha com transicao iniciado.");
            return;
        }

        PlayCutsceneCheat(
            _controller => _controller.ForcePlayFinalCutscene(),
            _library => new[] { _library.FimCampanhaLoja, _library.FimCampanhaAirFishers },
            "final");
    }

    private void ResetTutorialSlidesCheat()
    {
        CampaignQuestGuidanceController.ClearTutorialSlidesCompletedFlag();

        CampaignQuestGuidanceController guidanceController =
            FindFirstObjectByType<CampaignQuestGuidanceController>(FindObjectsInactive.Include);

        if (guidanceController != null && guidanceController.DebugRestoreTutorialGuidance())
        {
            ShowCheatFeedback("Cheat: tutorial visual reativado.");
            return;
        }

        ShowCheatFeedback("Cheat: slides do tutorial liberados novamente.");
    }

    private void PlayCutsceneCheat(
        System.Func<CampaignCutsceneController, bool> _playCutscene,
        System.Func<RoteiroDialogLibrary, DialogSequenceAsset[]> _playDialogFallback,
        string _label)
    {
        CampaignCutsceneController cutsceneController = FindFirstObjectByType<CampaignCutsceneController>(
            FindObjectsInactive.Include);

        if (cutsceneController != null)
        {
            if (cutsceneController.IsPlaying)
            {
                ShowCheatFeedback("Cheat: ja existe uma cutscene tocando.");
                return;
            }

            if (_playCutscene != null && _playCutscene.Invoke(cutsceneController))
            {
                ShowCheatFeedback($"Cheat: cutscene {_label} iniciada.");
                return;
            }
        }

        if (RoteiroDialogPlayback.TryPlayFromLibrary(null, _playDialogFallback, null))
        {
            ShowCheatFeedback($"Cheat: dialogo {_label} iniciado.");
            return;
        }

        ShowCheatFeedback($"Cheat: nao foi possivel tocar a cutscene/dialogo {_label}.");
    }

    private void SpawnFishPullVfxCheat()
    {
        FishingRod fishingRod = FindFirstObjectByType<FishingRod>(FindObjectsInactive.Include);

        if (fishingRod == null)
        {
            ShowCheatFeedback("Cheat: FishingRod não encontrado.");
            return;
        }

        if (!TryGetFishPullVfxSpawnPose(out Vector3 spawnPosition, out Vector3 spawnDirection))
        {
            ShowCheatFeedback("Cheat: sem referência para posicionar o Fish Pull VFX.");
            return;
        }

        if (!fishingRod.DebugSpawnFishPullVFXAtWater(spawnPosition, spawnDirection))
        {
            ShowCheatFeedback("Cheat: Fish Pull VFX não configurado no FishingRod.");
            return;
        }

        ShowCheatFeedback("Cheat: Fish Pull VFX spawnado na água.");
    }

    private bool TryGetFishPullVfxSpawnPose(out Vector3 _position, out Vector3 _direction)
    {
        _position = Vector3.zero;
        _direction = Vector3.forward;

        Transform reference = GetFishPullVfxReference();

        if (reference == null)
            return false;

        _direction = reference.forward;
        _direction.y = 0f;

        if (_direction.sqrMagnitude <= 0.0001f)
            _direction = Vector3.forward;

        _direction.Normalize();
        _position = reference.position + _direction * _fishPullVfxSpawnDistance;

        if (WaveManager.instance != null)
            _position.y = WaveManager.instance.GetWaveHeight(_position.x, _position.z);

        return true;
    }

    private Transform GetFishPullVfxReference()
    {
        BoatController boatController = FindFirstObjectByType<BoatController>(FindObjectsInactive.Include);

        if (boatController != null && boatController.IsPlayerOnBoat())
            return boatController.transform;

        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);

        if (playerMove != null)
            return playerMove.transform;

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform : null;
    }

    private void CompleteCampaign()
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null)
        {
            ShowCheatFeedback("Cheat: CampaignProgressSystem não encontrado.");
            return;
        }

        campaignProgress.DebugCompleteCampaign();
        SaveGameIfPossible();
        ShowCheatFeedback("Cheat: campanha concluída e modo sem fim liberado.");
    }

    private void ForceEndlessSpecialDelivery()
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null)
        {
            ShowCheatFeedback("Cheat: CampaignProgressSystem não encontrado.");
            return;
        }

        if (campaignProgress.GameMode != GameProgressMode.Endless)
            campaignProgress.StartUnlockedEndlessMode();

        if (!campaignProgress.DebugForceEndlessSpecialDelivery())
        {
            ShowCheatFeedback("Cheat: não foi possível gerar entrega especial sem peixe elegível.");
            return;
        }

        SaveGameIfPossible();

        string fishName = campaignProgress.SpecialDeliveryFish != null
            ? campaignProgress.SpecialDeliveryFish.fishName
            : "peixe especial";

        ShowCheatFeedback($"Cheat: entrega especial ativa -> {campaignProgress.SpecialDeliveryQuantity}x {fishName}.");
    }

    private void UnlockEndlessMode()
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null)
        {
            ShowCheatFeedback("Cheat: CampaignProgressSystem não encontrado.");
            return;
        }

        campaignProgress.DebugUnlockEndlessMode();
        SaveGameIfPossible();
        RefreshMainMenuIfPresent();
        ShowCheatFeedback("Cheat: modo sem fim liberado.");
    }

    private void DisableTutorial()
    {
        CampaignQuestGuidanceController tutorial = CampaignQuestGuidanceController.instance;

        if (tutorial == null)
            tutorial = FindFirstObjectByType<CampaignQuestGuidanceController>(FindObjectsInactive.Include);

        if (tutorial == null)
        {
            ShowCheatFeedback("Cheat: tutorial não encontrado nesta cena.");
            return;
        }

        tutorial.DebugDisableTutorial();
        ShowCheatFeedback("Cheat: tutorial desativado nesta cena.");
    }

    private void ForceQuestFailure()
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null || !campaignProgress.DebugExpireCurrentQuestDeadline())
        {
            ShowCheatFeedback("Cheat: nenhuma quest ativa para falhar.");
            return;
        }

        ShowCheatFeedback("Cheat: falha de prazo forçada.");
    }

    private void AddSpecialDeliveryFish()
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null ||
            !campaignProgress.CurrentQuestRequiresSpecialDelivery ||
            campaignProgress.SpecialDeliveryFish == null)
        {
            ShowCheatFeedback("Cheat: nenhuma entrega especial ativa.");
            return;
        }

        ShipInventory shipInventory = FindFirstObjectByType<ShipInventory>(FindObjectsInactive.Include);

        if (shipInventory == null)
        {
            ShowCheatFeedback("Cheat: ShipInventory não encontrado.");
            return;
        }

        FishScriptableObject requestedFish = campaignProgress.SpecialDeliveryFish;
        int requiredQuantity = Mathf.Max(1, campaignProgress.SpecialDeliveryQuantity);
        int ownedQuantity = shipInventory.CountFish(requestedFish);
        int missingQuantity = Mathf.Max(0, requiredQuantity - ownedQuantity);

        if (missingQuantity <= 0)
        {
            ShowCheatFeedback($"Cheat: entrega especial já possui {ownedQuantity}/{requiredQuantity} {GetFishDisplayName(requestedFish)}.");
            return;
        }

        List<FishData> fishList = new List<FishData>(shipInventory.OwnedFish);
        int fishWeight = Mathf.Max(requestedFish.minWeight, requestedFish.maxWeight);

        for (int i = 0; i < missingQuantity; i++)
            fishList.Add(new FishData(requestedFish, fishWeight));

        shipInventory.ReplaceFish(fishList);
        SaveGameIfPossible();
        ShowCheatFeedback($"Cheat: +{missingQuantity}x {GetFishDisplayName(requestedFish)} para entrega especial.");
    }

    private void TeleportToBoatCheat()
    {
        TeleportToBoat();
    }

    private void TeleportToDockCheat()
    {
        TeleportToDock();
    }

    private void TeleportToArrivalPointCheat()
    {
        SceneTransitionArrivalPoint arrivalPoint = ResolveArrivalPoint(_debugArrivalPointId);

        if (arrivalPoint == null)
        {
            ShowCheatFeedback(GetMissingArrivalPointFeedback());
            return;
        }

        if (!arrivalPoint.TryApplyArrival(false))
        {
            ShowCheatFeedback($"Cheat: não foi possível aplicar Arrival Point '{arrivalPoint.ArrivalPointId}'.");
            return;
        }

        ShowCheatFeedback($"Cheat: teleporte -> Arrival Point '{arrivalPoint.ArrivalPointId}'.");
    }

    private bool TeleportToBoat()
    {
        BoatController boat = FindFirstObjectByType<BoatController>(FindObjectsInactive.Include);

        if (boat == null)
        {
            ShowCheatFeedback("Cheat: BoatController não encontrado.");
            return false;
        }

        if (GameManager.instance == null)
        {
            ShowCheatFeedback("Cheat: GameManager não encontrado.");
            return false;
        }

        if (!boat.IsPlayerOnBoat())
        {
            SetGameplayState(GameManager.GameState.OnFoot);
            boat.EnterBoat();
        }

        if (!boat.IsPlayerOnBoat())
        {
            ShowCheatFeedback("Cheat: não foi possível entrar no barco.");
            return false;
        }

        SetGameplayState(GameManager.GameState.OnBoat);
        ShowCheatFeedback("Cheat: teleporte -> barco.");
        return true;
    }

    private bool TeleportToDock()
    {
        Dock dock = ResolveDock();

        if (dock == null)
        {
            ShowCheatFeedback("Cheat: Dock não encontrado.");
            return false;
        }

        Transform exitPoint = dock.ExitPoint != null ? dock.ExitPoint : dock.BoatParkPoint;

        if (exitPoint == null)
        {
            ShowCheatFeedback("Cheat: dock sem exit point ou boat park point.");
            return false;
        }

        BoatController boat = FindFirstObjectByType<BoatController>(FindObjectsInactive.Include);

        if (boat != null && boat.IsPlayerOnBoat() && dock.BoatParkPoint != null)
        {
            boat.ParkBoatAndExit(dock.BoatParkPoint, dock.ExitPoint);
            ShowCheatFeedback("Cheat: teleporte -> doca.");
            return true;
        }

        if (boat != null && boat.IsPlayerOnBoat())
            boat.ExitBoatForTeleport();

        SetGameplayState(GameManager.GameState.OnFoot);

        if (!TryPlacePlayerAt(exitPoint.position + Vector3.up * _teleportHeightOffset, exitPoint.rotation, true))
            return false;

        ShowCheatFeedback("Cheat: teleporte -> doca.");
        return true;
    }

    private void ResetSave()
    {
        GameSaveManager saveManager = GameSaveManager.GetOrCreate();

        if (saveManager == null)
        {
            ShowCheatFeedback("Cheat: GameSaveManager não encontrado.");
            return;
        }

        saveManager.DeleteSave();

        if (_resetCampaignStateOnSaveReset)
            CampaignProgressSystem.GetOrCreate()?.StartNewCampaign();

        RefreshMainMenuIfPresent();
        ShowCheatFeedback("Cheat: save resetado.");
    }

    private void SaveGameIfPossible()
    {
        GameSaveManager.GetOrCreate()?.SaveGame();
    }

    private Dock ResolveDock()
    {
        Dock[] docks = FindObjectsByType<Dock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Dock fallbackDock = null;

        for (int i = 0; i < docks.Length; i++)
        {
            Dock dock = docks[i];

            if (dock == null)
                continue;

            fallbackDock ??= dock;

            if (dock.ExitPoint != null)
                return dock;
        }

        return fallbackDock;
    }

    private SceneTransitionArrivalPoint ResolveArrivalPoint(string _arrivalPointId)
    {
        SceneTransitionArrivalPoint[] arrivalPoints = FindObjectsByType<SceneTransitionArrivalPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (arrivalPoints == null || arrivalPoints.Length == 0)
            return null;

        SceneTransitionArrivalPoint fallback = null;

        for (int i = 0; i < arrivalPoints.Length; i++)
        {
            SceneTransitionArrivalPoint point = arrivalPoints[i];

            if (point == null)
                continue;

            fallback ??= point;

            if (!string.IsNullOrWhiteSpace(_arrivalPointId) && point.ArrivalPointId == _arrivalPointId)
                return point;
        }

        return string.IsNullOrWhiteSpace(_arrivalPointId) ? fallback : null;
    }

    private string GetMissingArrivalPointFeedback()
    {
        SceneTransitionArrivalPoint[] arrivalPoints = FindObjectsByType<SceneTransitionArrivalPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (arrivalPoints == null || arrivalPoints.Length == 0)
            return "Cheat: nenhum Arrival Point encontrado na cena.";

        List<string> ids = new List<string>();

        for (int i = 0; i < arrivalPoints.Length; i++)
        {
            if (arrivalPoints[i] == null)
                continue;

            ids.Add(string.IsNullOrWhiteSpace(arrivalPoints[i].ArrivalPointId)
                ? "(sem id)"
                : arrivalPoints[i].ArrivalPointId);
        }

        string requestedId = string.IsNullOrWhiteSpace(_debugArrivalPointId) ? "(primeiro)" : _debugArrivalPointId;
        return $"Cheat: Arrival Point '{requestedId}' não encontrado. Disponíveis: {string.Join(", ", ids)}.";
    }

    private Transform ResolvePlayerTransform()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (playerController != null)
            return playerController.transform;

        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);
        return playerMove != null ? playerMove.transform : null;
    }

    private void SetGameplayState(GameManager.GameState _state)
    {
        Time.timeScale = 1f;

        if (GameManager.instance != null)
            GameManager.instance.SetState(_state);
    }

    private bool TryPlacePlayerAt(Vector3 _position, Quaternion _rotation, bool _enablePlayerControl)
    {
        Transform player = ResolvePlayerTransform();

        if (player == null)
        {
            ShowCheatFeedback("Cheat: player não encontrado.");
            return false;
        }

        CharacterController characterController = player.GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = false;

        player.SetPositionAndRotation(_position, _rotation);
        ResetAttachedRigidbodies(player);

        PlayerController playerController = player.GetComponent<PlayerController>();

        if (playerController != null)
            playerController.enabled = _enablePlayerControl;

        if (characterController != null)
            characterController.enabled = _enablePlayerControl;

        PlayerMove playerMove = player.GetComponent<PlayerMove>();

        if (playerMove != null)
        {
            playerMove.ResetMovementState();
            playerMove.SetSafeRespawnPosition(player.position);
        }

        PlayerAnimationController playerAnimationController = player.GetComponentInChildren<PlayerAnimationController>(true);

        if (playerAnimationController != null)
        {
            playerAnimationController.ResetFishingAnimationState();
            playerAnimationController.ResetFishingVisualOffset();
            playerAnimationController.ResetBoatVisualOffset();
        }

        Physics.SyncTransforms();
        return true;
    }

    private void ResetAttachedRigidbodies(Transform _root)
    {
        if (_root == null)
            return;

        Rigidbody[] rigidbodies = _root.GetComponentsInChildren<Rigidbody>();

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody body = rigidbodies[i];

            if (body == null)
                continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void RefreshMainMenuIfPresent()
    {
        MainMenuManager mainMenuManager = FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        mainMenuManager?.RefreshModeAvailabilityForDebug();
    }

    private string GetFishDisplayName(FishScriptableObject _fish)
    {
        if (_fish == null)
            return "peixe";

        return string.IsNullOrWhiteSpace(_fish.fishName) ? _fish.name : _fish.fishName;
    }

    private void ShowCheatFeedback(string _message)
    {
        Debug.Log(_message);

        if (HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(_message);
    }
}
