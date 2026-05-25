using UnityEngine;
using UnityEngine.VFX;

public class MoneyLender : MonoBehaviour
{
    public enum DebtPaymentResult
    {
        Failed,
        Partial,
        MoneyTargetCompleted,
        Completed,
        PaidOff
    }

    [Header("Weight Payment")]
    [SerializeField] private int initialFishWeightPaid = 100;
    [SerializeField] private int fishWeightPaidIncremetion = 20;

    [Header("Debt Payment")]
    [SerializeField] private int initialDebtPayment = 150;
    [SerializeField] private int debtPaymentIncremetion = 75;
    [SerializeField] private int specificFishDebtReduction = 250;

    [Header("Specific Fish Payment")]
    [SerializeField] private int qttSpecificFish;
    [SerializeField] private FishScriptableObject specificFish;

    [Header("References")]
    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;
    [SerializeField] private DebtSystem debtSystem;

    [Header("Firework VFX")]
    [SerializeField] private VisualEffect fireworkVFXPrefab;
    [SerializeField] private Transform fireworkSpawnPoint;
    [SerializeField] private float fireworkVFXLifetime = 3f;
    [SerializeField] private bool useFireworkVFXPool = true;
    [SerializeField] private string fireworkVFXPoolKey = "FireworkVFX";
    [SerializeField, Min(1)] private int fireworkVFXPoolSize = 2;

    [Header("Quest Completion SFX")]
    [SerializeField, InspectorName("Quest Complete SFX")] private AudioClip fireworkSfx;
    [SerializeField, Range(0f, 1f), InspectorName("Quest Complete SFX Volume")] private float fireworkSfxVolume = 1f;

    private int currentFishWeightPayment;
    private int currentDebtPayment;
    private int currentDebtPaymentPaidAmount;
    private int timesPaid = 0;
    private bool tutorialFinishFireworksStarted = false;

    public int CurrentFishWeightPayment => currentFishWeightPayment;
    public int CurrentDebtPayment => currentDebtPayment;
    public int CurrentDebtBalance => debtSystem != null ? debtSystem.CurrentDebt : 0;

    public delegate void OnNewFishWeightPaymentDelegate(int fishWeightPayment);
    public event OnNewFishWeightPaymentDelegate OnNewFishWeightPayment;

    public delegate void OnNewDebtPaymentDelegate(int debtPayment);
    public event OnNewDebtPaymentDelegate OnNewDebtPayment;

    private void Awake()
    {
        ResolveReferences();
        CalculateNewPayment();
        CalculateNewDebtPayment();
        PrepareFireworkVFXPool();
    }

    private void OnEnable()
    {
        ResolveReferences();
        DebtSystem.OnDebtChangedEvent += HandleDebtChanged;
        CalculateNewDebtPayment();
    }

    private void OnDisable()
    {
        DebtSystem.OnDebtChangedEvent -= HandleDebtChanged;
    }

    public bool TryGetFishWeightPayment()
    {
        if (shipInventory == null)
            return false;

        if (!shipInventory.TryPayFishWeight(currentFishWeightPayment))
        {
            Debug.Log("Não tem peso de peixe suficiente para pagar.");
            return false;
        }

        GetFishWeightPayment();
        PlayFireworkVFX();

        Debug.Log("Pagou o peso de peixe.");
        return true;
    }

    public bool TryPayDebt()
    {
        return TryPayDebt(out int _, out DebtPaymentResult _);
    }

