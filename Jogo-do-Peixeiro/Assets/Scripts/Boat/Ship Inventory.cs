using System.Collections.Generic;
using UnityEngine;

public class ShipInventory : MonoBehaviour
{

    [SerializeField] private List<FishData> ownedFish = new List<FishData>();
    private float maxFishCapacity = 100;
    private float currentFishWeight = 0;
    private bool fullCapacity;

    public void TryAddFish(FishData fish)
    {

        if (fullCapacity) { return; }

        AddFish(fish);

    }
   
    private void AddFish(FishData fish)
    {

        ownedFish.Add(fish);
        currentFishWeight += fish.weight;

        if (currentFishWeight >= maxFishCapacity) { fullCapacity = true; }

    }

    public void SellAllFish()
    {

        foreach (FishData fish in ownedFish)
        {

            // add money by fish.CalculatePrice();

        }
        
        ownedFish.Clear();
        maxFishCapacity = 0;
    }

    public void SellFish(int i)
    {

        //add money by fish.CalculatePrice();
        currentFishWeight -= ownedFish[i].weight;
        ownedFish.RemoveAt(i);

    }
}
