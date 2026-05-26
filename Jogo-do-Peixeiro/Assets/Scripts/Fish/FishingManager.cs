using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class FishingManager : MonoBehaviour
{
    #region Fields And Types

    public static FishingManager instance;

    [Header("UI")]
    [FormerlySerializedAs("fishingResultPanelUI")]
    [FormerlySerializedAs("_fishResultPanelUI")]
    [SerializeField] private FishResultUI _fishResultUI;

    [Header("Fishing Visibility")]
    [FormerlySerializedAs("_panelsToCloseOnFishingStart")]
    [SerializeField] private GameObject[] _panelsToHideWhileFishing;
    [SerializeField] private bool _restoreHiddenPanelsAfterFishing = true;
    [SerializeField] private bool _hidePanelsWithCanvasGroup = true;
    [SerializeField] private bool _closeInventoryOnFishingStart = true;

    [Header("Fishing Settings")]
    [FormerlySerializedAs("useSkillCheck")]
    [SerializeField] private bool _useSkillCheck = true;
    [SerializeField] private float _baseProgressSpeed;
    [SerializeField] private bool _requireSkillCheckToFinish = true;
    [SerializeField, Range(0f, 1f)] private float _skillCheckFinishGateProgress = 0.8f;

    [Header("Rarity Progress Multipliers")]
    [SerializeField] private float _rarity1ProgressMultiplier = 1f;
    [SerializeField] private float _rarity2ProgressMultiplier = 0.85f;
    [SerializeField] private float _rarity3ProgressMultiplier = 0.7f;

    [Header("Fish Selection")]
    [SerializeField] private bool _avoidRepeatingSameFish = true;
    [SerializeField, Min(1)] private int _maxSameFishInARow = 2;
    [SerializeField] private bool _boostObjectiveFishChance = true;
    [SerializeField] private bool _onlyBoostObjectiveFishWhileStillNeeded = true;
    [SerializeField, Range(0f, 1f)] private float _objectiveFishBaseChance = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _objectiveFishChanceIncreasePerMiss = 0.15f;
    [SerializeField, Min(0)] private int _objectiveFishGuaranteedAfterMisses = 4;

    [Header("References")]
    [FormerlySerializedAs("fishSkillCheck")]
    [SerializeField] private FishSkillCheck _fishSkillCheck;
    [FormerlySerializedAs("fishBiteHandler")]
    [SerializeField] private FishBiteHandler _fishBiteHandler;
    [FormerlySerializedAs("fishDirectionPull")]
    [SerializeField] private FishDirectionPull _fishDirectionPull;
    [FormerlySerializedAs("fishingRod")]
    [SerializeField] private FishingRod _fishingRod;
    [SerializeField] private BaitInventory _baitInventory;

    public bool IsFishing { get; private set; }
    public bool HasFishBitten { get; private set; }
    public float ProgressNormalized { get; private set; }
    public event System.Action<bool, bool> FishingEnded;

    private ShipInventory _currentShipInventory;
    private FishScriptableObject[] _currentAvailableFish;
    private FishScriptableObject _selectedFishType;
    private FishData _pendingFish;
    private FishScriptableObject _lastBittenFishType;
    private BaitData _activeBait;
    private int _sameFishBiteStreak;
    private int _objectiveFishMissStreak;
    private HiddenPanelState[] _hiddenPanelStates;
    private bool _waitForCatchResultClose;

    private struct HiddenPanelState
    {
        public GameObject Panel;
        public CanvasGroup CanvasGroup;
        public bool WasActiveSelf;
        public bool WasHiddenWithCanvasGroup;
        public float OriginalAlpha;
        public bool OriginalInteractable;
        public bool OriginalBlocksRaycasts;
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        AutoAssignMissingReferences();
    }

    private void OnEnable()
    {
        AutoAssignMissingReferences();
    }

    private void OnDisable()
    {
        UnsubscribeCatchResultPanel();
        RestoreConfiguredPanelsAfterFishing();
    }

    private void OnValidate()
    {
        _maxSameFishInARow = Mathf.Max(1, _maxSameFishInARow);
        _objectiveFishBaseChance = Mathf.Clamp01(_objectiveFishBaseChance);
        _objectiveFishChanceIncreasePerMiss = Mathf.Clamp01(_objectiveFishChanceIncreasePerMiss);
        _objectiveFishGuaranteedAfterMisses = Mathf.Max(0, _objectiveFishGuaranteedAfterMisses);
    }

    private void AutoAssignMissingReferences()
    {
        if (_fishSkillCheck == null)
            _fishSkillCheck = FindFirstObjectByType<FishSkillCheck>(FindObjectsInactive.Include);

        if (_fishBiteHandler == null)
            _fishBiteHandler = FindFirstObjectByType<FishBiteHandler>(FindObjectsInactive.Include);

        if (_fishDirectionPull == null)
            _fishDirectionPull = FindFirstObjectByType<FishDirectionPull>(FindObjectsInactive.Include);

        if (_fishingRod == null)
            _fishingRod = FindFirstObjectByType<FishingRod>(FindObjectsInactive.Include);

        if (_fishResultUI == null)
            _fishResultUI = FindFirstObjectByType<FishResultUI>(FindObjectsInactive.Include);

        if (_baitInventory == null)
            _baitInventory = FindFirstObjectByType<BaitInventory>(FindObjectsInactive.Include);
    }

    #endregion

    #region Fishing Flow

    private void Update()
    {
        if (!IsFishing || !HasFishBitten)
            return;

        UpdateFishingProgress();
    }

    public bool StartFishing(ShipInventory _shipInventory, FishScriptableObject[] _availableFish)
    {
        if (IsFishing)
            return false;

        if (_shipInventory == null)
        {
            ReturnToBoatState();
            return false;
        }

        if (_availableFish == null || _availableFish.Length == 0)
        {
            Debug.LogWarning("Nenhum peixe configurado nesse spot.");
            ReturnToBoatState();
            return false;
        }

        if (_shipInventory.IsFull)
        {
            Debug.Log("Inventário do barco cheio.");

            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning("Inventário cheio");

            ReturnToBoatState();
            return false;
        }

        _currentShipInventory = _shipInventory;
        _currentAvailableFish = _availableFish;
        _waitForCatchResultClose = false;
        _activeBait = GetUsableEquippedBait();

        _selectedFishType = PickRandomFishType();

        if (_selectedFishType == null)
        {
            Debug.LogWarning("Falha ao selecionar um peixe.");
            ClearFishingData();
            ReturnToBoatState();
            return false;
        }

        _pendingFish = new FishData(_selectedFishType);

        IsFishing = true;
        HasFishBitten = false;
        ProgressNormalized = 0f;

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.Fishing);

        HideConfiguredPanelsForFishing();
        StartBiteWaiting();
        return true;
    }

    public void CancelFishing()
    {
        if (!IsFishing)
            return;

        EndFishing(false);
    }

    private void HideConfiguredPanelsForFishing()
    {
        if (_closeInventoryOnFishingStart)
            InvertoryManager.TryCloseOpenInventory();

        if (_panelsToHideWhileFishing == null || _panelsToHideWhileFishing.Length == 0)
            return;

        _hiddenPanelStates = new HiddenPanelState[_panelsToHideWhileFishing.Length];

        for (int i = 0; i < _panelsToHideWhileFishing.Length; i++)
        {
            GameObject panel = _panelsToHideWhileFishing[i];

            if (panel == null)
                continue;

            HiddenPanelState state = new HiddenPanelState
            {
                Panel = panel,
                WasActiveSelf = panel.activeSelf
            };

            if (_hidePanelsWithCanvasGroup && _restoreHiddenPanelsAfterFishing && panel.activeInHierarchy)
            {
                CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();

                if (canvasGroup == null)
                    canvasGroup = panel.AddComponent<CanvasGroup>();

                state.CanvasGroup = canvasGroup;
                state.WasHiddenWithCanvasGroup = true;
                state.OriginalAlpha = canvasGroup.alpha;
                state.OriginalInteractable = canvasGroup.interactable;
                state.OriginalBlocksRaycasts = canvasGroup.blocksRaycasts;

                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                panel.SetActive(false);
            }

            _hiddenPanelStates[i] = state;
        }
    }

    private void RestoreConfiguredPanelsAfterFishing()
    {
        if (_hiddenPanelStates == null)
            return;

        if (!_restoreHiddenPanelsAfterFishing)
        {
            _hiddenPanelStates = null;
            return;
        }

        for (int i = 0; i < _hiddenPanelStates.Length; i++)
        {
            HiddenPanelState state = _hiddenPanelStates[i];
            GameObject panel = state.Panel;

            if (panel == null)
                continue;

            if (state.WasHiddenWithCanvasGroup && state.CanvasGroup != null)
            {
                state.CanvasGroup.alpha = state.OriginalAlpha;
                state.CanvasGroup.interactable = state.OriginalInteractable;
                state.CanvasGroup.blocksRaycasts = state.OriginalBlocksRaycasts;
                continue;
            }

            panel.SetActive(state.WasActiveSelf);
        }

        _hiddenPanelStates = null;
    }

    #endregion

    #region Bite And Progress

    private void StartBiteWaiting()
    {
        if (_fishBiteHandler != null)
        {
            float delayMultiplier = _activeBait != null ? _activeBait.BiteDelayMultiplier : 1f;
            _fishBiteHandler.StartWaiting(OnFishBite, delayMultiplier);
            return;
        }

        OnFishBite();
    }

    private void OnFishBite()
    {
        if (!IsFishing)
            return;

        if (TutorialEvents.TryHandleFishingBiteTutorial(ContinueAfterBiteTutorial))
            return;

        ContinueAfterBiteTutorial();
    }

    private void ContinueAfterBiteTutorial()
    {
        if (!IsFishing)
            return;

        HasFishBitten = true;

        ConsumeActiveBait();

        RegisterBittenFish(_selectedFishType);

        if (_fishDirectionPull != null)
            _fishDirectionPull.StartPull(_selectedFishType, _activeBait);

        if (_useSkillCheck && _fishSkillCheck != null)
            _fishSkillCheck.StartSkillCheck(this, _selectedFishType, _activeBait);

        Debug.Log("Peixe mordeu a isca.");
    }

    private void UpdateFishingProgress()
    {
        float progressMultiplier = GetProgressMultiplierByFish();
        progressMultiplier *= _activeBait != null ? _activeBait.CatchProgressMultiplier : 1f;
        float progressDelta = _baseProgressSpeed * progressMultiplier;

        if (_fishDirectionPull != null)
        {
            Vector2 input = Vector2.zero;

            if (InputHandler.instance != null)
                input = InputHandler.instance.moveInput;

            progressDelta += _fishDirectionPull.GetProgressModifier(input, ProgressNormalized);
        }

        float nextProgress = ProgressNormalized + (progressDelta * Time.deltaTime);

        if (ShouldHoldForFinalSkillCheck(nextProgress))
            nextProgress = Mathf.Min(nextProgress, _skillCheckFinishGateProgress);

        ProgressNormalized = nextProgress;
        ProgressNormalized = Mathf.Clamp01(ProgressNormalized);

        if (CanCompleteFishing())
            CompleteFishing();
    }

    public void AddSkillCheckProgressBonus(float _bonus)
    {
        if (!IsFishing)
            return;

        ProgressNormalized += _bonus;

        ProgressNormalized = Mathf.Clamp01(ProgressNormalized);

        if (CanCompleteFishing())
            CompleteFishing();
    }

    public void ApplySkillCheckPenalty(float _penalty)
    {
        if (!IsFishing)
            return;

        ProgressNormalized -= _penalty;
        ProgressNormalized = Mathf.Clamp01(ProgressNormalized);
    }

    public void OnSkillCheckSuccessTick(FishSkillCheck.FeedbackResult _result)
    {
        if (!IsFishing)
            return;

        if (_fishingRod != null)
            _fishingRod.PlaySuccessSplash(_result);
    }

    public void OnSkillCheckFail()
    {
        if (!IsFishing)
            return;

        Debug.Log("Falhou na pescaria.");
        EndFishing(false);
    }

    #endregion

    #region Completion And Fish Selection

    private void CompleteFishing()
    {
        if (!IsFishing)
            return;

        ProgressNormalized = 1f;
        GivePendingFish();
        EndFishing(true);
    }

    private bool CanCompleteFishing()
    {
        return ProgressNormalized >= 1f;
    }

    private bool ShouldHoldForFinalSkillCheck(float _progressNormalized)
    {
        return _requireSkillCheckToFinish &&
               _useSkillCheck &&
               _fishSkillCheck != null &&
               _progressNormalized >= _skillCheckFinishGateProgress;
    }

    private bool IsSkillCheckActive()
    {
        return _fishSkillCheck != null && _fishSkillCheck.IsSkillCheckActive;
    }

    private FishScriptableObject PickRandomFishType()
    {
        if (_currentAvailableFish == null || _currentAvailableFish.Length == 0)
            return null;

        List<FishScriptableObject> candidates = BuildFishSelectionCandidates();

        if (candidates.Count == 0)
            return null;

        FishScriptableObject objectiveFish = GetCurrentObjectiveFish();

        if (objectiveFish != null &&
            ContainsFish(candidates, objectiveFish) &&
            ShouldPickObjectiveFish())
        {
            return objectiveFish;
        }

        return PickWeightedFish(candidates);
    }

    private List<FishScriptableObject> BuildFishSelectionCandidates()
    {
        List<FishScriptableObject> candidates = new List<FishScriptableObject>();

        for (int i = 0; i < _currentAvailableFish.Length; i++)
        {
            FishScriptableObject fishType = _currentAvailableFish[i];

            if (fishType != null && IsFishAvailableNow(fishType))
                candidates.Add(fishType);
        }

        if (!_avoidRepeatingSameFish ||
            candidates.Count <= 1 ||
            _lastBittenFishType == null ||
            _sameFishBiteStreak < _maxSameFishInARow)
        {
            return candidates;
        }

        bool hasAlternativeFish = false;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] != _lastBittenFishType)
            {
                hasAlternativeFish = true;
                break;
            }
        }

        if (!hasAlternativeFish)
            return candidates;

        candidates.RemoveAll(fishType => fishType == _lastBittenFishType);
        return candidates;
    }

    private bool ShouldPickObjectiveFish()
    {
        if (!_boostObjectiveFishChance)
            return false;

        if (_objectiveFishGuaranteedAfterMisses > 0 &&
            _objectiveFishMissStreak >= _objectiveFishGuaranteedAfterMisses)
        {
            return true;
        }

        float chance = _objectiveFishBaseChance +
                       _objectiveFishChanceIncreasePerMiss * _objectiveFishMissStreak;

        return Random.value <= Mathf.Clamp01(chance);
    }

    private FishScriptableObject PickWeightedFish(List<FishScriptableObject> _candidates)
    {
        if (_candidates == null || _candidates.Count == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < _candidates.Count; i++)
        {
            if (_candidates[i] != null)
                totalWeight += Mathf.Max(0f, _candidates[i].spawnWeight);
        }

        if (totalWeight <= 0f)
            return _candidates[Random.Range(0, _candidates.Count)];

        float randomWeight = Random.Range(0f, totalWeight);

        for (int i = 0; i < _candidates.Count; i++)
        {
            FishScriptableObject candidate = _candidates[i];

            if (candidate == null)
                continue;

            randomWeight -= Mathf.Max(0f, candidate.spawnWeight);

            if (randomWeight <= 0f)
                return candidate;
        }

        return _candidates[_candidates.Count - 1];
    }

    private bool IsFishAvailableNow(FishScriptableObject _fishType)
    {
        if (_fishType == null)
            return false;

        DayCycle dayCycle = FindFirstObjectByType<DayCycle>();

        if (dayCycle == null)
            return true;

        return _fishType.IsAvailableAtHour(dayCycle.NormalizedTime * 24f);
    }

    private FishScriptableObject GetCurrentObjectiveFish()
    {
        CampaignQuestGuidanceController tutorialController = CampaignQuestGuidanceController.instance;

        if (!_boostObjectiveFishChance ||
            tutorialController == null ||
            !tutorialController.IsTutorialRunning ||
            !tutorialController.HasAcceptedRequest ||
            tutorialController.RequestedFish == null)
        {
            return null;
        }

        if (_onlyBoostObjectiveFishWhileStillNeeded &&
            tutorialController.HasEnoughRequestedFish)
        {
            return null;
        }

        return tutorialController.RequestedFish;
    }

    private bool ContainsFish(List<FishScriptableObject> _fishList, FishScriptableObject _fishType)
    {
        if (_fishList == null || _fishType == null)
            return false;

        for (int i = 0; i < _fishList.Count; i++)
        {
            if (_fishList[i] == _fishType)
                return true;
        }

        return false;
    }

    private bool ContainsFish(FishScriptableObject[] _fishList, FishScriptableObject _fishType)
    {
        if (_fishList == null || _fishType == null)
            return false;

        for (int i = 0; i < _fishList.Length; i++)
        {
            if (_fishList[i] == _fishType)
                return true;
        }

        return false;
    }

    private void RegisterBittenFish(FishScriptableObject _fishType)
    {
        if (_fishType == null)
            return;

        if (_fishType == _lastBittenFishType)
        {
            _sameFishBiteStreak++;
        }
        else
        {
            _lastBittenFishType = _fishType;
            _sameFishBiteStreak = 1;
        }

        FishScriptableObject objectiveFish = GetCurrentObjectiveFish();

        if (objectiveFish == null || !ContainsFish(_currentAvailableFish, objectiveFish))
        {
            _objectiveFishMissStreak = 0;
            return;
        }

        if (_fishType == objectiveFish)
        {
            _objectiveFishMissStreak = 0;
            return;
        }

        _objectiveFishMissStreak++;
    }

    #endregion

    #region Rewards And Bait

    private float GetProgressMultiplierByFish()
    {
        if (_selectedFishType == null)
            return 1f;

        switch (_selectedFishType.rarity)
        {
            case 1:
                return _rarity1ProgressMultiplier;

            case 2:
                return _rarity2ProgressMultiplier;

            case 3:
                return _rarity3ProgressMultiplier;

            case 4:
                return _rarity3ProgressMultiplier;

            default:
                return 1f;
        }
    }

    private BaitData GetUsableEquippedBait()
    {
        if (_baitInventory == null)
            _baitInventory = FindFirstObjectByType<BaitInventory>(FindObjectsInactive.Include);

        if (_baitInventory == null || _baitInventory.EquippedBait == null)
            return null;

        if (_baitInventory.HasBait(_baitInventory.EquippedBait))
            return _baitInventory.EquippedBait;

        _baitInventory.ClearEquippedBait();
        return null;
    }

    private void ConsumeActiveBait()
    {
        if (_activeBait == null)
            return;

        if (_baitInventory == null)
            _baitInventory = FindFirstObjectByType<BaitInventory>(FindObjectsInactive.Include);

        if (_baitInventory == null || !_baitInventory.TryConsumeBait(_activeBait))
            _activeBait = null;
    }

    private void GivePendingFish()
    {
        if (_currentShipInventory == null || _pendingFish == null)
            return;

        bool addedSuccessfully = _currentShipInventory.TryAddFish(_pendingFish);

        if (addedSuccessfully)
        {
            FishCaptureHistory.RegisterCatch(_pendingFish.typeOfFish);
            Debug.Log($"Peixe capturado: {_pendingFish.typeOfFish.fishName} - {_pendingFish.weight}kg");

            bool showedCatchResult = TryShowCatchResult(_pendingFish);

            if (!showedCatchResult && HUDFishInfoUI.Instance != null)
            {
                HUDFishInfoUI.Instance.ShowFishInfo(
                    _pendingFish.typeOfFish.fishName,
                    _pendingFish.weight
                );
            }

            UpdateTutorialAfterFishCaught(_pendingFish);
        }
        else
        {
            Debug.Log("Inventário cheio.");

            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning("Sem espaço para mais peixes");
        }

        _pendingFish = null;
    }

    private void UpdateTutorialAfterFishCaught(FishData _caughtFish)
    {
        TutorialEvents.NotifyFishCaught(_caughtFish, _currentShipInventory);
    }

    #endregion

    #region End And Cleanup

    private void EndFishing(bool _success)
    {
        bool hadFishBitten = HasFishBitten;

        IsFishing = false;
        HasFishBitten = false;
        ProgressNormalized = 0f;

        if (_fishBiteHandler != null)
            _fishBiteHandler.StopWaiting();

        if (_fishDirectionPull != null)
            _fishDirectionPull.StopPull();

        if (_fishSkillCheck != null)
            _fishSkillCheck.StopSkillCheck();

        if (GameManager.instance != null)
            GameManager.instance.SetState(_waitForCatchResultClose ? GameManager.GameState.InUI : GameManager.GameState.OnBoat);

        if (!_waitForCatchResultClose)
            RestoreConfiguredPanelsAfterFishing();

        if (_fishingRod != null)
            _fishingRod.ReturnHookAfterFishing();

        FishingEnded?.Invoke(_success, hadFishBitten);
        ClearFishingData();
    }

    private bool TryShowCatchResult(FishData _caughtFish)
    {
        if (_caughtFish == null || _caughtFish.typeOfFish == null)
            return false;

        AutoAssignMissingReferences();

        if (_fishResultUI != null)
        {
            _fishResultUI.Closed -= HandleCatchResultClosed;
            _fishResultUI.Closed += HandleCatchResultClosed;
            _fishResultUI.ShowCatchResult(_caughtFish);

            _waitForCatchResultClose = _fishResultUI.IsShowing && _fishResultUI.gameObject.activeInHierarchy;

            if (!_waitForCatchResultClose)
                UnsubscribeCatchResultPanel();

            return _waitForCatchResultClose;
        }

        Debug.LogWarning("FishResultPanelUI não encontrado na cena.");
        return false;
    }

    private void HandleCatchResultClosed()
    {
        if (!_waitForCatchResultClose)
            return;

        _waitForCatchResultClose = false;
        UnsubscribeCatchResultPanel();

        if (GameManager.instance != null &&
            GameManager.instance.currentState == GameManager.GameState.InUI)
        {
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
        }

        RestoreConfiguredPanelsAfterFishing();
    }

    private void UnsubscribeCatchResultPanel()
    {
        if (_fishResultUI != null)
            _fishResultUI.Closed -= HandleCatchResultClosed;
    }

    private void ClearFishingData()
    {
        _currentShipInventory = null;
        _currentAvailableFish = null;
        _selectedFishType = null;
        _pendingFish = null;
        _activeBait = null;
    }

    private void ReturnToBoatState()
    {
        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
    }

    #endregion
}