    public bool TryPayDebt(out int paidAmount, out DebtPaymentResult paymentResult)
    {
        ResolveReferences();
        paidAmount = 0;
        paymentResult = DebtPaymentResult.Failed;

        if (playerMoneyManager == null)
            return false;

        int paymentAmount = GetCurrentPayableDebtPayment();
        int questTargetBeforePayment = currentDebtPayment;
        CampaignProgressSystem campaignProgress = ResolveCampaignProgress();
        bool usesCampaignDebtPayment = ShouldUseCampaignDebtPayment(campaignProgress);
        int previousCampaignQuestIndex = usesCampaignDebtPayment ? campaignProgress.CurrentQuestIndex : 0;
        int previousCampaignRemaining = usesCampaignDebtPayment ? campaignProgress.QuestDebtPaymentRemaining : 0;

        if (paymentAmount <= 0)
        {
            Debug.Log("Dívida já está paga.");
            return false;
        }

        int availableMoney = Mathf.FloorToInt(playerMoneyManager.PlayerMoney);

        if (availableMoney <= 0)
        {
            Debug.Log("Dinheiro insuficiente para pagar a dívida.");
            return false;
        }

        paidAmount = Mathf.Min(paymentAmount, availableMoney);

        if (!playerMoneyManager.TrySpendMoney(paidAmount))
        {
            paidAmount = 0;
            Debug.Log("Dinheiro insuficiente para pagar a dívida.");
            return false;
        }

        ReduceDebt(paidAmount);

        if (usesCampaignDebtPayment)
        {
            campaignProgress.RegisterDebtPayment(paidAmount, questTargetBeforePayment, GetNextDebtPaymentFallbackTarget());

            bool completedCampaignPayment =
                campaignProgress.IsCampaignCompleted ||
                campaignProgress.CurrentQuestIndex != previousCampaignQuestIndex ||
                (previousCampaignRemaining > 0 && paidAmount >= previousCampaignRemaining);

            CalculateNewDebtPayment();

            if (completedCampaignPayment)
            {
                if (campaignProgress.CurrentQuestRequiresSpecialDelivery &&
                    !campaignProgress.IsCampaignCompleted &&
                    campaignProgress.CurrentQuestIndex == previousCampaignQuestIndex)
                {
                    paymentResult = DebtPaymentResult.MoneyTargetCompleted;
                    Debug.Log($"Pagou R$ {paidAmount} da meta em dinheiro. Falta a entrega especial.");
                    return true;
                }

                paymentResult = DebtPaymentResult.Completed;
                PlayFireworkVFX();
                Debug.Log($"Pagou R$ {paidAmount} da meta da quest.");
                return true;
            }

            paymentResult = DebtPaymentResult.Partial;
            Debug.Log($"Pagamento parcial de R$ {paidAmount} da meta da quest.");
            return true;
        }

        currentDebtPaymentPaidAmount += paidAmount;

        bool completedPayment = currentDebtPaymentPaidAmount >= currentDebtPayment;
        bool paidOff = debtSystem != null && !debtSystem.HasDebt;

        if (paidOff)
        {
            paymentResult = DebtPaymentResult.PaidOff;
            currentDebtPaymentPaidAmount = 0;
            CalculateNewDebtPayment();
            CampaignProgressSystem.GetOrCreate().RegisterDebtPayment(paidAmount, questTargetBeforePayment, currentDebtPayment);
            PlayFireworkVFX();
            Debug.Log($"Pagou R$ {paidAmount} e quitou a dívida.");
            return true;
        }

        if (completedPayment)
        {
            paymentResult = DebtPaymentResult.Completed;
            currentDebtPaymentPaidAmount = 0;
            AdvancePaymentCycle();
            CampaignProgressSystem.GetOrCreate().RegisterDebtPayment(paidAmount, questTargetBeforePayment, currentDebtPayment);
            PlayFireworkVFX();
        Debug.Log($"Pagou R$ {paidAmount} da dívida.");
            return true;
        }

        paymentResult = DebtPaymentResult.Partial;
        CalculateNewDebtPayment();
        CampaignProgressSystem.GetOrCreate().RegisterDebtPayment(paidAmount, questTargetBeforePayment, currentDebtPayment);
        Debug.Log($"Pagamento parcial de R$ {paidAmount} da dívida.");
        return true;
    }

    private void GetFishWeightPayment()
    {
        AdvancePaymentCycle();
    }

    private void CalculateNewPayment()
    {
        currentFishWeightPayment = initialFishWeightPaid + fishWeightPaidIncremetion * timesPaid;
        OnNewFishWeightPayment?.Invoke(currentFishWeightPayment);
    }

    private void CalculateNewDebtPayment()
    {
        CampaignProgressSystem campaignProgress = ResolveCampaignProgress();

        if (TryCalculateCampaignDebtPayment(campaignProgress))
            return;

        int baseDebtPayment = initialDebtPayment + debtPaymentIncremetion * timesPaid;
        currentDebtPayment = baseDebtPayment;

        if (debtSystem != null)
            currentDebtPayment = debtSystem.HasDebt ? Mathf.Min(baseDebtPayment, debtSystem.CurrentDebt) : 0;

        currentDebtPaymentPaidAmount = Mathf.Clamp(currentDebtPaymentPaidAmount, 0, currentDebtPayment);
        OnNewDebtPayment?.Invoke(GetCurrentPayableDebtPaymentWithoutRecalculate());
    }

