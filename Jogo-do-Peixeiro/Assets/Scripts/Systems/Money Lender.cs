using UnityEngine;

public class MoneyLender : MonoBehaviour
{
    private int currentFishWeightPayment;
    [SerializeField] private int initialFishWeightPaid = 100;
    [SerializeField] private int fishWeightPaidIncremetion = 20;

    [SerializeField] private int qttSpecificFish;
    [SerializeField] private FishScriptableObject specificFish;

    private int timesPaid = 0;

    [SerializeField] private ShipInventory shipInventory;

    public void TryGetFishWeightPayment()
    {
        if (shipInventory.TryPayFishWeight(currentFishWeightPayment))
        {
            GetFishWeightPayment();
            Debug.Log("Pagou o peso de Peixe");
        }
        else
        {

            Debug.Log("não tem peso de peixe o suficiente para pagar. Vamos ter que arrancar o dedo do seu filho.");

        }
    }

    private void GetFishWeightPayment()
    {




    }

    private void CalculateNewPayment()
    {

        currentFishWeightPayment = initialFishWeightPaid + fishWeightPaidIncremetion * timesPaid;

    }

    public void TryGetSpecificFishPayment()
    {
        if (shipInventory.TryPaySpecificFish(specificFish, qttSpecificFish))
        {

            GetSpecificFishPayment();
            Debug.Log($"Pagou a {specificFish.fishName}");

        }
        else
        {

            Debug.Log($"Não tem a quantidade de {specificFish.fishName} o suficiente, vamos tirar um dente da sua esposa.");

        }
    }

    private void GetSpecificFishPayment()
    {



    }

    private void Awake()
    {
        CalculateNewPayment();
    }
}
