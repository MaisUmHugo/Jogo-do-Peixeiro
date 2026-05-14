using System.Collections.Generic;
using UnityEngine;

public static class FishSaveResolver
{
    public static FishScriptableObject FindFishById(string _fishId)
    {
        if (string.IsNullOrWhiteSpace(_fishId))
            return null;

        string normalizedId = NormalizeId(_fishId);
        HashSet<FishScriptableObject> candidates = new HashSet<FishScriptableObject>();

        foreach (FishingAreaDefinition area in Resources.FindObjectsOfTypeAll<FishingAreaDefinition>())
        {
            if (area == null || area.AvailableFish == null)
                continue;

            foreach (FishScriptableObject fish in area.AvailableFish)
            {
                if (fish != null)
                    candidates.Add(fish);
            }
        }

        foreach (FishScriptableObject fish in Resources.FindObjectsOfTypeAll<FishScriptableObject>())
        {
            if (fish != null)
                candidates.Add(fish);
        }

        foreach (FishScriptableObject fish in candidates)
        {
            if (NormalizeId(fish.SaveId) == normalizedId ||
                NormalizeId(fish.name) == normalizedId ||
                NormalizeId(fish.fishName) == normalizedId)
            {
                return fish;
            }
        }

        return null;
    }

    private static string NormalizeId(string _value)
    {
        return string.IsNullOrWhiteSpace(_value)
            ? string.Empty
            : _value.Trim().ToLowerInvariant();
    }
}