    private bool TryCalculateCampaignDebtPayment(CampaignProgressSystem _campaignProgress)
    {
        if (!ShouldUseCampaignDebtPayment(_campaignProgress))
            return false;

        timesPaid = Mathf.Max(0, _campaignProgress.CurrentQuestIndex - 1);
        currentDebtPayment = Mathf.Max(0, _campaignProgress.QuestDebtPaymentTarget);
        currentDebtPaymentPaidAmount = Mathf.Clamp(_campaignProgress.QuestDebtPaidAmount, 0, currentDebtPayment);

        if (debtSystem != null)
        {
            if (!debtSystem.HasDebt)
            {
                currentDebtPayment = currentDebtPaymentPaidAmount;
            }
            else
            {
                int payableAmount = Mathf.Min(GetCurrentPayableDebtPaymentWithoutRecalculate(), debtSystem.CurrentDebt);
                currentDebtPayment = currentDebtPaymentPaidAmount + Mathf.Max(0, payableAmount);
            }
        }

        OnNewDebtPayment?.Invoke(GetCurrentPayableDebtPaymentWithoutRecalculate());
        return true;
    }

    private bool ShouldUseCampaignDebtPayment(CampaignProgressSystem _campaignProgress)
    {
        return _campaignProgress != null &&
               _campaignProgress.UsesQuestDebtPayment;
    }

    private CampaignProgressSystem ResolveCampaignProgress()
    {
        if (CampaignProgressSystem.Instance != null)
            return CampaignProgressSystem.Instance;

        return FindFirstObjectByType<CampaignProgressSystem>(FindObjectsInactive.Include);
    }

    private int GetNextDebtPaymentFallbackTarget()
    {
        int nextTimesPaid = Mathf.Max(0, timesPaid + 1);
        return Mathf.Max(0, initialDebtPayment + debtPaymentIncremetion * nextTimesPaid);
    }

    public bool TryGetSpecificFishPayment()
    {
        return TryGetSpecificFishPayment(specificFish, qttSpecificFish);
    }

    public bool TryGetSpecificFishPayment(FishScriptableObject _specificFish, int _specificFishQuantity, bool _useTutorialFireworks = false)
    {
        if (shipInventory == null || _specificFish == null || !_specificFish.CanBeRequestedByMoneyLender)
            return false;

        if (!shipInventory.TryPaySpecificFish(_specificFish, _specificFishQuantity))
        {
            Debug.Log($"Não tem quantidade suficiente de {_specificFish.fishName}.");
            return false;
        }

        bool handledByQuest = TryCompleteCampaignSpecialDelivery(_specificFish);

        if (!handledByQuest)
        {
            ReduceDebt(specificFishDebtReduction);
            GetSpecificFishPayment();
        }

        if (_useTutorialFireworks)
            StartTutorialFinishFireworks();
        else
            PlayFireworkVFX();

        Debug.Log($"Pagou {_specificFish.fishName}.");
        return true;
    }

    private void GetSpecificFishPayment()
    {
        AdvancePaymentCycle();
    }

