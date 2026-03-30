using System.Collections.Generic;
using UnityEngine;

public class ShipInventory : MonoBehaviour
{
    public List<FishData> ownedFish = new List<FishData>();
    [SerializeField] private float maxFishCapacity;
    private float currentFishWeight = 0f;
    public bool IsFull => currentFishWeight >= maxFishCapacity;

    public DebugShipInventory debugShipInventory;

    public bool TryAddFish(FishData fish)
    {
        if (fish == null)
            return false;

        if (IsFull)
        {
            Debug.Log($"Inventário cheio. Peso atual: {currentFishWeight} / {maxFishCapacity}");
            return false;
        }

        AddFish(fish);
        return true;
    }
    private void AddFish(FishData _fish)
    {
        ownedFish.Add(_fish);
        AttFishWeight();
    }

    public void SellAllFish()
    {
        int moneyToRecive = 0;

        foreach (FishData _fish in ownedFish)
        {
            moneyToRecive += _fish.CalculatePrice();
        }

        ownedFish.Clear();
        AttFishWeight();
    }

    public void SellFish(int _i)
    {
        //add money by fish.CalculatePrice();
        ownedFish.RemoveAt(_i);
        AttFishWeight();
    }

    public bool TryPayFishWeight(int _weightFishPayment)
    {
        int fishWeight = 0;
        int fishIndex = -1;

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
            ownedFish.RemoveRange(0, fishIndex + 1);
            AttFishWeight();
            return true;
        }

        return false;
    }

    private void AttFishWeight()
    {
        currentFishWeight = 0;

        foreach (FishData _fish in ownedFish)
        {
            currentFishWeight += _fish.weight;
        }

        if (debugShipInventory != null)
            debugShipInventory.AttFishDebugText();
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

                if (currentQtt == _wantedQtt) { return true; }

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

