using UnityEngine;

public static class SceneTransitionRequest
{
    private const string ArrivalPointIdKey = "SceneTransitionArrivalPointId";

    public static void RequestArrival(string _arrivalPointId)
    {
        if (string.IsNullOrWhiteSpace(_arrivalPointId))
            PlayerPrefs.DeleteKey(ArrivalPointIdKey);
        else
            PlayerPrefs.SetString(ArrivalPointIdKey, _arrivalPointId);

        PlayerPrefs.Save();
    }

    public static bool TryGetPendingArrivalId(out string _arrivalPointId)
    {
        _arrivalPointId = PlayerPrefs.GetString(ArrivalPointIdKey, string.Empty);
        return !string.IsNullOrWhiteSpace(_arrivalPointId);
    }

    public static void ClearPendingArrival()
    {
        PlayerPrefs.DeleteKey(ArrivalPointIdKey);
        PlayerPrefs.Save();
    }
}
