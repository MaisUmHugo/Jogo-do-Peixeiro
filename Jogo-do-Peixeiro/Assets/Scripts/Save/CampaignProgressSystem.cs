using System;
using System.Collections.Generic;
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
    [Range(0, 4)] public int specialDeliveryRarity;
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
        bool _completesCampaign,
        int _specialDeliveryRarity = 0,
        int _specialDeliveryQuantity = 0,
        int _specialDeliveryRequiredWeight = 0)
    {
        questName = _questName;
        durationDays = Mathf.Max(1, _durationDays);
        debtPaymentTarget = Mathf.Max(0, _debtPaymentTarget);
        isTutorialQuest = _isTutorialQuest;
        unlocksFreePlayOnComplete = _unlocksFreePlayOnComplete;
        requiresSpecialMoneyLenderDelivery = _requiresSpecialDelivery;
        completesCampaign = _completesCampaign;
        specialDeliveryRarity = Mathf.Clamp(_specialDeliveryRarity, 0, 4);
        specialDeliveryQuantity = Mathf.Max(0, _specialDeliveryQuantity);
        specialDeliveryRequiredWeight = Mathf.Max(0, _specialDeliveryRequiredWeight);
    }
}

public class CampaignProgressSystem : MonoBehaviour
{
    private const int DefaultCampaignCompletionDebtAmount = 100000000;
    private const int LegacyCampaignCompletionDebtAmount = 999999;

    public static CampaignProgressSystem Instance { get; private set; }

    [Header("Mode")]
    [SerializeField] private GameProgressMode gameMode = GameProgressMode.Campaign;
    [SerializeField] private bool endlessUnlocked;

    [Header("Campaign Quest List")]
    [SerializeField] private CampaignQuestDefinition[] questDefinitions = CreateDefaultQuestDefinitions();
    [SerializeField, Min(0)] private int campaignCompletionDebtAmount = DefaultCampaignCompletionDebtAmount;

    [Header("Debt Formula")]
    [SerializeField] private bool useGddDebtFormula = true;
    [SerializeField, Min(1)] private int debtFormulaBaseAmount = 150;
    [SerializeField, Range(0f, 1f)] private float debtFormulaRandomMin = 0.02f;
    [SerializeField, Range(0f, 1f)] private float debtFormulaRandomMax = 0.1f;
    [SerializeField, Min(0.01f)] private float debtFormulaProgressionDivisor = 16f;

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
    [SerializeField, Min(0)] private int specialDeliveryDebtReduction;

    [Header("Special Delivery Fallback")]
    [SerializeField] private FishScriptableObject[] specialDeliveryFishPool;
    [SerializeField, Min(1)] private int defaultSpecialDeliveryQuantity = 1;

    [Header("Endless Mode")]
    [SerializeField, Range(0f, 1f)] private float endlessSpecialDeliveryChanceIncrease = 0.1f;
    [SerializeField, Range(0f, 1f)] private float endlessSpecialDeliveryMaxChance = 0.7f;
    [SerializeField, Min(1)] private int endlessSpecialDeliveryBaseQuantity = 1;
    [SerializeField, Min(1)] private int endlessSpecialDeliveryQuantityStep = 5;
    [SerializeField, Range(1, 3)] private int endlessSpecialDeliveryMinRarity = 1;
    [SerializeField, Range(1, 3)] private int endlessSpecialDeliveryMaxRarity = 3;
    [SerializeField, Min(0)] private int endlessDeliveriesWithoutSpecialRequest;

    [Header("Autosave")]
    [SerializeField] private bool saveOnQuestAdvanced = true;

    private DayCycle boundDayCycle;

    private static readonly Vector2Int[] GddCampaignDebtPaymentRanges =
    {
        new Vector2Int(153, 165),
        new Vector2Int(162, 176),
        new Vector2Int(191, 206),
        new Vector2Int(239, 258),
        new Vector2Int(245, 264)
    };

