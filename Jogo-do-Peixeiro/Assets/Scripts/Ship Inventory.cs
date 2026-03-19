using System.Collections.Generic;
using UnityEngine;

public class ShipInventory : MonoBehaviour
{

    [SerializeField] private List<FishData> OwnedFish = new List<FishData>();
    private float MaxFishCapacity = 100;
    private float CurrentFishWeight = 0;
    private bool FullCapacity;

    public FishScriptableObject teste2;

    public void TryAddFish(FishData fish)
    {

        if (FullCapacity) { return; }

        AddFish(fish);

    }
   
    private void AddFish(FishData fish)
    {

        OwnedFish.Add(fish);
        CurrentFishWeight += fish.Weight;

        if (CurrentFishWeight >= MaxFishCapacity) { FullCapacity = true; }


    }

    public void SellAllFish()
    {

        foreach (FishData fish in OwnedFish)
        {

            // add money by fish.CalculatePrice();
            OwnedFish.Remove(fish);

        }
    }

    public void SellFish(int i)
    {

        //add money bey fish.CalculatePrice();
        OwnedFish.RemoveAt(i);

    }
}
