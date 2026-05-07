using UnityEngine;
using UnityEngine.VFX;

public class MoneyLender : MonoBehaviour
{
    [Header("Weight Payment")]
    [SerializeField] private int initialFishWeightPaid = 100;
    [SerializeField] private int fishWeightPaidIncremetion = 20;

    [Header("Debt Payment")]
    [SerializeField] private int initialDebtPayment = 150;
    [SerializeField] private int debtPaymentIncremetion = 75;

    [Header("Specific Fish Payment")]
    [SerializeField] private int qttSpecificFish;
    [SerializeField] private FishScriptableObject specificFish;

    [Header("References")]
    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;

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
        ResolveReferences();

        if (playerMoneyManager == null)
            return false;

        if (!playerMoneyManager.TrySpendMoney(currentDebtPayment))
        {
            Debug.Log("Dinheiro insuficiente para pagar a divida.");
            return false;
        }

        AdvancePaymentCycle();
        PlayFireworkVFX();

        Debug.Log("Pagou a divida em dinheiro.");
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
        currentDebtPayment = initialDebtPayment + debtPaymentIncremetion * timesPaid;
        OnNewDebtPayment?.Invoke(currentDebtPayment);
    }

    public bool TryGetSpecificFishPayment()
    {
        return TryGetSpecificFishPayment(specificFish, qttSpecificFish);
    }

    public bool TryGetSpecificFishPayment(FishScriptableObject _specificFish, int _specificFishQuantity, bool _useTutorialFireworks = false)
    {
        if (shipInventory == null || _specificFish == null)
            return false;

        if (!shipInventory.TryPaySpecificFish(_specificFish, _specificFishQuantity))
        {
            Debug.Log($"Não tem quantidade suficiente de {_specificFish.fishName}.");
            return false;
        }

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
