using UnityEngine;
using UnityEngine.VFX;

public class MoneyLender : MonoBehaviour
{
    public enum DebtPaymentResult
    {
        Failed,
        Partial,
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

        if (paymentAmount <= 0)
        {
            Debug.Log("Divida ja esta paga.");
            return false;
        }

        int availableMoney = Mathf.FloorToInt(playerMoneyManager.PlayerMoney);

        if (availableMoney <= 0)
        {
            Debug.Log("Dinheiro insuficiente para pagar a divida.");
            return false;
        }

        paidAmount = Mathf.Min(paymentAmount, availableMoney);

        if (!playerMoneyManager.TrySpendMoney(paidAmount))
        {
            paidAmount = 0;
            Debug.Log("Dinheiro insuficiente para pagar a divida.");
            return false;
        }

        ReduceDebt(paidAmount);
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
            Debug.Log($"Pagou R$ {paidAmount} e quitou a divida.");
            return true;
        }

        if (completedPayment)
        {
            paymentResult = DebtPaymentResult.Completed;
            currentDebtPaymentPaidAmount = 0;
            AdvancePaymentCycle();
            CampaignProgressSystem.GetOrCreate().RegisterDebtPayment(paidAmount, questTargetBeforePayment, currentDebtPayment);
            PlayFireworkVFX();
            Debug.Log($"Pagou R$ {paidAmount} da divida.");
            return true;
        }

        paymentResult = DebtPaymentResult.Partial;
        CalculateNewDebtPayment();
        CampaignProgressSystem.GetOrCreate().RegisterDebtPayment(paidAmount, questTargetBeforePayment, currentDebtPayment);
        Debug.Log($"Pagamento parcial de R$ {paidAmount} da divida.");
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
        int baseDebtPayment = initialDebtPayment + debtPaymentIncremetion * timesPaid;
        currentDebtPayment = baseDebtPayment;

        if (debtSystem != null)
            currentDebtPayment = debtSystem.HasDebt ? Mathf.Min(baseDebtPayment, debtSystem.CurrentDebt) : 0;

        currentDebtPaymentPaidAmount = Mathf.Clamp(currentDebtPaymentPaidAmount, 0, currentDebtPayment);
        OnNewDebtPayment?.Invoke(GetCurrentPayableDebtPaymentWithoutRecalculate());
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

        ReduceDebt(specificFishDebtReduction);
        GetSpecificFishPayment();
        TryCompleteCampaignSpecialDelivery(_specificFish);

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

    private void TryCompleteCampaignSpecialDelivery(FishScriptableObject _deliveredFish)
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null ||
            !campaignProgress.CurrentQuestRequiresSpecialDelivery ||
            campaignProgress.SpecialDeliveryFish == null ||
            campaignProgress.SpecialDeliveryFish != _deliveredFish)
        {
            return;
        }

        campaignProgress.CompleteSpecialDeliveryQuest();
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
        if (fireworkVFXPrefab == null || fireworkSpawnPoint == null)
            return;

        VisualEffect instance = Instantiate(
            fireworkVFXPrefab,
            fireworkSpawnPoint.position,
            fireworkSpawnPoint.rotation
        );

        instance.gameObject.SetActive(true);
        instance.Reinit();
        instance.Play();

        Destroy(instance.gameObject, fireworkVFXLifetime);
    }

    private void StartTutorialFinishFireworks()
    {
        if (tutorialFinishFireworksStarted)
            return;

        if (fireworkVFXPrefab == null || fireworkSpawnPoint == null)
            return;

        tutorialFinishFireworksStarted = true;

        VisualEffect instance = Instantiate(
            fireworkVFXPrefab,
            fireworkSpawnPoint.position,
            fireworkSpawnPoint.rotation
        );

        instance.gameObject.SetActive(true);
        instance.Reinit();
        instance.Play();
    }

    public void PlayTutorialFinishFireworks()
    {
        StartTutorialFinishFireworks();
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
        if (TutorialController.instance != null &&
            TutorialController.instance.ShouldHandleMoneyLenderPayment(this))
        {
            TutorialController.instance.TryDeliverRequestedFish(this);
            return;
        }

        TryPayDebt();
    }
}
