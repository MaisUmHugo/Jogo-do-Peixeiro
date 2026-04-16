using UnityEngine;

public class FishData
{
    public FishScriptableObject typeOfFish { get; private set; }
    public int weight { get; private set; }
    public float price { get; private set; }

    private float CalculatePrice()
    {

        return weight * typeOfFish.pricePerWeight;

    }

    public FishData(FishScriptableObject _typeOfFish)
    {

        typeOfFish = _typeOfFish;
        weight = Random.Range(typeOfFish.minWeight, typeOfFish.maxWeight);
        price = CalculatePrice();
    }
}
