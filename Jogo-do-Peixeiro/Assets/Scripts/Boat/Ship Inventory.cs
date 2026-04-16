using System.Collections.Generic;
using UnityEngine;

public class ShipInventory : MonoBehaviour
{
    public List<FishData> ownedFish = new List<FishData>();

    [SerializeField] private float maxFishCapacity;

    private float currentFishWeight = 0f;
    private bool wasFullLastUpdate = false;

    public bool IsFull => currentFishWeight >= maxFishCapacity;

    public DebugShipInventory debugShipInventory;

    [SerializeField] private PlayerMoneyManager playerMoneyManager;


    public bool TryAddFish(FishData fish)
    {
        if (fish == null)
            return false;

        // Só bloqueia se já estiver cheio antes da adição.
        // Assim ainda permite passar do limite na última captura.
        if (IsFull)
        {
            Debug.Log($"Inventário cheio. Peso atual: {currentFishWeight} / {maxFishCapacity}");
            return false;
        }

        AddFish(fish);
        return true;
    }

    // Pode continuar existindo para usos futuros, mas não será usado
    // para bloquear a pescaria antes da última captura.
    public bool CanAddFish(FishData _fish)
    {
        if (_fish == null)
            return false;

        return currentFishWeight + _fish.weight <= maxFishCapacity;
    }

    private void AddFish(FishData _fish)
    {
        ownedFish.Add(_fish);
        AttFishWeight();
    }

    public bool TryPayFishWeight(int _weightFishPayment)
    {
        int fishWeight = 0;
        int fishIndex = -1;

        Debug.Log("tentou pagar peixe");

        foreach (FishData fish in ownedFish)
        {
            fishWeight += fish.weight;

            if (fishWeight >= _weightFishPayment)
            {
                fishIndex = ownedFish.IndexOf(fish);
                break;
            }
        }

        if (fishIndex != -1)
        {
            Debug.Log("conseguiu pagar o peixe");
            AttFishWeight();
            SellHalfPriceFish(fishIndex);
            SellRemainingFish(); 
            return true;
        }

        return false;
    }

    private void SellRemainingFish()
    {
        Debug.Log("tentando receber peixe");
        float money = 0;
        foreach (FishData _fish in ownedFish)
        {
            money += _fish.price;
        }

        ownedFish.Clear();
        Debug.Log($"dinheiro a receber: {money}");
        playerMoneyManager.ReciveMoney(money);
    }

    private void SellHalfPriceFish(int _fishIndex)
    {
        float money = 0;

        for (int i = 0; i < _fishIndex; i++)
        {

            money += ownedFish[i].price;

        }

        ownedFish.RemoveRange(0, _fishIndex + 1);
        money = money / 2;
        playerMoneyManager.ReciveMoney(money);

    }

    private void AttFishWeight()
    {
        currentFishWeight = 0f;

        foreach (FishData _fish in ownedFish)
        {
            currentFishWeight += _fish.weight;
        }

        if (debugShipInventory != null)
            debugShipInventory.AttFishDebugText();

        bool isFullNow = IsFull;

        if (isFullNow && !wasFullLastUpdate)
        {
            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning("Inventário cheio");
        }

        wasFullLastUpdate = isFullNow;
    }

    public float GetCurrentWeight()
    {
        return currentFishWeight;
    }

    public float GetMaxCapacity()
    {
        return maxFishCapacity;
    }

    private bool TryFindFish(FishScriptableObject _wantedFish, int _wantedQtt = 1)
    {
        int currentQtt = 0;

        foreach (FishData fish in ownedFish)
        {
            if (fish.typeOfFish == _wantedFish)
            {
                currentQtt++;

                if (currentQtt == _wantedQtt)
                    return true;
            }
        }

        return false;
    }

    public bool TryPaySpecificFish(FishScriptableObject _wantedFish, int _wantedQtt)
    {
        if (TryFindFish(_wantedFish, _wantedQtt))
        {
            for (int i = 0; i < _wantedQtt; i++)
            {
                ownedFish.RemoveAt(ownedFish.FindIndex(i => i.typeOfFish == _wantedFish));
            }

            AttFishWeight();
            return true;
        }

        return false;
    }
}