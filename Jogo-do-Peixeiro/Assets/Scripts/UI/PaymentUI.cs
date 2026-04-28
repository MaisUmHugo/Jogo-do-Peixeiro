using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PaymentUI : MonoBehaviour
{
    [Header("Texts References")]
    [SerializeField] private TMP_Text paymentText;

    [Header("Ship References")]
    [SerializeField] private ShipInventory shipInventory;
    private List<FishData> ownedFish = new List<FishData>();
    private float fishWeight;

    [Header("Lender References")]
    [SerializeField] private MoneyLender moneyLender;   
    private int currentFishWeightPayment = 0;    

    private void OnEnable()
    {
        // limpa a lista e adiciona os peixes da lista
        ownedFish.Clear();
        ownedFish.AddRange(shipInventory.OwnedFish);        
        fishWeight = shipInventory.GetCurrentWeight();

        shipInventory.OnFishListChange += ChangeFishList;
        moneyLender.OnNewFishWeightPayment += ChangePayment;
        SetPaymentTexts();
    }

    private void OnDisable()
    {
        moneyLender.OnNewFishWeightPayment -= ChangePayment;
        shipInventory.OnFishListChange -= ChangeFishList;
    }

    private void ChangePayment(int _amount)
    {        
        currentFishWeightPayment = _amount;
        SetPaymentTexts();
    }

    private void ChangeFishList(List<FishData> _fishList,float _fishWeight)
    {        
        ownedFish.Clear();
        ownedFish.AddRange(_fishList);       
        fishWeight = _fishWeight;
    }

    private void SetPaymentTexts()
    {
        string color = "green";

        if (currentFishWeightPayment > fishWeight) color = "red";

        paymentText.text = $"Pagamento: <color={color}>{fishWeight}</color> / {currentFishWeightPayment}";

    }
}
