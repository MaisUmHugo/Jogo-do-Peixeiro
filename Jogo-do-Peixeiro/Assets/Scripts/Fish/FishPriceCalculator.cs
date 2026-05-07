using UnityEngine;

public static class FishPriceCalculator
{
    public static int CalculatePrice(FishData fish)
    {
        if (fish == null)
            return 0;

        return CalculatePrice(fish.typeOfFish, fish.weight);
    }

    public static int CalculatePrice(FishScriptableObject fishType, int weight)
    {
        if (fishType == null)
            return 0;

        float rarityMultiplier = GetRarityMultiplier(fishType.rarity);
        float rawPrice = ((fishType.BasePrice + Mathf.Max(0, weight)) * rarityMultiplier) / 2f;
        return Mathf.CeilToInt(rawPrice);
    }

    public static float GetRarityMultiplier(int rarity)
    {
        return rarity switch
        {
            1 => 1.5f,
            2 => 2.5f,
            3 => 4f,
            4 => 6.7f,
            _ => 1f
        };
    }
}
