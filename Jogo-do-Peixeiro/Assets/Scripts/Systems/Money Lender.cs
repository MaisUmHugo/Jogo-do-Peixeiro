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

        bool completedPayment = paidAmount >= paymentAmount;
        bool paidOff = debtSystem != null && !debtSystem.HasDebt;

        if (paidOff)
        {
            paymentResult = DebtPaymentResult.PaidOff;
            CalculateNewDebtPayment();
            PlayFireworkVFX();
            Debug.Log($"Pagou R$ {paidAmount} e quitou a divida.");
            return true;
        }

        if (completedPayment)
        {
            paymentResult = DebtPaymentResult.Completed;
            AdvancePaymentCycle();
            PlayFireworkVFX();
            Debug.Log($"Pagou R$ {paidAmount} da divida.");
            return true;
        }

        paymentResult = DebtPaymentResult.Partial;
        CalculateNewDebtPayment();
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

        OnNewDebtPayment?.Invoke(currentDebtPayment);
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
        return currentDebtPayment;
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
