using System.Collections.Generic;
using UnityEngine;

public static class FishCaptureHistory
{
    private static readonly Dictionary<string, int> captureCounts = new Dictionary<string, int>();

    public static void RegisterCatch(FishScriptableObject _fish)
    {
        string fishId = GetFishId(_fish);

        if (string.IsNullOrEmpty(fishId))
            return;

        if (!captureCounts.ContainsKey(fishId))
            captureCounts[fishId] = 0;

        captureCounts[fishId]++;
    }

    public static void RegisterDiscovery(FishScriptableObject _fish)
    {
        string fishId = GetFishId(_fish);

        if (string.IsNullOrEmpty(fishId))
            return;

        if (!captureCounts.ContainsKey(fishId))
            captureCounts[fishId] = 0;
    }

    public static int GetCaptureCount(FishScriptableObject _fish)
    {
        string fishId = GetFishId(_fish);

        if (string.IsNullOrEmpty(fishId))
            return 0;

        return captureCounts.TryGetValue(fishId, out int count) ? count : 0;
    }

    public static bool IsDiscovered(FishScriptableObject _fish)
    {
        string fishId = GetFishId(_fish);

        if (string.IsNullOrEmpty(fishId))
            return false;

        return captureCounts.ContainsKey(fishId);
    }

    public static List<SavedFishCaptureData> CaptureSaveData()
    {
        List<SavedFishCaptureData> savedCaptures = new List<SavedFishCaptureData>();

        foreach (KeyValuePair<string, int> entry in captureCounts)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
                continue;

            savedCaptures.Add(new SavedFishCaptureData
            {
                fishId = entry.Key,
                captureCount = entry.Value
            });
        }

        return savedCaptures;
    }

    public static void ApplySaveData(List<SavedFishCaptureData> _savedCaptures)
    {
        captureCounts.Clear();

        if (_savedCaptures == null)
            return;

        foreach (SavedFishCaptureData savedCapture in _savedCaptures)
        {
            if (savedCapture == null || string.IsNullOrWhiteSpace(savedCapture.fishId))
                continue;

            FishScriptableObject fish = FishSaveResolver.FindFishById(savedCapture.fishId);
            string fishId = fish != null ? fish.SaveId : savedCapture.fishId;
            captureCounts[NormalizeId(fishId)] = Mathf.Max(0, savedCapture.captureCount);
        }
    }

    public static void Reset()
    {
        captureCounts.Clear();
    }

    private static string GetFishId(FishScriptableObject _fish)
    {
        if (_fish == null)
            return string.Empty;

        return NormalizeId(_fish.SaveId);
    }

    private static string NormalizeId(string _value)
    {
        return string.IsNullOrWhiteSpace(_value)
            ? string.Empty
            : _value.Trim().ToLowerInvariant();
    }
}
