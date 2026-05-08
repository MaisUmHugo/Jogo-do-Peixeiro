using UnityEngine;

public class FishData
{
    public FishScriptableObject typeOfFish { get; private set; }
    public int weight { get; private set; }
    public int price { get; private set; }

    private int CalculatePrice()
    {

        return FishPriceCalculator.CalculatePrice(typeOfFish, weight);

    }

    public FishData(FishScriptableObject _typeOfFish)
    {

        typeOfFish = _typeOfFish;
        int minWeight = Mathf.Min(typeOfFish.minWeight, typeOfFish.maxWeight);
        int maxWeight = Mathf.Max(typeOfFish.minWeight, typeOfFish.maxWeight);
        weight = Random.Range(minWeight, maxWeight + 1);
        price = CalculatePrice();
    }
}
