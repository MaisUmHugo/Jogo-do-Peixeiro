using UnityEngine;

public static class SceneTransitionRequest
{
    private const string ArrivalPointIdKey = "SceneTransitionArrivalPointId";

    private static string pendingArrivalPointId;

    public static void RequestArrival(string _arrivalPointId)
    {
        if (string.IsNullOrWhiteSpace(_arrivalPointId))
            pendingArrivalPointId = string.Empty;
        else
            pendingArrivalPointId = _arrivalPointId;

        ClearLegacyPendingArrival();
    }

    public static bool TryGetPendingArrivalId(out string _arrivalPointId)
    {
        _arrivalPointId = pendingArrivalPointId;
        return !string.IsNullOrWhiteSpace(_arrivalPointId);
    }

    public static void ClearPendingArrival()
    {
        pendingArrivalPointId = string.Empty;
        ClearLegacyPendingArrival();
    }

    private static void ClearLegacyPendingArrival()
    {
        if (!PlayerPrefs.HasKey(ArrivalPointIdKey))
            return;

        PlayerPrefs.DeleteKey(ArrivalPointIdKey);
        PlayerPrefs.Save();
    }
}
