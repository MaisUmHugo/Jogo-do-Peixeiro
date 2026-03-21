using System.Collections.Generic;
using UnityEngine;

public class ShipInventory : MonoBehaviour
{

    [SerializeField] private List<FishData> ownedFish = new List<FishData>();
    private float maxFishCapacity = 100;
    private float currentFishWeight = 0;
    private bool fullCapacity;

    public bool TryAddFish(FishData fish)
    {

        if (fullCapacity) { return false; }

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

    public bool TryPayFish(int _weightFishPayment)
    {

        int fishWeight = 0;
        int fishIndex = -1;

        foreach (FishData fish in ownedFish) { 

            fishWeight += fish.weight;

            if (fishWeight >= _weightFishPayment) { 
            
                fishIndex = ownedFish.IndexOf(fish);
                break;

            }

        }

        if (fishIndex != -1){

            ownedFish.RemoveRange(0, fishIndex + 1);
            AttFishWeight();
            return true;

        }

        return false;

    }

    private void AttFishWeight()
    {
        fullCapacity = false;
        currentFishWeight = 0;

        foreach (FishData _fish in ownedFish) { 
        
            currentFishWeight += _fish.weight;
        
        }

        if (currentFishWeight >= maxFishCapacity) { 
        
            fullCapacity = true;

        }

    }
}