    public event Action OnProgressChanged;
    public event Action OnQuestAdvanced;
    public event Action OnQuestDeadlineExpired;
    public event Action OnCampaignCompleted;

    public GameProgressMode GameMode => gameMode;
    public bool EndlessUnlocked => endlessUnlocked;
    public bool HasUnlockedFreePlay => hasUnlockedFreePlay;
    public bool IsCampaignCompleted => isCampaignCompleted;
    public bool IsCampaignQuestRunning => IsCampaignRunning();
    public bool IsDebtQuestRunning => IsQuestRunning();
    public bool UsesQuestDebtPayment => IsQuestRunning() && questDebtPaymentTarget > 0;
    public int CurrentQuestIndex => currentQuestIndex;
    public int MaxQuestCount => questDefinitions != null ? questDefinitions.Length : 0;
    public CampaignQuestDefinition CurrentQuestDefinition => GetQuestDefinition(currentQuestIndex);
    public int CampaignCompletionDebtAmount => campaignCompletionDebtAmount;
    public string CurrentQuestName => gameMode == GameProgressMode.Endless
        ? $"Entrega {currentQuestIndex}"
        : CurrentQuestDefinition != null ? CurrentQuestDefinition.questName : $"Quest {currentQuestIndex}";
    public bool IsCurrentQuestTutorial => gameMode == GameProgressMode.Campaign &&
                                          CurrentQuestDefinition != null &&
                                          CurrentQuestDefinition.isTutorialQuest;
    public bool IsCurrentQuestFinal => gameMode == GameProgressMode.Campaign &&
                                       CurrentQuestDefinition != null &&
                                       (CurrentQuestDefinition.completesCampaign || currentQuestIndex >= MaxQuestCount);
    public bool CurrentQuestRequiresSpecialDelivery => gameMode == GameProgressMode.Endless
        ? isSpecialMoneyLenderDeliveryActive
        : CurrentQuestDefinition != null && CurrentQuestDefinition.requiresSpecialMoneyLenderDelivery;
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
    public int SpecialDeliveryDebtReduction => specialDeliveryDebtReduction;
    public int EndlessDeliveriesWithoutSpecialRequest => endlessDeliveriesWithoutSpecialRequest;

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
        endlessDeliveriesWithoutSpecialRequest = 0;
        ApplyCurrentQuestDefinition(true);
        NotifyChanged();
    }

    public bool StartEndlessMode()
    {
        if (!endlessUnlocked)
            return false;

        gameMode = GameProgressMode.Endless;
        hasFailedCurrentQuest = false;
        isCampaignCompleted = false;
        hasUnlockedFreePlay = true;
        ApplyEndlessQuestDefinition(true);
        NotifyChanged();
        return true;
    }

    public void StartUnlockedEndlessMode()
    {
        endlessUnlocked = true;
        hasUnlockedFreePlay = true;
        gameMode = GameProgressMode.Endless;
        isCampaignCompleted = false;
        hasFailedCurrentQuest = false;
        currentQuestIndex = 1;
        daysElapsedInCurrentQuest = 0;
        questDebtPaidAmount = 0;
        endlessDeliveriesWithoutSpecialRequest = 0;
        ApplyEndlessQuestDefinition(true);
        NotifyChanged();
    }

    public void RegisterDebtPayment(int _paidAmount, int _currentQuestTarget, int _nextQuestTarget)
    {
        if (!IsQuestRunning())
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
        if (!IsQuestRunning())
            return;

        CompleteCurrentQuest(_nextQuestDebtPaymentTarget);
    }

    public void RetryCurrentQuest()
    {
        if (!hasFailedCurrentQuest)
            return;

        hasFailedCurrentQuest = false;
        daysElapsedInCurrentQuest = 0;
        ClampState();
        NotifyChanged();
    }

    public bool CompleteSpecialDeliveryQuest()
    {
        if (!IsQuestRunning() || !CurrentQuestRequiresSpecialDelivery)
            return false;

        DebtSystem debtSystem = DebtSystem.GetOrCreate();

        if (debtSystem != null && debtSystem.HasDebt)
        {
            int debtReduction = GetSpecialDeliveryCompletionDebtReduction(debtSystem);

            if (debtReduction > 0)
                debtSystem.ReduceDebt(debtReduction);
        }

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

    public void DebugUnlockEndlessMode()
    {
        endlessUnlocked = true;
        hasUnlockedFreePlay = true;
        NotifyChanged();
    }

    public bool DebugExpireCurrentQuestDeadline()
    {
        if (!IsQuestRunning())
            return false;

        daysElapsedInCurrentQuest = questDurationDays;
        hasFailedCurrentQuest = true;
        OnQuestDeadlineExpired?.Invoke();
        NotifyChanged();
        return true;
    }

    public void DebugCompleteCampaign()
    {
        gameMode = GameProgressMode.Campaign;
        hasFailedCurrentQuest = false;
        currentQuestIndex = Mathf.Max(1, MaxQuestCount);
        ApplyCurrentQuestDefinition(true);
        CompleteCampaign();
    }

    public bool DebugForceEndlessSpecialDelivery()
    {
        if (gameMode != GameProgressMode.Endless)
            return false;

        int requestedRarity = GetRandomEndlessSpecialDeliveryRarity();
        float randomBonus = GetDebtFormulaRandomBonus();
        int fullTarget = CalculateDebtPaymentTarget(currentQuestIndex - 1, 0, randomBonus);
        int specialTarget = CalculateDebtPaymentTarget(currentQuestIndex - 1, requestedRarity, randomBonus);
        int quantity = GetEndlessSpecialDeliveryQuantity();

        hasFailedCurrentQuest = false;
        daysElapsedInCurrentQuest = 0;
        questDebtPaidAmount = 0;
        questDebtPaymentTarget = specialTarget;
        endlessDeliveriesWithoutSpecialRequest = 0;
        SetSpecialDeliveryInternal(null, quantity, 0, requestedRarity, Mathf.Max(0, fullTarget - specialTarget));
        NotifyChanged();

        return isSpecialMoneyLenderDeliveryActive;
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
            specialDeliveryRequiredWeight = specialDeliveryRequiredWeight,
            specialDeliveryDebtReduction = specialDeliveryDebtReduction,
            endlessDeliveriesWithoutSpecialRequest = endlessDeliveriesWithoutSpecialRequest
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
        {
            campaignCompletionDebtAmount = _data.campaignCompletionDebtAmount == LegacyCampaignCompletionDebtAmount
                ? DefaultCampaignCompletionDebtAmount
                : _data.campaignCompletionDebtAmount;
        }

        isSpecialMoneyLenderDeliveryActive = _data.isSpecialMoneyLenderDeliveryActive;
        specialDeliveryFish = FishSaveResolver.FindFishById(_data.specialDeliveryFishId);
        specialDeliveryQuantity = _data.specialDeliveryQuantity;
        specialDeliveryRequiredWeight = _data.specialDeliveryRequiredWeight;
        specialDeliveryDebtReduction = _data.specialDeliveryDebtReduction;
        endlessDeliveriesWithoutSpecialRequest = _data.endlessDeliveriesWithoutSpecialRequest;

        ClampState();
        RestoreSpecialDeliveryStateFromSave();
        NotifyChanged();
    }

    private void CompleteCurrentQuest(int _fallbackNextDebtPaymentTarget)
    {
        CampaignQuestDefinition completedQuest = CurrentQuestDefinition;

        if (completedQuest != null && completedQuest.unlocksFreePlayOnComplete)
            hasUnlockedFreePlay = true;

        if (gameMode == GameProgressMode.Campaign &&
            (completedQuest != null && completedQuest.completesCampaign || currentQuestIndex >= MaxQuestCount))
        {
            CompleteCampaign();
            return;
        }

        currentQuestIndex++;
        daysElapsedInCurrentQuest = 0;
        questDebtPaidAmount = 0;
        hasFailedCurrentQuest = false;

        if (gameMode == GameProgressMode.Endless)
            ApplyEndlessQuestDefinition(true);
        else
            ApplyCurrentQuestDefinition(true, _fallbackNextDebtPaymentTarget);

        OnQuestAdvanced?.Invoke();
        NotifyChanged();
        SaveProgressAfterQuestResolution();
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
        SaveProgressAfterQuestResolution();
    }

    private void ApplyCurrentQuestDefinition(bool _resetPayment, int _fallbackDebtPaymentTarget = 0)
    {
        if (gameMode == GameProgressMode.Endless)
        {
            ApplyEndlessQuestDefinition(_resetPayment);
            return;
        }

        CampaignQuestDefinition currentQuest = CurrentQuestDefinition;

        if (currentQuest == null)
        {
            questDurationDays = Mathf.Max(1, questDurationDays);
            questDebtPaymentTarget = Mathf.Max(0, _fallbackDebtPaymentTarget);
            ClearSpecialDeliveryInternal();
            return;
        }

        questDurationDays = Mathf.Max(1, currentQuest.durationDays);
        questDebtPaymentTarget = GetCampaignQuestDebtPaymentTarget(currentQuest, currentQuestIndex, out int specialDebtReduction);

        if (questDebtPaymentTarget <= 0 && _fallbackDebtPaymentTarget > 0 && !currentQuest.requiresSpecialMoneyLenderDelivery)
            questDebtPaymentTarget = _fallbackDebtPaymentTarget;

        if (_resetPayment)
            questDebtPaidAmount = 0;

        if (currentQuest.requiresSpecialMoneyLenderDelivery)
        {
            SetSpecialDeliveryInternal(
                currentQuest.specialDeliveryFish,
                currentQuest.specialDeliveryQuantity,
                currentQuest.specialDeliveryRequiredWeight,
                currentQuest.specialDeliveryRarity,
                specialDebtReduction
            );
        }
        else
        {
            ClearSpecialDeliveryInternal();
        }
    }

    private void ApplyEndlessQuestDefinition(bool _resetPayment)
    {
        questDurationDays = GetDefaultQuestDurationDays();

        if (_resetPayment)
            questDebtPaidAmount = 0;

        bool requestSpecialDelivery = ShouldGenerateEndlessSpecialDelivery();
        int requestedRarity = requestSpecialDelivery ? GetRandomEndlessSpecialDeliveryRarity() : 0;
        float randomBonus = GetDebtFormulaRandomBonus();
        int fullTarget = CalculateDebtPaymentTarget(currentQuestIndex - 1, 0, randomBonus);
        questDebtPaymentTarget = requestSpecialDelivery
            ? CalculateDebtPaymentTarget(currentQuestIndex - 1, requestedRarity, randomBonus)
            : fullTarget;

        if (requestSpecialDelivery)
        {
            int quantity = GetEndlessSpecialDeliveryQuantity();
            int debtReduction = Mathf.Max(0, fullTarget - questDebtPaymentTarget);
            SetSpecialDeliveryInternal(null, quantity, 0, requestedRarity, debtReduction);
            endlessDeliveriesWithoutSpecialRequest = 0;
        }
        else
        {
            ClearSpecialDeliveryInternal();
            endlessDeliveriesWithoutSpecialRequest++;
        }
    }

    private int GetCampaignQuestDebtPaymentTarget(CampaignQuestDefinition _quest, int _questIndex, out int _specialDebtReduction)
    {
        _specialDebtReduction = 0;

        if (_quest == null)
            return 0;

        if (!useGddDebtFormula)
            return Mathf.Max(0, _quest.debtPaymentTarget);

        if (TryGetGddCampaignDebtPaymentRange(_questIndex, out Vector2Int range))
            return UnityEngine.Random.Range(range.x, range.y + 1);

        int requestedRarity = _quest.requiresSpecialMoneyLenderDelivery ? _quest.specialDeliveryRarity : 0;
        float randomBonus = GetDebtFormulaRandomBonus();
        int fullTarget = CalculateDebtPaymentTarget(_questIndex - 1, 0, randomBonus);
        int target = CalculateDebtPaymentTarget(_questIndex - 1, requestedRarity, randomBonus);
        _specialDebtReduction = Mathf.Max(0, fullTarget - target);
        return target;
    }

    private static bool TryGetGddCampaignDebtPaymentRange(int _questIndex, out Vector2Int _range)
    {
        int index = _questIndex - 1;

        if (index >= 0 && index < GddCampaignDebtPaymentRanges.Length)
        {
            _range = GddCampaignDebtPaymentRanges[index];
            return true;
        }

        _range = default;
        return false;
    }

    private int GenerateDebtPaymentTarget(int _completedDeliveries, int _requestedFishRarity)
    {
        return CalculateDebtPaymentTarget(_completedDeliveries, _requestedFishRarity, GetDebtFormulaRandomBonus());
    }

    private int CalculateDebtPaymentTarget(int _completedDeliveries, int _requestedFishRarity, float _randomBonus)
    {
        int completedDeliveries = Mathf.Max(0, _completedDeliveries);
        float randomMultiplier = 1f + Mathf.Max(0f, _randomBonus);
        float progressionMultiplier = 1f + (completedDeliveries * completedDeliveries) / Mathf.Max(0.01f, debtFormulaProgressionDivisor);
        float rarityMultiplier = GetRarityDebtMultiplier(_requestedFishRarity);
        float target = debtFormulaBaseAmount * progressionMultiplier * randomMultiplier * rarityMultiplier;
        return Mathf.CeilToInt(target);
    }

    private float GetDebtFormulaRandomBonus()
    {
        float randomMin = Mathf.Min(debtFormulaRandomMin, debtFormulaRandomMax);
        float randomMax = Mathf.Max(debtFormulaRandomMin, debtFormulaRandomMax);
        return UnityEngine.Random.Range(randomMin, randomMax);
    }

    private float GetRarityDebtMultiplier(int _rarity)
    {
        return Mathf.Clamp01(1f - Mathf.Max(0, _rarity) / 10f);
    }

    private bool ShouldGenerateEndlessSpecialDelivery()
    {
        if (endlessDeliveriesWithoutSpecialRequest <= 0)
            return false;

        float chance = Mathf.Min(
            endlessSpecialDeliveryMaxChance,
            endlessDeliveriesWithoutSpecialRequest * endlessSpecialDeliveryChanceIncrease
        );

        return UnityEngine.Random.value <= chance;
    }

    private int GetRandomEndlessSpecialDeliveryRarity()
    {
        int minRarity = Mathf.Clamp(Mathf.Min(endlessSpecialDeliveryMinRarity, endlessSpecialDeliveryMaxRarity), 1, 3);
        int maxRarity = Mathf.Clamp(Mathf.Max(endlessSpecialDeliveryMinRarity, endlessSpecialDeliveryMaxRarity), 1, 3);
        return UnityEngine.Random.Range(minRarity, maxRarity + 1);
    }

    private int GetEndlessSpecialDeliveryQuantity()
    {
        int step = Mathf.Max(1, endlessSpecialDeliveryQuantityStep);
        int bonus = Mathf.Max(0, (currentQuestIndex - 1) / step);
        return Mathf.Max(1, endlessSpecialDeliveryBaseQuantity + bonus);
    }

    private int GetDefaultQuestDurationDays()
    {
        CampaignQuestDefinition firstQuest = GetQuestDefinition(1);
        return firstQuest != null ? Mathf.Max(1, firstQuest.durationDays) : Mathf.Max(1, questDurationDays);
    }

    private int GetSpecialDeliveryCompletionDebtReduction(DebtSystem _debtSystem)
    {
        if (_debtSystem == null)
            return 0;

        if (gameMode == GameProgressMode.Campaign && IsCurrentQuestFinal)
            return _debtSystem.CurrentDebt;

        return Mathf.Min(_debtSystem.CurrentDebt, Mathf.Max(0, specialDeliveryDebtReduction));
    }

    private void RestoreSpecialDeliveryStateFromSave()
    {
        if (gameMode == GameProgressMode.Endless)
        {
            isSpecialMoneyLenderDeliveryActive = specialDeliveryFish != null && specialDeliveryQuantity > 0;
            return;
        }

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
                currentQuest.specialDeliveryRequiredWeight,
                currentQuest.specialDeliveryRarity,
                specialDeliveryDebtReduction
            );
        }

        isSpecialMoneyLenderDeliveryActive = specialDeliveryFish != null && specialDeliveryQuantity > 0;
    }

    private void SetSpecialDeliveryInternal(FishScriptableObject _fish, int _quantity, int _requiredWeight)
    {
        SetSpecialDeliveryInternal(_fish, _quantity, _requiredWeight, 0, 0);
    }

    private void SetSpecialDeliveryInternal(FishScriptableObject _fish, int _quantity, int _requiredWeight, int _rarity, int _debtReduction)
    {
        specialDeliveryFish = IsRequestableSpecialDeliveryFish(_fish, _rarity)
            ? _fish
            : GetRandomSpecialDeliveryFish(_rarity);

        specialDeliveryQuantity = Mathf.Max(0, _quantity);

        if (specialDeliveryFish != null && specialDeliveryQuantity <= 0)
            specialDeliveryQuantity = defaultSpecialDeliveryQuantity;

        specialDeliveryRequiredWeight = Mathf.Max(0, _requiredWeight);
        specialDeliveryDebtReduction = Mathf.Max(0, _debtReduction);
        isSpecialMoneyLenderDeliveryActive = specialDeliveryFish != null && specialDeliveryQuantity > 0;
    }

    private FishScriptableObject GetRandomSpecialDeliveryFish(int _rarity = 0)
    {
        List<FishScriptableObject> candidates = new List<FishScriptableObject>();

        AddSpecialDeliveryCandidates(candidates, specialDeliveryFishPool, _rarity);

        foreach (FishingAreaDefinition area in Resources.FindObjectsOfTypeAll<FishingAreaDefinition>())
        {
            if (area != null)
                AddSpecialDeliveryCandidates(candidates, area.AvailableFish, _rarity);
        }

        AddSpecialDeliveryCandidates(candidates, Resources.FindObjectsOfTypeAll<FishScriptableObject>(), _rarity);

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private void AddSpecialDeliveryCandidates(List<FishScriptableObject> _candidates, IEnumerable<FishScriptableObject> _fishList, int _rarity)
    {
        if (_candidates == null || _fishList == null)
            return;

        foreach (FishScriptableObject fish in _fishList)
        {
            if (IsRequestableSpecialDeliveryFish(fish, _rarity) && !_candidates.Contains(fish))
                _candidates.Add(fish);
        }
    }

    private bool IsRequestableSpecialDeliveryFish(FishScriptableObject _fish)
    {
        return IsRequestableSpecialDeliveryFish(_fish, 0);
    }

    private bool IsRequestableSpecialDeliveryFish(FishScriptableObject _fish, int _rarity)
    {
        return _fish != null &&
               _fish.CanBeRequestedByMoneyLender &&
               (_rarity <= 0 || _fish.rarity == _rarity);
    }

    private void ClearSpecialDeliveryInternal()
    {
        isSpecialMoneyLenderDeliveryActive = false;
        specialDeliveryFish = null;
        specialDeliveryQuantity = 0;
        specialDeliveryRequiredWeight = 0;
        specialDeliveryDebtReduction = 0;
    }

    private void SaveProgressAfterQuestResolution()
    {
        if (!saveOnQuestAdvanced)
            return;

        GameSaveManager.GetOrCreate().SaveGame();
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
        if (!IsQuestRunning())
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

    private bool IsQuestRunning()
    {
        if (hasFailedCurrentQuest)
            return false;

        return gameMode == GameProgressMode.Endless || IsCampaignRunning();
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
            quest.specialDeliveryRarity = Mathf.Clamp(quest.specialDeliveryRarity, 0, 4);
            quest.specialDeliveryQuantity = Mathf.Max(0, quest.specialDeliveryQuantity);
            quest.specialDeliveryRequiredWeight = Mathf.Max(0, quest.specialDeliveryRequiredWeight);
        }
    }

    private void ClampState()
    {
        EnsureQuestDefinitions();
        ClampQuestDefinitions();

        int maxQuestIndex = Mathf.Max(1, MaxQuestCount);
        currentQuestIndex = gameMode == GameProgressMode.Endless
            ? Mathf.Max(1, currentQuestIndex)
            : Mathf.Clamp(currentQuestIndex, 1, maxQuestIndex);
        questDurationDays = Mathf.Max(1, questDurationDays);
        daysElapsedInCurrentQuest = Mathf.Max(0, daysElapsedInCurrentQuest);
        questDebtPaymentTarget = Mathf.Max(0, questDebtPaymentTarget);
        questDebtPaidAmount = Mathf.Max(0, questDebtPaidAmount);
        specialDeliveryQuantity = Mathf.Max(0, specialDeliveryQuantity);
        specialDeliveryRequiredWeight = Mathf.Max(0, specialDeliveryRequiredWeight);
        specialDeliveryDebtReduction = Mathf.Max(0, specialDeliveryDebtReduction);
        campaignCompletionDebtAmount = Mathf.Max(0, campaignCompletionDebtAmount);
        defaultSpecialDeliveryQuantity = Mathf.Max(1, defaultSpecialDeliveryQuantity);
        debtFormulaBaseAmount = Mathf.Max(1, debtFormulaBaseAmount);
        debtFormulaProgressionDivisor = Mathf.Max(0.01f, debtFormulaProgressionDivisor);
        endlessSpecialDeliveryMaxChance = Mathf.Max(endlessSpecialDeliveryChanceIncrease, endlessSpecialDeliveryMaxChance);
        endlessSpecialDeliveryBaseQuantity = Mathf.Max(1, endlessSpecialDeliveryBaseQuantity);
        endlessSpecialDeliveryQuantityStep = Mathf.Max(1, endlessSpecialDeliveryQuantityStep);
        endlessSpecialDeliveryMinRarity = Mathf.Clamp(endlessSpecialDeliveryMinRarity, 1, 3);
        endlessSpecialDeliveryMaxRarity = Mathf.Clamp(endlessSpecialDeliveryMaxRarity, 1, 3);
        endlessDeliveriesWithoutSpecialRequest = Mathf.Max(0, endlessDeliveriesWithoutSpecialRequest);
    }

    private static CampaignQuestDefinition[] CreateDefaultQuestDefinitions()
    {
        return new[]
        {
            new CampaignQuestDefinition("Quest 1 - Tutorial", 3, 153, true, false, false, false),
            new CampaignQuestDefinition("Quest 2", 3, 162, false, false, false, false),
            new CampaignQuestDefinition("Quest 3", 3, 191, false, false, false, false),
            new CampaignQuestDefinition("Quest 4", 3, 239, false, false, false, false),
            new CampaignQuestDefinition("Quest 5 - Entrega especial", 3, 245, false, false, true, true, 2, 1)
        };
    }

    private void NotifyChanged()
    {
        OnProgressChanged?.Invoke();
    }
}
