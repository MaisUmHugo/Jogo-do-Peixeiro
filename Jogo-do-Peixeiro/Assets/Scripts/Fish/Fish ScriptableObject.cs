using UnityEngine;
using UnityEngine.Rendering;
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
    private const string PreviewLightRigName = "FishPreviewLightRig";
    private const string PreviewKeyLightName = "PreviewKeyLight";
    private const string PreviewFillLightName = "PreviewFillLight";
    private const string PreviewTopLightName = "PreviewTopLight";

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

    public static void ApplyPreviewLighting(Renderer _renderer)
    {
        if (_renderer == null)
            return;

        ConfigurePreviewRenderer(_renderer);

        FishVisualRuntimeModelHost runtimeHost = _renderer.GetComponent<FishVisualRuntimeModelHost>();

        if (runtimeHost != null)
            runtimeHost.ConfigurePreviewLighting(_renderer.gameObject.layer);

        EnsurePreviewLightRig(_renderer);
    }

    public static void ConfigurePreviewRenderer(Renderer _renderer)
    {
        if (_renderer == null)
            return;

        _renderer.shadowCastingMode = ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _renderer.lightProbeUsage = LightProbeUsage.Off;
        _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
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

    private static void EnsurePreviewLightRig(Renderer _renderer)
    {
        Transform parent = _renderer.transform.parent != null ? _renderer.transform.parent : _renderer.transform;
        FishPreviewLightRig lightRig = parent.GetComponentInChildren<FishPreviewLightRig>(true);

        if (lightRig == null)
        {
            GameObject lightRigObject = new GameObject(PreviewLightRigName);
            lightRigObject.transform.SetParent(parent, false);
            lightRig = lightRigObject.AddComponent<FishPreviewLightRig>();
        }

        lightRig.Configure(_renderer.gameObject.layer);
    }

    private static Light EnsurePreviewLight(Transform _parent, string _name, Quaternion _localRotation, float _intensity, Color _color, int _cullingMask)
    {
        Transform lightTransform = _parent.Find(_name);

        if (lightTransform == null)
        {
            GameObject lightObject = new GameObject(_name);
            lightObject.transform.SetParent(_parent, false);
            lightTransform = lightObject.transform;
        }

        lightTransform.localPosition = Vector3.zero;
        lightTransform.localRotation = _localRotation;
        lightTransform.localScale = Vector3.one;

        Light light = lightTransform.GetComponent<Light>();

        if (light == null)
            light = lightTransform.gameObject.AddComponent<Light>();

        light.type = LightType.Directional;
        light.intensity = _intensity;
        light.color = _color;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        light.cullingMask = _cullingMask;
        light.enabled = true;

        return light;
    }

    private class FishPreviewLightRig : MonoBehaviour
    {
        public void Configure(int _layer)
        {
            int cullingMask = 1 << _layer;

            EnsurePreviewLight(transform, PreviewKeyLightName, Quaternion.Euler(25f, -35f, 0f), 1.35f, Color.white, cullingMask);
            EnsurePreviewLight(transform, PreviewFillLightName, Quaternion.Euler(0f, 145f, 0f), 0.8f, new Color(0.85f, 0.92f, 1f, 1f), cullingMask);
            EnsurePreviewLight(transform, PreviewTopLightName, Quaternion.Euler(70f, 20f, 0f), 0.55f, new Color(1f, 0.94f, 0.86f, 1f), cullingMask);
        }
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
        SetLayerRecursively(runtimeModel, gameObject.layer);
        runtimeModel.SetActive(true);

        Renderer[] renderers = runtimeModel.GetComponentsInChildren<Renderer>(true);
        FishVisualUtility.ApplyMaterialToRenderers(_fish, renderers, _useSharedMaterial);
    }

    public void ConfigurePreviewLighting(int _layer)
    {
        if (runtimeModel == null)
            return;

        SetLayerRecursively(runtimeModel, _layer);

        Renderer[] renderers = runtimeModel.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
            FishVisualUtility.ConfigurePreviewRenderer(renderer);
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

    private static void SetLayerRecursively(GameObject _root, int _layer)
    {
        if (_root == null)
            return;

        _root.layer = _layer;

        Transform rootTransform = _root.transform;

        for (int i = 0; i < rootTransform.childCount; i++)
            SetLayerRecursively(rootTransform.GetChild(i).gameObject, _layer);
    }
}
