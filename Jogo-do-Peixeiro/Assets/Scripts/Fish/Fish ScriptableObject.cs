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
    [Tooltip("Opcional. Use para arrastar um prefab ou FBX inteiro. Se preenchido, tem prioridade sobre Mesh.")]
    public GameObject modelPrefab;
    public Vector3 modelLocalPosition;
    public Vector3 modelLocalEulerAngles;
    public Vector3 modelLocalScale = Vector3.one;
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
    public GameObject ModelPrefab => modelPrefab;
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

        FishVisualRuntimeModelHost runtimeHost = ResolveRuntimeHost(_meshFilter, _renderer);

        if (_fish.ModelPrefab != null)
        {
            if (_meshFilter != null)
                _meshFilter.sharedMesh = null;

            if (runtimeHost != null)
                runtimeHost.ApplyModelPrefab(_fish, _renderer, _useSharedMaterial);

            return;
        }

        if (runtimeHost != null)
            runtimeHost.ClearRuntimeModel(true);

        if (_meshFilter != null)
            _meshFilter.sharedMesh = _fish.mesh;

        ApplyMaterial(_fish, _renderer, _useSharedMaterial);
    }

    public static bool HasVisual(FishScriptableObject _fish)
    {
        return _fish != null && (_fish.ModelPrefab != null || _fish.mesh != null);
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

    public static void ApplyMaterialToRenderers(FishScriptableObject _fish, Renderer[] _renderers, bool _useSharedMaterial)
    {
        if (_renderers == null)
            return;

        foreach (Renderer renderer in _renderers)
            ApplyMaterial(_fish, renderer, _useSharedMaterial);
    }

    private static FishVisualRuntimeModelHost ResolveRuntimeHost(MeshFilter _meshFilter, Renderer _renderer)
    {
        Transform hostTransform = _renderer != null
            ? _renderer.transform
            : _meshFilter != null ? _meshFilter.transform : null;

        if (hostTransform == null)
            return null;

        FishVisualRuntimeModelHost runtimeHost = hostTransform.GetComponent<FishVisualRuntimeModelHost>();

        if (runtimeHost == null)
            runtimeHost = hostTransform.gameObject.AddComponent<FishVisualRuntimeModelHost>();

        return runtimeHost;
    }
}

public class FishVisualRuntimeModelHost : MonoBehaviour
{
    private GameObject runtimeModel;
    private GameObject currentModelPrefab;
    private Renderer placeholderRenderer;
    private bool hasStoredPlaceholderState;
    private bool placeholderRendererEnabled;

    public void ApplyModelPrefab(FishScriptableObject _fish, Renderer _placeholderRenderer, bool _useSharedMaterial)
    {
        if (_fish == null || _fish.ModelPrefab == null)
            return;

        StorePlaceholderRendererState(_placeholderRenderer);
        SetPlaceholderRendererVisible(false);

        if (runtimeModel == null || currentModelPrefab != _fish.ModelPrefab)
        {
            DestroyRuntimeModel();
            runtimeModel = Instantiate(_fish.ModelPrefab, transform);
            currentModelPrefab = _fish.ModelPrefab;
        }

        runtimeModel.transform.localPosition = _fish.modelLocalPosition;
        runtimeModel.transform.localRotation = Quaternion.Euler(_fish.modelLocalEulerAngles);
        runtimeModel.transform.localScale = _fish.modelLocalScale == Vector3.zero
            ? Vector3.one
            : _fish.modelLocalScale;
        runtimeModel.SetActive(true);

        Renderer[] renderers = runtimeModel.GetComponentsInChildren<Renderer>(true);
        FishVisualUtility.ApplyMaterialToRenderers(_fish, renderers, _useSharedMaterial);
    }

    public void ClearRuntimeModel(bool _restorePlaceholderRenderer)
    {
        DestroyRuntimeModel();
        currentModelPrefab = null;

        if (_restorePlaceholderRenderer)
            RestorePlaceholderRenderer();
    }

    private void StorePlaceholderRendererState(Renderer _placeholderRenderer)
    {
        if (_placeholderRenderer == null)
            return;

        if (placeholderRenderer != _placeholderRenderer)
        {
            placeholderRenderer = _placeholderRenderer;
            hasStoredPlaceholderState = false;
        }

        if (hasStoredPlaceholderState)
            return;

        placeholderRendererEnabled = placeholderRenderer.enabled;
        hasStoredPlaceholderState = true;
    }

    private void SetPlaceholderRendererVisible(bool _visible)
    {
        if (placeholderRenderer != null)
            placeholderRenderer.enabled = _visible;
    }

    private void RestorePlaceholderRenderer()
    {
        if (placeholderRenderer == null || !hasStoredPlaceholderState)
            return;

        placeholderRenderer.enabled = placeholderRendererEnabled;
    }

    private void DestroyRuntimeModel()
    {
        if (runtimeModel == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeModel);
        else
            DestroyImmediate(runtimeModel);

        runtimeModel = null;
    }
}
