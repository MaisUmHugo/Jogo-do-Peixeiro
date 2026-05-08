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

    public FishScriptableObject GetRandomFish(bool _onlyMoneyLenderRequestable = false)
    {
        if (!HasFishAvailable)
            return null;

        if (!_onlyMoneyLenderRequestable)
            return _availableFish[Random.Range(0, _availableFish.Length)];

        FishScriptableObject[] requestableFish = System.Array.FindAll(
            _availableFish,
            fish => fish != null && fish.CanBeRequestedByMoneyLender
        );

        if (requestableFish == null || requestableFish.Length == 0)
            return null;

        return requestableFish[Random.Range(0, requestableFish.Length)];
    }
}
