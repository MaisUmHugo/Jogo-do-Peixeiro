using UnityEngine;

public class MoneyLender : MonoBehaviour
{
    private int currentPayment = 0;
    private int initialPaid = 100;
    private int paidIncremetion = 20;
    private int timesPaid = 0;


    [SerializeField] private ShipInventory shipInventory;

    public void TryGetFishPayment(int _payment)
    {
        if (shipInventory.TryPayFish(_payment))
        {
            GetFishPayment();
        }
        else{

            Debug.Log("não conseguiu pagar ainda, ta na dívida ");

        }
    }

    private void GetFishPayment()
    {

        Debug.Log("foi pago, ta liberado pra comer puta");

    }

    private void NewPayment()
    {

        currentPayment = initialPaid + paidIncremetion * timesPaid;

    }

    private void Awake()
    {
        NewPayment();
        TryGetFishPayment(currentPayment);
    }
}
