using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CampaignProgressSystem : MonoBehaviour
{
    public static CampaignProgressSystem Instance { get; private set; }

    [Header("Mode")]
    [SerializeField] private GameProgressMode gameMode = GameProgressMode.Campaign;
    [SerializeField] private bool endlessUnlocked;

    [Header("Campaign Quest")]
    [SerializeField, Min(1)] private int currentQuestIndex = 1;
    [SerializeField, Min(1)] private int questDurationDays = 3;
    [SerializeField, Min(0)] private int daysElapsedInCurrentQuest;
    [SerializeField, Min(0)] private int questDebtPaymentTarget = 150;
    [SerializeField, Min(0)] private int questDebtPaidAmount;
    [SerializeField] private bool hasFailedCurrentQuest;

    [Header("Special Money Lender Delivery")]
    [SerializeField] private bool isSpecialMoneyLenderDeliveryActive;
    [SerializeField] private FishScriptableObject specialDeliveryFish;
    [SerializeField, Min(0)] private int specialDeliveryQuantity;
    [SerializeField, Min(0)] private int specialDeliveryRequiredWeight;

    private DayCycle boundDayCycle;

    public event Action OnProgressChanged;
    public event Action OnQuestDeadlineExpired;

    public GameProgressMode GameMode => gameMode;
    public bool EndlessUnlocked => endlessUnlocked;
    public int CurrentQuestIndex => currentQuestIndex;
    public int QuestDurationDays => questDurationDays;
    public int DaysElapsedInCurrentQuest => daysElapsedInCurrentQuest;
    public int DaysRemainingInCurrentQuest => Mathf.Max(0, questDurationDays - daysElapsedInCurrentQuest);
    public int QuestDebtPaymentTarget => questDebtPaymentTarget;
    public int QuestDebtPaidAmount => questDebtPaidAmount;
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
        ClampState();
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

    public void RegisterDebtPayment(int _paidAmount, int _currentQuestTarget, int _nextQuestTarget)
    {
        if (gameMode != GameProgressMode.Campaign || hasFailedCurrentQuest)
            return;

        if (questDebtPaymentTarget <= 0)
            questDebtPaymentTarget = Mathf.Max(0, _currentQuestTarget);

        questDebtPaidAmount += Mathf.Max(0, _paidAmount);

        if (IsCurrentQuestPaymentComplete())
            AdvanceQuest(_nextQuestTarget);
        else
            NotifyChanged();
    }

    public void AdvanceQuest(int _nextQuestDebtPaymentTarget)
    {
        currentQuestIndex++;
        daysElapsedInCurrentQuest = 0;
        questDebtPaidAmount = 0;
        questDebtPaymentTarget = Mathf.Max(0, _nextQuestDebtPaymentTarget);
        hasFailedCurrentQuest = false;
        ClearSpecialDelivery();
        NotifyChanged();
    }

    public void SetSpecialDelivery(FishScriptableObject _fish, int _quantity, int _requiredWeight)
    {
        specialDeliveryFish = _fish;
        specialDeliveryQuantity = Mathf.Max(0, _quantity);
        specialDeliveryRequiredWeight = Mathf.Max(0, _requiredWeight);
        isSpecialMoneyLenderDeliveryActive = specialDeliveryFish != null && specialDeliveryQuantity > 0;
        NotifyChanged();
    }

    public void ClearSpecialDelivery()
    {
        isSpecialMoneyLenderDeliveryActive = false;
        specialDeliveryFish = null;
        specialDeliveryQuantity = 0;
        specialDeliveryRequiredWeight = 0;
    }

    public CampaignSaveData CaptureSaveData()
    {
        return new CampaignSaveData
        {
            gameMode = gameMode,
            currentQuestIndex = currentQuestIndex,
            questDurationDays = questDurationDays,
            daysElapsedInCurrentQuest = daysElapsedInCurrentQuest,
            questDebtPaymentTarget = questDebtPaymentTarget,
            questDebtPaidAmount = questDebtPaidAmount,
            hasFailedCurrentQuest = hasFailedCurrentQuest,
            endlessUnlocked = endlessUnlocked,
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

        gameMode = _data.gameMode;
        currentQuestIndex = _data.currentQuestIndex;
        questDurationDays = _data.questDurationDays;
        daysElapsedInCurrentQuest = _data.daysElapsedInCurrentQuest;
        questDebtPaymentTarget = _data.questDebtPaymentTarget;
        questDebtPaidAmount = _data.questDebtPaidAmount;
        hasFailedCurrentQuest = _data.hasFailedCurrentQuest;
        endlessUnlocked = _data.endlessUnlocked;
        isSpecialMoneyLenderDeliveryActive = _data.isSpecialMoneyLenderDeliveryActive;
        specialDeliveryFish = FishSaveResolver.FindFishById(_data.specialDeliveryFishId);
        specialDeliveryQuantity = _data.specialDeliveryQuantity;
        specialDeliveryRequiredWeight = _data.specialDeliveryRequiredWeight;

        if (specialDeliveryFish == null)
            ClearSpecialDelivery();

        ClampState();
        NotifyChanged();
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
        if (gameMode != GameProgressMode.Campaign || hasFailedCurrentQuest)
            return;

        daysElapsedInCurrentQuest++;

        if (daysElapsedInCurrentQuest >= questDurationDays && !IsCurrentQuestPaymentComplete())
        {
            hasFailedCurrentQuest = true;
            OnQuestDeadlineExpired?.Invoke();
        }

        NotifyChanged();
    }

    private bool IsCurrentQuestPaymentComplete()
    {
        return questDebtPaymentTarget <= 0 || questDebtPaidAmount >= questDebtPaymentTarget;
    }

    private void ClampState()
    {
        currentQuestIndex = Mathf.Max(1, currentQuestIndex);
        questDurationDays = Mathf.Max(1, questDurationDays);
        daysElapsedInCurrentQuest = Mathf.Max(0, daysElapsedInCurrentQuest);
        questDebtPaymentTarget = Mathf.Max(0, questDebtPaymentTarget);
        questDebtPaidAmount = Mathf.Max(0, questDebtPaidAmount);
        specialDeliveryQuantity = Mathf.Max(0, specialDeliveryQuantity);
        specialDeliveryRequiredWeight = Mathf.Max(0, specialDeliveryRequiredWeight);
    }

    private void NotifyChanged()
    {
        OnProgressChanged?.Invoke();
    }
}