    private bool TryCompleteCampaignSpecialDelivery(FishScriptableObject _deliveredFish)
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null ||
            !campaignProgress.CurrentQuestRequiresSpecialDelivery ||
            campaignProgress.SpecialDeliveryFish == null ||
            campaignProgress.SpecialDeliveryFish != _deliveredFish)
        {
            return false;
        }

        return campaignProgress.CompleteSpecialDeliveryQuest();
    }

    private void AdvancePaymentCycle()
    {
        timesPaid++;
        CalculateNewPayment();
        CalculateNewDebtPayment();
    }

    private void ResolveReferences()
    {
        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        if (debtSystem == null)
            debtSystem = DebtSystem.GetOrCreate();
    }

    private void HandleDebtChanged(int _currentDebt, int _changeAmount)
    {
        CalculateNewDebtPayment();
    }

    private void PlayFireworkVFX()
    {
        PlayFireworkSfx();

        if (fireworkVFXPrefab == null || fireworkSpawnPoint == null)
            return;

        VisualEffect instance = PooledVisualEffectUtility.Spawn(
            fireworkVFXPrefab,
            fireworkVFXPoolKey,
            fireworkSpawnPoint.position,
            fireworkSpawnPoint.rotation,
            null,
            useFireworkVFXPool,
            fireworkVFXPoolSize,
            fireworkVFXLifetime,
            true,
            out _
        );

        if (instance == null)
            return;

        instance.Reinit();
        instance.Play();
    }

    private void StartTutorialFinishFireworks()
    {
        if (tutorialFinishFireworksStarted)
            return;

        tutorialFinishFireworksStarted = true;
        PlayFireworkSfx();

        if (fireworkVFXPrefab == null || fireworkSpawnPoint == null)
            return;

        VisualEffect instance = PooledVisualEffectUtility.Spawn(
            fireworkVFXPrefab,
            fireworkVFXPoolKey,
            fireworkSpawnPoint.position,
            fireworkSpawnPoint.rotation,
            null,
            useFireworkVFXPool,
            fireworkVFXPoolSize,
            fireworkVFXLifetime,
            false,
            out _
        );

        if (instance == null)
            return;

        instance.Reinit();
        instance.Play();
    }

    public void PlayTutorialFinishFireworks()
    {
        StartTutorialFinishFireworks();
    }

    private void PlayFireworkSfx()
    {
        if (AudioManager.Instance == null || fireworkSfx == null)
            return;

        AudioManager.Instance.PlaySfx(fireworkSfx, fireworkSfxVolume);
    }

    private void PrepareFireworkVFXPool()
    {
        PooledVisualEffectUtility.EnsurePool(
            fireworkVFXPoolKey,
            fireworkVFXPrefab,
            fireworkVFXPoolSize,
            useFireworkVFXPool,
            true
        );
    }

    public int GetCurrentFishWeightPayment()
    {
        return currentFishWeightPayment;
    }

    public int GetCurrentDebtPayment()
    {
        return currentDebtPayment;
    }

    public int GetCurrentPayableDebtPayment()
    {
        ResolveReferences();
        CalculateNewDebtPayment();
        return GetCurrentPayableDebtPaymentWithoutRecalculate();
    }

    public int GetCurrentDebtBalance()
    {
        ResolveReferences();
        return debtSystem != null ? debtSystem.CurrentDebt : 0;
    }

    public int ReduceDebt(int _amount)
    {
        ResolveReferences();
        return debtSystem != null ? debtSystem.ReduceDebt(_amount) : 0;
    }

    public float GetCurrentOwnedWeight()
    {
        if (shipInventory == null)
            return 0f;

        return shipInventory.GetCurrentWeight();
    }

    public int GetTimesPaid()
    {
        return timesPaid;
    }

    public void SetPaymentCycle(int _timesPaid)
    {
        SetPaymentCycle(_timesPaid, 0);
    }

    public void SetPaymentCycle(int _timesPaid, int _currentDebtPaymentPaidAmount)
    {
        timesPaid = Mathf.Max(0, _timesPaid);
        currentDebtPaymentPaidAmount = Mathf.Max(0, _currentDebtPaymentPaidAmount);
        CalculateNewPayment();
        CalculateNewDebtPayment();
    }

    public int GetCurrentDebtPaymentPaidAmount()
    {
        return currentDebtPaymentPaidAmount;
    }

    private int GetCurrentPayableDebtPaymentWithoutRecalculate()
    {
        return Mathf.Max(0, currentDebtPayment - currentDebtPaymentPaidAmount);
    }

    public FishScriptableObject GetSpecificFish()
    {
        if (specificFish == null || !specificFish.CanBeRequestedByMoneyLender)
            return null;

        return specificFish;
    }

    public int GetSpecificFishQuantity()
    {
        return qttSpecificFish;
    }

    public void TryPayButton()
    {
        if (CampaignQuestGuidanceController.instance != null &&
            CampaignQuestGuidanceController.instance.ShouldHandleMoneyLenderPayment(this))
        {
            CampaignQuestGuidanceController.instance.TryDeliverRequestedFish(this);
            return;
        }

        TryPayDebt();
    }
}
