using UnityEngine;

public class MoneyLender : MonoBehaviour
{
    private int currentFishWeightPayment;
    private int initialFishWeightPaid = 100;
    private int fishWeightPaidIncremetion = 20;



    private int timesPaid = 0;

    [SerializeField] private ShipInventory shipInventory;

    public void TryGetFishWeightPayment()
    {
        if (shipInventory.TryPayFishWeight(currentFishWeightPayment))
        {
            GetFishWeightPayment();
        }
        else{

            Debug.Log("não tem peso de peixe o suficiente para pagar. Vamos ter que arrancar o dedo do seu filho.");

        }
    }

    private void GetFishWeightPayment()
    {


        CalculateNewPayment();

    }

    private void CalculateNewPayment()
    {

        currentFishWeightPayment = initialFishWeightPaid + fishWeightPaidIncremetion * timesPaid;

    }

    private void Awake()
    {
        CalculateNewPayment();
    }
}
