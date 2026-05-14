using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class CampaignQuestDefinition
{
    public string questName = "Quest";
    [Min(1)] public int durationDays = 3;
    [Min(0)] public int debtPaymentTarget = 150;
    public bool isTutorialQuest;
    public bool unlocksFreePlayOnComplete;
    public bool requiresSpecialMoneyLenderDelivery;
    public FishScriptableObject specialDeliveryFish;
    [Min(0)] public int specialDeliveryQuantity;
    [Min(0)] public int specialDeliveryRequiredWeight;
    public bool completesCampaign;

    public CampaignQuestDefinition()
    {
    }

    public CampaignQuestDefinition(
        string _questName,
        int _durationDays,
        int _debtPaymentTarget,
        bool _isTutorialQuest,
        bool _unlocksFreePlayOnComplete,
        bool _requiresSpecialDelivery,
        bool _completesCampaign)
    {
        questName = _questName;
        durationDays = Mathf.Max(1, _durationDays);
        debtPaymentTarget = Mathf.Max(0, _debtPaymentTarget);
        isTutorialQuest = _isTutorialQuest;
        unlocksFreePlayOnComplete = _unlocksFreePlayOnComplete;
        requiresSpecialMoneyLenderDelivery = _requiresSpecialDelivery;
        completesCampaign = _completesCampaign;
    }
}

public class CampaignProgressSystem : MonoBehaviour
{
    public static CampaignProgressSystem Instance { get; private set; }

    [Header("Mode")]
    [SerializeField] private GameProgressMode gameMode = GameProgressMode.Campaign;
    [SerializeField] private bool endlessUnlocked;

    [Header("Campaign Quest List")]
    [SerializeField] private CampaignQuestDefinition[] questDefinitions = CreateDefaultQuestDefinitions();
    [SerializeField, Min(0)] private int campaignCompletionDebtAmount = 999999;

    [Header("Campaign Quest State")]
    [SerializeField, Min(1)] private int currentQuestIndex = 1;
    [SerializeField, Min(1)] private int questDurationDays = 3;
    [SerializeField, Min(0)] private int daysElapsedInCurrentQuest;
    [SerializeField, Min(0)] private int questDebtPaymentTarget = 50;
    [SerializeField, Min(0)] private int questDebtPaidAmount;
    [SerializeField] private bool hasFailedCurrentQuest;
    [SerializeField] private bool hasUnlockedFreePlay;
    [SerializeField] private bool isCampaignCompleted;

    [Header("Special Money Lender Delivery")]
    [SerializeField] private bool isSpecialMoneyLenderDeliveryActive;
    [SerializeField] private FishScriptableObject specialDeliveryFish;
    [SerializeField, Min(0)] private int specialDeliveryQuantity;
    [SerializeField, Min(0)] private int specialDeliveryRequiredWeight;

    private DayCycle boundDayCycle;

    public event Action OnProgressChanged;
    public event Action OnQuestAdvanced;
    public event Action OnQuestDeadlineExpired;
    public event Action OnCampaignCompleted;

