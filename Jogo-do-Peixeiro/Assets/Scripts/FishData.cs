using System;
using UnityEngine;

public class FishData : MonoBehaviour
{
    public FishScriptableObject TypeOfFish;
    public float Weight {  get; private set; }

    private void Awake()
    {
        Weight = (float)Math.Round(UnityEngine.Random.Range(TypeOfFish.MinWeight, TypeOfFish.MaxWeight), 1);
    }

    public float CalculatePrice()
    {

        return Weight * TypeOfFish.PricePerWeght;

    }


}
