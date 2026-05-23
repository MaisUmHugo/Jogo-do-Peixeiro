using UnityEngine;
using UnityEngine.Serialization;

public enum FishAvailabilityPeriod
{
    Any,
    Morning,
    Night,
    Day
}

[CreateAssetMenu(fileName = "Fish", menuName = "New Fish")]
public class FishScriptableObject : ScriptableObject
{
    public int minWeight;
    public int maxWeight;
    
    [Header("Model Visuals")]
    public Mesh mesh;
    public Material material;
    public Texture2D texture;

    [Header("Inventory UI")]
    public Sprite inventoryIcon;

    [Range(1, 4)] public int rarity;

    [Min(0)]
    [FormerlySerializedAs("pricePerWeight")]
    public int basePrice;
    public int BasePrice => basePrice;

    [Min(0f)] public float spawnWeight = 1f;
    public bool canBeRequestedByMoneyLender = true;
    public FishAvailabilityPeriod availabilityPeriod = FishAvailabilityPeriod.Any;

    public string fishName;
    [TextArea] public string description;

    public string SaveId => name;
    public Sprite InventoryIcon => inventoryIcon;
    public Texture2D FishTexture => texture;
    public bool CanBeRequestedByMoneyLender => canBeRequestedByMoneyLender;

    public bool IsAvailableAtHour(float _hour)
    {
        float hour = Mathf.Repeat(_hour, 24f);

        return availabilityPeriod switch
        {
            FishAvailabilityPeriod.Morning => hour >= 5f && hour < 12f,
            FishAvailabilityPeriod.Night => hour >= 18f || hour < 5f,
            FishAvailabilityPeriod.Day => hour >= 5f && hour < 18f,
            _ => true
        };
    }
}

public static class FishVisualUtility
{
    private static readonly int[] TexturePropertyIds =
    {
        Shader.PropertyToID("_BaseMap"),
        Shader.PropertyToID("_MainTex"),
        Shader.PropertyToID("_BaseColorMap")
    };

    public static void ApplyModel(FishScriptableObject _fish, MeshFilter _meshFilter, Renderer _renderer, bool _useSharedMaterial)
    {
        if (_fish == null)
            return;

        if (_meshFilter != null)
            _meshFilter.sharedMesh = _fish.mesh;

        ApplyMaterial(_fish, _renderer, _useSharedMaterial);
    }

    public static void ApplyMaterial(FishScriptableObject _fish, Renderer _renderer, bool _useSharedMaterial)
    {
        if (_renderer == null)
            return;

        if (_fish != null && _fish.material != null)
        {
            if (_useSharedMaterial)
                _renderer.sharedMaterial = _fish.material;
            else
                _renderer.material = _fish.material;
        }

        ApplyTexture(_renderer, _fish != null ? _fish.FishTexture : null);
    }

    public static void ApplyTexture(Renderer _renderer, Texture _texture)
    {
        if (_renderer == null)
            return;

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        if (_texture != null)
        {
            _renderer.GetPropertyBlock(propertyBlock);

            foreach (int propertyId in TexturePropertyIds)
                propertyBlock.SetTexture(propertyId, _texture);
        }

        _renderer.SetPropertyBlock(propertyBlock);
    }
}
