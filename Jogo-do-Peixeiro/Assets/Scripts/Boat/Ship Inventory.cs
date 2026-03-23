using System.Collections.Generic;
using UnityEngine;

public class ShipInventory : MonoBehaviour
{
    private List<FishData> ownedFish = new List<FishData>();
    private float maxFishCapacity;
    private float currentFishWeight = 0;

    public bool TryAddFish(FishData fish)
    {
        if (currentFishWeight + fish.weight > maxFishCapacity)
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
            ownedFish.RemoveRange(0, fishIndex);
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
    }

    public float GetCurrentWeight()
    {
        return currentFishWeight;
    }

    public float GetMaxCapacity()
    {
        return maxFishCapacity;
    }

    private bool TryFindFish(FishScriptableObject _wantedFish)
    {

        foreach(FishData fish in ownedFish)
        {
            if (fish.typeOfFish == _wantedFish) { return true; }
        }

        return false;

    }

    public bool IsFull => currentFishWeight >= maxFishCapacity;
}