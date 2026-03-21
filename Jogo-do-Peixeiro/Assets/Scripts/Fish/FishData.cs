using UnityEngine;

public class FishData
{
    public FishScriptableObject TypeOfFish;
    public int Weight {  get; private set; }

    public float CalculatePrice()
    {

        return Weight * TypeOfFish.PricePerWeight;

    }

    public FishData(FishScriptableObject typeOfFish)
    {
       
       TypeOfFish = typeOfFish;
       Weight = Random.Range(TypeOfFish.MinWeight, TypeOfFish.MaxWeight);        

    }


}