    public GameProgressMode GameMode => gameMode;
    public bool EndlessUnlocked => endlessUnlocked;
    public bool HasUnlockedFreePlay => hasUnlockedFreePlay;
    public bool IsCampaignCompleted => isCampaignCompleted;
    public bool IsCampaignQuestRunning => IsCampaignRunning();
    public int CurrentQuestIndex => currentQuestIndex;
    public int MaxQuestCount => questDefinitions != null ? questDefinitions.Length : 0;
    public CampaignQuestDefinition CurrentQuestDefinition => GetQuestDefinition(currentQuestIndex);
    public string CurrentQuestName => CurrentQuestDefinition != null ? CurrentQuestDefinition.questName : $"Quest {currentQuestIndex}";
    public bool IsCurrentQuestTutorial => CurrentQuestDefinition != null && CurrentQuestDefinition.isTutorialQuest;
    public bool IsCurrentQuestFinal => CurrentQuestDefinition != null &&
                                       (CurrentQuestDefinition.completesCampaign || currentQuestIndex >= MaxQuestCount);
    public bool CurrentQuestRequiresSpecialDelivery => CurrentQuestDefinition != null &&
                                                       CurrentQuestDefinition.requiresSpecialMoneyLenderDelivery;
    public int QuestDurationDays => questDurationDays;
    public int DaysElapsedInCurrentQuest => daysElapsedInCurrentQuest;
    public int DaysRemainingInCurrentQuest => Mathf.Max(0, questDurationDays - daysElapsedInCurrentQuest);
    public int QuestDebtPaymentTarget => questDebtPaymentTarget;
    public int QuestDebtPaidAmount => questDebtPaidAmount;
    public int QuestDebtPaymentRemaining => Mathf.Max(0, questDebtPaymentTarget - questDebtPaidAmount);
    public bool HasFailedCurrentQuest => hasFailedCurrentQuest;
    public bool IsSpecialMoneyLenderDeliveryActive => isSpecialMoneyLenderDeliveryActive;
    public FishScriptableObject SpecialDeliveryFish => specialDeliveryFish;
    public int SpecialDeliveryQuantity => specialDeliveryQuantity;
    public int SpecialDeliveryRequiredWeight => specialDeliveryRequiredWeight;

    public static CampaignProgressSystem GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<CampaignProgressSystem>();

        if (Instance != null)
            return Instance;

