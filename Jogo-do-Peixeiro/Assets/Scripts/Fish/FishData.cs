using UnityEngine;

public class FishData
{
    public FishScriptableObject typeOfFish;
    public int weight {  get; private set; }

    public int CalculatePrice()
    {

        return weight * typeOfFish.pricePerWeight;

    }

    public FishData(FishScriptableObject _typeOfFish)
    {
       
       typeOfFish = _typeOfFish;
       weight = Random.Range(typeOfFish.minWeight, typeOfFish.maxWeight);        

    }


}
