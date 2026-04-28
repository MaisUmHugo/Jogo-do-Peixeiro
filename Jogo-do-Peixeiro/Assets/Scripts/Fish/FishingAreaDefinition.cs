using UnityEngine;

[CreateAssetMenu(fileName = "Fishing Area", menuName = "Fishing/Fishing Area")]
public class FishingAreaDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _areaId = "lake_shallow";
    [SerializeField] private string _displayName = "Lago raso";

    [Header("Fish")]
    [SerializeField] private FishScriptableObject[] _availableFish;

    public string AreaId => _areaId;
    public string DisplayName => _displayName;
    public FishScriptableObject[] AvailableFish => _availableFish;
    public bool HasFishAvailable => _availableFish != null && _availableFish.Length > 0;

    public FishScriptableObject GetRandomFish()
    {
        if (!HasFishAvailable)
            return null;

        return _availableFish[Random.Range(0, _availableFish.Length)];
    }
}