        GameObject progressObject = new GameObject("CampaignProgressSystem");
        Instance = progressObject.AddComponent<CampaignProgressSystem>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureQuestDefinitions();
        ClampState();
        ApplyCurrentQuestDefinition(true);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryBindDayCycle();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindDayCycle();
    }

    private void OnValidate()
    {
        EnsureQuestDefinitions();
        ClampQuestDefinitions();
        ClampState();
    }

    public void StartNewCampaign()
    {
        gameMode = GameProgressMode.Campaign;
        endlessUnlocked = false;
        hasUnlockedFreePlay = false;
        isCampaignCompleted = false;
        hasFailedCurrentQuest = false;
        currentQuestIndex = 1;
        daysElapsedInCurrentQuest = 0;
        questDebtPaidAmount = 0;
        ApplyCurrentQuestDefinition(true);
        NotifyChanged();
    }

    public bool StartEndlessMode()
    {
        if (!endlessUnlocked)
            return false;

        gameMode = GameProgressMode.Endless;
        hasFailedCurrentQuest = false;
        ClearSpecialDeliveryInternal();
        NotifyChanged();
        return true;
    }

    public void RegisterDebtPayment(int _paidAmount, int _currentQuestTarget, int _nextQuestTarget)
    {
        if (!IsCampaignRunning())
            return;

        if (questDebtPaymentTarget <= 0 && !CurrentQuestRequiresSpecialDelivery)
            questDebtPaymentTarget = Mathf.Max(0, _currentQuestTarget);

        questDebtPaidAmount += Mathf.Max(0, _paidAmount);

        if (questDebtPaymentTarget > 0)
            questDebtPaidAmount = Mathf.Min(questDebtPaidAmount, questDebtPaymentTarget);

        if (CurrentQuestRequiresSpecialDelivery)
        {
            NotifyChanged();
            return;
        }

        if (IsCurrentQuestPaymentComplete())
            CompleteCurrentQuest(_nextQuestTarget);
        else
            NotifyChanged();
    }

    public void AdvanceQuest(int _nextQuestDebtPaymentTarget)
    {
        if (!IsCampaignRunning())
            return;

        CompleteCurrentQuest(_nextQuestDebtPaymentTarget);
    }

    public bool CompleteSpecialDeliveryQuest()
    {
        if (!IsCampaignRunning() || !CurrentQuestRequiresSpecialDelivery)
            return false;

        DebtSystem debtSystem = DebtSystem.GetOrCreate();

        if (debtSystem != null && debtSystem.HasDebt)
            debtSystem.ReduceDebt(debtSystem.CurrentDebt);

        CompleteCurrentQuest(0);
        return true;
    }

    public void SetSpecialDelivery(FishScriptableObject _fish, int _quantity, int _requiredWeight)
    {
        SetSpecialDeliveryInternal(_fish, _quantity, _requiredWeight);
        NotifyChanged();
    }

    public void ClearSpecialDelivery()
    {
        ClearSpecialDeliveryInternal();
        NotifyChanged();
    }

    public CampaignSaveData CaptureSaveData()
    {
        return new CampaignSaveData
        {
            gameMode = gameMode,
            currentQuestIndex = currentQuestIndex,
            maxQuestCount = MaxQuestCount,
            questDurationDays = questDurationDays,
            daysElapsedInCurrentQuest = daysElapsedInCurrentQuest,
            questDebtPaymentTarget = questDebtPaymentTarget,
            questDebtPaidAmount = questDebtPaidAmount,
            hasFailedCurrentQuest = hasFailedCurrentQuest,
            hasUnlockedFreePlay = hasUnlockedFreePlay,
            isCampaignCompleted = isCampaignCompleted,
            endlessUnlocked = endlessUnlocked,
            campaignCompletionDebtAmount = campaignCompletionDebtAmount,
            isSpecialMoneyLenderDeliveryActive = isSpecialMoneyLenderDeliveryActive,
            specialDeliveryFishId = specialDeliveryFish != null ? specialDeliveryFish.SaveId : string.Empty,
            specialDeliveryQuantity = specialDeliveryQuantity,
            specialDeliveryRequiredWeight = specialDeliveryRequiredWeight
        };
    }

    public void ApplySaveData(CampaignSaveData _data)
    {
        if (_data == null)
            return;

        EnsureQuestDefinitions();

        gameMode = _data.gameMode;
        currentQuestIndex = _data.currentQuestIndex;
        questDurationDays = _data.questDurationDays;
        daysElapsedInCurrentQuest = _data.daysElapsedInCurrentQuest;
        questDebtPaymentTarget = _data.questDebtPaymentTarget;
        questDebtPaidAmount = _data.questDebtPaidAmount;
        hasFailedCurrentQuest = _data.hasFailedCurrentQuest;
        hasUnlockedFreePlay = _data.hasUnlockedFreePlay;
        isCampaignCompleted = _data.isCampaignCompleted;
        endlessUnlocked = _data.endlessUnlocked;

        if (_data.campaignCompletionDebtAmount > 0)
            campaignCompletionDebtAmount = _data.campaignCompletionDebtAmount;

        isSpecialMoneyLenderDeliveryActive = _data.isSpecialMoneyLenderDeliveryActive;
        specialDeliveryFish = FishSaveResolver.FindFishById(_data.specialDeliveryFishId);
        specialDeliveryQuantity = _data.specialDeliveryQuantity;
        specialDeliveryRequiredWeight = _data.specialDeliveryRequiredWeight;

        ClampState();
        RestoreSpecialDeliveryStateFromSave();
        NotifyChanged();
    }

    private void CompleteCurrentQuest(int _fallbackNextDebtPaymentTarget)
    {
        CampaignQuestDefinition completedQuest = CurrentQuestDefinition;

        if (completedQuest != null && completedQuest.unlocksFreePlayOnComplete)
            hasUnlockedFreePlay = true;

        if (completedQuest != null && completedQuest.completesCampaign || currentQuestIndex >= MaxQuestCount)
        {
            CompleteCampaign();
            return;
        }

        currentQuestIndex++;
        daysElapsedInCurrentQuest = 0;
        questDebtPaidAmount = 0;
        hasFailedCurrentQuest = false;
        ApplyCurrentQuestDefinition(true, _fallbackNextDebtPaymentTarget);
        OnQuestAdvanced?.Invoke();
        NotifyChanged();
    }

    private void CompleteCampaign()
    {
        currentQuestIndex = Mathf.Max(1, MaxQuestCount);
        daysElapsedInCurrentQuest = 0;
        questDebtPaidAmount = 0;
        hasFailedCurrentQuest = false;
        hasUnlockedFreePlay = true;
        endlessUnlocked = true;
        isCampaignCompleted = true;
        ClearSpecialDeliveryInternal();

        DebtSystem debtSystem = DebtSystem.GetOrCreate();

        if (debtSystem != null && campaignCompletionDebtAmount > 0)
            debtSystem.SetDebt(campaignCompletionDebtAmount);

        OnCampaignCompleted?.Invoke();
        NotifyChanged();
    }

    private void ApplyCurrentQuestDefinition(bool _resetPayment, int _fallbackDebtPaymentTarget = 0)
    {
        CampaignQuestDefinition currentQuest = CurrentQuestDefinition;

        if (currentQuest == null)
        {
            questDurationDays = Mathf.Max(1, questDurationDays);
            questDebtPaymentTarget = Mathf.Max(0, _fallbackDebtPaymentTarget);
            ClearSpecialDeliveryInternal();
            return;
        }

        questDurationDays = Mathf.Max(1, currentQuest.durationDays);
        questDebtPaymentTarget = Mathf.Max(0, currentQuest.debtPaymentTarget);

        if (questDebtPaymentTarget <= 0 && _fallbackDebtPaymentTarget > 0 && !currentQuest.requiresSpecialMoneyLenderDelivery)
            questDebtPaymentTarget = _fallbackDebtPaymentTarget;

        if (_resetPayment)
            questDebtPaidAmount = 0;

        if (currentQuest.requiresSpecialMoneyLenderDelivery)
        {
            SetSpecialDeliveryInternal(
                currentQuest.specialDeliveryFish,
                currentQuest.specialDeliveryQuantity,
                currentQuest.specialDeliveryRequiredWeight
            );
        }
        else
        {
            ClearSpecialDeliveryInternal();
        }
    }

    private void RestoreSpecialDeliveryStateFromSave()
    {
        CampaignQuestDefinition currentQuest = CurrentQuestDefinition;

        if (currentQuest == null || !currentQuest.requiresSpecialMoneyLenderDelivery)
        {
            ClearSpecialDeliveryInternal();
            return;
        }

        if (specialDeliveryFish == null)
        {
            SetSpecialDeliveryInternal(
                currentQuest.specialDeliveryFish,
                currentQuest.specialDeliveryQuantity,
                currentQuest.specialDeliveryRequiredWeight
            );
        }

        isSpecialMoneyLenderDeliveryActive = specialDeliveryFish != null && specialDeliveryQuantity > 0;
    }

    private void SetSpecialDeliveryInternal(FishScriptableObject _fish, int _quantity, int _requiredWeight)
    {
        specialDeliveryFish = _fish;
        specialDeliveryQuantity = Mathf.Max(0, _quantity);
        specialDeliveryRequiredWeight = Mathf.Max(0, _requiredWeight);
        isSpecialMoneyLenderDeliveryActive = specialDeliveryFish != null && specialDeliveryQuantity > 0;
    }

    private void ClearSpecialDeliveryInternal()
    {
        isSpecialMoneyLenderDeliveryActive = false;
        specialDeliveryFish = null;
        specialDeliveryQuantity = 0;
        specialDeliveryRequiredWeight = 0;
    }

    private void HandleSceneLoaded(Scene _scene, LoadSceneMode _mode)
    {
        TryBindDayCycle();
    }

    private void TryBindDayCycle()
    {
        DayCycle dayCycle = FindFirstObjectByType<DayCycle>();

        if (dayCycle == null || dayCycle == boundDayCycle)
            return;

        UnbindDayCycle();
        boundDayCycle = dayCycle;
        boundDayCycle.DayChanged += HandleDayChanged;
    }

    private void UnbindDayCycle()
    {
        if (boundDayCycle != null)
            boundDayCycle.DayChanged -= HandleDayChanged;

        boundDayCycle = null;
    }

    private void HandleDayChanged(int _elapsedDays)
    {
        if (!IsCampaignRunning())
            return;

        daysElapsedInCurrentQuest++;

        if (daysElapsedInCurrentQuest >= questDurationDays && !IsCurrentQuestComplete())
        {
            hasFailedCurrentQuest = true;
            OnQuestDeadlineExpired?.Invoke();
        }

        NotifyChanged();
    }

    private bool IsCampaignRunning()
    {
        return gameMode == GameProgressMode.Campaign &&
               !hasFailedCurrentQuest &&
               !isCampaignCompleted;
    }

    private bool IsCurrentQuestComplete()
    {
        if (CurrentQuestRequiresSpecialDelivery)
            return false;

        return IsCurrentQuestPaymentComplete();
    }

    private bool IsCurrentQuestPaymentComplete()
    {
        return questDebtPaymentTarget <= 0 || questDebtPaidAmount >= questDebtPaymentTarget;
    }

    private CampaignQuestDefinition GetQuestDefinition(int _questIndex)
    {
        EnsureQuestDefinitions();

        if (questDefinitions == null || questDefinitions.Length == 0)
            return null;

        int index = Mathf.Clamp(_questIndex, 1, questDefinitions.Length) - 1;
        return questDefinitions[index];
    }

    private void EnsureQuestDefinitions()
    {
        if (questDefinitions != null && questDefinitions.Length > 0)
            return;

        questDefinitions = CreateDefaultQuestDefinitions();
    }

    private void ClampQuestDefinitions()
    {
        if (questDefinitions == null)
            return;

        for (int i = 0; i < questDefinitions.Length; i++)
        {
            CampaignQuestDefinition quest = questDefinitions[i];

            if (quest == null)
                continue;

            quest.durationDays = Mathf.Max(1, quest.durationDays);
            quest.debtPaymentTarget = Mathf.Max(0, quest.debtPaymentTarget);
            quest.specialDeliveryQuantity = Mathf.Max(0, quest.specialDeliveryQuantity);
            quest.specialDeliveryRequiredWeight = Mathf.Max(0, quest.specialDeliveryRequiredWeight);
        }
    }

    private void ClampState()
    {
        EnsureQuestDefinitions();
        ClampQuestDefinitions();

        int maxQuestIndex = Mathf.Max(1, MaxQuestCount);
        currentQuestIndex = Mathf.Clamp(currentQuestIndex, 1, maxQuestIndex);
        questDurationDays = Mathf.Max(1, questDurationDays);
        daysElapsedInCurrentQuest = Mathf.Max(0, daysElapsedInCurrentQuest);
        questDebtPaymentTarget = Mathf.Max(0, questDebtPaymentTarget);
        questDebtPaidAmount = Mathf.Max(0, questDebtPaidAmount);
        specialDeliveryQuantity = Mathf.Max(0, specialDeliveryQuantity);
        specialDeliveryRequiredWeight = Mathf.Max(0, specialDeliveryRequiredWeight);
        campaignCompletionDebtAmount = Mathf.Max(0, campaignCompletionDebtAmount);
    }

    private static CampaignQuestDefinition[] CreateDefaultQuestDefinitions()
    {
        return new[]
        {
            new CampaignQuestDefinition("Quest 1 - Tutorial", 3, 50, true, true, false, false),
            new CampaignQuestDefinition("Quest 2", 3, 225, false, false, false, false),
            new CampaignQuestDefinition("Quest 3", 3, 300, false, false, false, false),
            new CampaignQuestDefinition("Quest 4", 3, 375, false, false, false, false),
            new CampaignQuestDefinition("Quest 5 - Entrega especial", 3, 0, false, false, true, true)
        };
    }

    private void NotifyChanged()
    {
        OnProgressChanged?.Invoke();
    }
}
