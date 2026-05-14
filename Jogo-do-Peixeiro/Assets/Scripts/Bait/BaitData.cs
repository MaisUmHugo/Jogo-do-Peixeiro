using UnityEngine;

[CreateAssetMenu(fileName = "Bait", menuName = "Fishing/Bait")]
public class BaitData : ScriptableObject
{
    [SerializeField] private string saveId;
    [SerializeField] private string baitName = "Isca";
    [TextArea, SerializeField] private string description;
    [SerializeField] private Sprite inventoryIcon;
    [SerializeField, Min(0)] private int purchasePrice = 20;
    [SerializeField, Min(1)] private int purchaseQuantity = 1;

    [Header("Fishing Bonus")]
    [SerializeField, Range(0.1f, 2f)] private float biteDelayMultiplier = 1f;
    [SerializeField, Range(0.1f, 3f)] private float catchProgressMultiplier = 1f;
    [SerializeField, Range(0.1f, 2f)] private float skillCheckIndicatorSpeedMultiplier = 1f;
    [SerializeField, Range(0.1f, 3f)] private float skillCheckSuccessZoneMultiplier = 1f;
    [SerializeField, Range(0.1f, 3f)] private float directionChangeIntervalMultiplier = 1f;

    public string SaveId => string.IsNullOrWhiteSpace(saveId) ? name : saveId;
    public string BaitName => string.IsNullOrWhiteSpace(baitName) ? name : baitName;
    public string Description => description;
    public Sprite InventoryIcon => inventoryIcon;
    public int PurchasePrice => purchasePrice;
    public int PurchaseQuantity => purchaseQuantity;
    public float BiteDelayMultiplier => biteDelayMultiplier;
    public float CatchProgressMultiplier => catchProgressMultiplier;
    public float SkillCheckIndicatorSpeedMultiplier => skillCheckIndicatorSpeedMultiplier;
    public float SkillCheckSuccessZoneMultiplier => skillCheckSuccessZoneMultiplier;
    public float DirectionChangeIntervalMultiplier => directionChangeIntervalMultiplier;

    public void InitializeRuntime(
        string _saveId,
        string _baitName,
        string _description,
        int _purchasePrice,
        int _purchaseQuantity,
        float _biteDelayMultiplier,
        float _catchProgressMultiplier,
        float _skillCheckIndicatorSpeedMultiplier,
        float _skillCheckSuccessZoneMultiplier,
        float _directionChangeIntervalMultiplier)
    {
        saveId = _saveId;
        baitName = _baitName;
        description = _description;
        purchasePrice = Mathf.Max(0, _purchasePrice);
        purchaseQuantity = Mathf.Max(1, _purchaseQuantity);
        biteDelayMultiplier = Mathf.Clamp(_biteDelayMultiplier, 0.1f, 2f);
        catchProgressMultiplier = Mathf.Clamp(_catchProgressMultiplier, 0.1f, 3f);
        skillCheckIndicatorSpeedMultiplier = Mathf.Clamp(_skillCheckIndicatorSpeedMultiplier, 0.1f, 2f);
        skillCheckSuccessZoneMultiplier = Mathf.Clamp(_skillCheckSuccessZoneMultiplier, 0.1f, 3f);
        directionChangeIntervalMultiplier = Mathf.Clamp(_directionChangeIntervalMultiplier, 0.1f, 3f);
        name = _saveId;
    }

    private void OnValidate()
    {
        purchasePrice = Mathf.Max(0, purchasePrice);
        purchaseQuantity = Mathf.Max(1, purchaseQuantity);
        biteDelayMultiplier = Mathf.Clamp(biteDelayMultiplier, 0.1f, 2f);
        catchProgressMultiplier = Mathf.Clamp(catchProgressMultiplier, 0.1f, 3f);
        skillCheckIndicatorSpeedMultiplier = Mathf.Clamp(skillCheckIndicatorSpeedMultiplier, 0.1f, 2f);
        skillCheckSuccessZoneMultiplier = Mathf.Clamp(skillCheckSuccessZoneMultiplier, 0.1f, 3f);
        directionChangeIntervalMultiplier = Mathf.Clamp(directionChangeIntervalMultiplier, 0.1f, 3f);
    }
}
