using UnityEngine;

public class MoneyLender : MonoBehaviour
{
    [Header("Weight Payment")]
    [SerializeField] private int initialFishWeightPaid = 100;
    [SerializeField] private int fishWeightPaidIncremetion = 20;

    [Header("Specific Fish Payment")]
    [SerializeField] private int qttSpecificFish;
    [SerializeField] private FishScriptableObject specificFish;

    [Header("References")]
    [SerializeField] private ShipInventory shipInventory;

    private int currentFishWeightPayment;
    private int timesPaid = 0;

    private void Awake()
    {
        CalculateNewPayment();
    }

    public bool TryGetFishWeightPayment()
    {
        if (shipInventory == null)
            return false;

        if (shipInventory.TryPayFishWeight(currentFishWeightPayment))
        {
            GetFishWeightPayment();
            TutorialHandler.Instance.GoNextObjective();
            Debug.Log("Pagou o peso de peixe.");
            return true;
        }

        Debug.Log("Não tem peso de peixe suficiente para pagar.");
        return false;
    }

    private void GetFishWeightPayment()
    {
        timesPaid++;
        CalculateNewPayment();
    }

    private void CalculateNewPayment()
    {
        currentFishWeightPayment = initialFishWeightPaid + fishWeightPaidIncremetion * timesPaid;
    }

    public bool TryGetSpecificFishPayment()
    {
        if (shipInventory == null || specificFish == null)
            return false;

        if (shipInventory.TryPaySpecificFish(specificFish, qttSpecificFish))
        {
            GetSpecificFishPayment();
            Debug.Log($"Pagou {specificFish.fishName}.");
            return true;
        }

        Debug.Log($"Não tem quantidade suficiente de {specificFish.fishName}.");
        return false;
    }

    private void GetSpecificFishPayment()
    {
        timesPaid++;
        CalculateNewPayment();
    }

    public int GetCurrentFishWeightPayment()
    {
        return currentFishWeightPayment;
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
}