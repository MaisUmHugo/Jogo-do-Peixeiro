using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

public class FishingSpot : MonoBehaviour, IPoolable
{
    [Header("Área")]
    [SerializeField] private FishingAreaDefinition fishingArea;
    [SerializeField] private FishScriptableObject[] availableFish;
    [Tooltip("Somente visualização: mostra os peixes que serão usados, vindos da Fishing Area ou do fallback Available Fish.")]
    [SerializeField] private FishScriptableObject[] effectiveAvailableFishPreview;
    [SerializeField] private string effectiveFishingAreaPreview;
    [TextArea, SerializeField] private string effectiveAvailableFishNamesPreview;
    [Tooltip("Quando ligado, o spot some depois de uma pescaria resolvida com mordida/captura. Cancelar antes da mordida não consome o spot.")]
    [SerializeField] private bool deactivateAfterFishingStarts;

    [SerializeField] private float minHorizontalDistance = 4f;

    [Header("Fish Escape")]
    [SerializeField] private bool ignoreEscapeForDebug;
    [SerializeField] private string boatTag = "Boat";
    [SerializeField] private string escapeWarningMessage = "Os peixes fugiram";

    [Header("Boat Escape")]
    [Tooltip("Distância horizontal até o centro/boia em que os peixes fogem. A área de interação continua pescável.")]
    [SerializeField] private float escapeDistance = 1.5f;
    [SerializeField, HideInInspector] private float escapeFadeDuration = 0.18f;

    [Header("Cast Target")]
    [SerializeField] private Transform castTargetPoint;

    [Header("Área VFX")]
    [SerializeField] private GameObject areaVFXObject;
    [SerializeField] private GameObject areaVFXPrefab;
    [SerializeField] private Transform areaVFXPoint;
    [SerializeField] private bool alignAreaVFXToWaterSurface = true;
    [SerializeField] private bool alignAreaVFXOnlyInDeepArea = true;
    [Tooltip("Offset aplicado depois de alinhar o Area VFX na superfície da água.")]
    [SerializeField] private float areaVFXWaterYOffset;
    [Tooltip("Offset extra aplicado somente em área profunda/dark water. Use valor negativo para baixar o VFX.")]
    [SerializeField] private float deepAreaVFXWaterYOffset = -0.25f;
    [SerializeField] private bool parentAreaVFXToSpot = true;
    [SerializeField] private bool playAreaVFXOnEnable = true;
    [SerializeField] private bool updateAreaVFXPositionEveryFrame = true;

    [Header("Debug Gizmos")]
    [SerializeField] private bool showEscapeAreaGizmo = true;
    [SerializeField] private Color escapeAreaGizmoColor = new Color(1f, 0.25f, 0.1f, 0.85f);

    private FishingSpotSpawner spawner;
    private Transform boatTransform;
    private Collider[] boatColliders;
    private Vector3 originalScale;
    private bool hasDeactivated;
    private bool isEscaping;
    private bool isWaitingFishingResult;
    private GameObject areaVFXInstance;
    private Coroutine escapeFadeRoutine;

    public FishingAreaDefinition FishingArea => fishingArea;
    public bool IsLockedByFishing => ShouldHoldSpotDuringFishing();

    private void Awake()
    {
        originalScale = transform.localScale;
        RefreshEffectiveAvailableFishPreview();
    }

    private void OnValidate()
    {
        minHorizontalDistance = Mathf.Max(0f, minHorizontalDistance);
        escapeDistance = Mathf.Max(0f, escapeDistance);
        escapeFadeDuration = Mathf.Max(0.01f, escapeFadeDuration);
        RefreshEffectiveAvailableFishPreview();
    }

    private void OnEnable()
    {
        ResetRuntimeState();
        TryFindBoatTransform();
        SetAreaVFXVisible(playAreaVFXOnEnable);
    }

    private void OnDisable()
    {
        UnregisterFishingResult();
        SetAreaVFXVisible(false);
    }

    private void Update()
    {
        UpdateBoatEscapeDistance();
        UpdateAreaVFXPositionForLiveTuning();
    }

    public void Initialize(FishingAreaDefinition _fishingArea, FishingSpotSpawner _spawner, bool _deactivateAfterFishingStarts)
    {
        fishingArea = _fishingArea;
        spawner = _spawner;
        deactivateAfterFishingStarts = _deactivateAfterFishingStarts;
        ResetRuntimeState();
        RefreshEffectiveAvailableFishPreview();
        SetAreaVFXVisible(playAreaVFXOnEnable);
    }

    public void OnSpawnFromPool()
    {
        ResetRuntimeState();
    }

    public void OnReturnToPool()
    {
        UnregisterFishingResult();

        if (escapeFadeRoutine != null)
        {
            StopCoroutine(escapeFadeRoutine);
            escapeFadeRoutine = null;
        }

        isEscaping = false;
        isWaitingFishingResult = false;
        boatTransform = null;
        boatColliders = null;
        transform.localScale = originalScale == Vector3.zero ? transform.localScale : originalScale;
        SetAreaVFXVisible(false);
    }

    public bool TryStartFishingFromRod(ShipInventory _inventory)
    {
        if (!CanStartFishing(_inventory, true))
            return false;

        FishScriptableObject[] fishList = GetAvailableFish();
        bool startedFishing = FishingManager.instance.StartFishing(_inventory, fishList);

        if (startedFishing && deactivateAfterFishingStarts)
            RegisterFishingResult();

        return startedFishing;
    }

    public bool CanStartFishingFromInteraction(ShipInventory _inventory)
    {
        return CanStartFishing(_inventory, false);
    }

    private bool CanStartFishing(ShipInventory _inventory, bool _triggerEscapeIfBlocked)
    {
        if (hasDeactivated || isEscaping || !isActiveAndEnabled)
            return false;

        if (FishingManager.instance == null)
            return false;

        if (_inventory == null)
            return false;

        if (GameManager.instance == null)
            return false;

        if (!HasFishAvailable())
            return false;

        Vector3 fishingReferencePosition = GetFishingReferencePosition(_inventory);

        Vector3 a = fishingReferencePosition;
        Vector3 b = transform.position;

        a.y = 0f;
        b.y = 0f;

        float horizontalDistance = Vector3.Distance(a, b);

        if (horizontalDistance < minHorizontalDistance)
        {
            if (_triggerEscapeIfBlocked)
                EscapeFishIfFishingWouldBeBlocked(horizontalDistance);

            return false;
        }

        return true;
    }

    public FishScriptableObject[] GetAvailableFish()
    {
        return ResolveAvailableFish();
    }

    public bool HasFishAvailable()
    {
        if (fishingArea != null)
            return fishingArea.HasFishAvailable;

        return availableFish != null && availableFish.Length > 0;
    }

    public Vector3 GetCastTargetPosition(Vector3 _fallbackPosition)
    {
        if (castTargetPoint != null)
            return castTargetPoint.position;

        return transform.position;
    }

    public Transform GetMarkerTarget()
    {
        return castTargetPoint != null ? castTargetPoint : transform;
    }

    private void OnTriggerEnter(Collider _other)
    {
        if (ignoreEscapeForDebug)
            return;

        Transform detectedBoatTransform = GetBoatTransformFromCollider(_other);

        if (detectedBoatTransform == null)
            return;

        CacheBoatTransform(detectedBoatTransform);
    }

    private Transform GetBoatTransformFromCollider(Collider _collider)
    {
        if (_collider == null)
            return null;

        Transform current = _collider.transform;

        while (current != null)
        {
            BoatController boatController = current.GetComponent<BoatController>();

            if (boatController != null)
                return boatController.transform;

            if (!string.IsNullOrWhiteSpace(boatTag) && current.CompareTag(boatTag))
                return current;

            current = current.parent;
        }

        return null;
    }

    private void EscapeFish()
    {
        if (hasDeactivated || isEscaping)
            return;

        if (HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(escapeWarningMessage);

        StartEscapeFade();
    }

    private void RegisterFishingResult()
    {
        if (FishingManager.instance == null)
            return;

        UnregisterFishingResult();
        isWaitingFishingResult = true;
        FishingManager.instance.FishingEnded += HandleFishingEnded;
    }

    private void UnregisterFishingResult()
    {
        if (!isWaitingFishingResult)
            return;

        if (FishingManager.instance != null)
            FishingManager.instance.FishingEnded -= HandleFishingEnded;

        isWaitingFishingResult = false;
    }

    private void HandleFishingEnded(bool _success, bool _hadFishBitten)
    {
        UnregisterFishingResult();

        if (hasDeactivated)
            return;

        if (_success || _hadFishBitten)
            DeactivateSpot();
    }

    private void UpdateBoatEscapeDistance()
    {
        if (ignoreEscapeForDebug || hasDeactivated || isEscaping || ShouldHoldSpotDuringFishing())
            return;

        if (boatTransform == null && !TryFindBoatTransform())
            return;

        float horizontalDistance = GetBoatEscapeDistance(GetEscapeReferencePosition());

        if (horizontalDistance <= GetEffectiveEscapeDistance())
            EscapeFish();
    }

    private void EscapeFishIfFishingWouldBeBlocked(float _horizontalDistance)
    {
        if (ignoreEscapeForDebug || hasDeactivated || ShouldHoldSpotDuringFishing())
            return;

        if (_horizontalDistance <= GetEffectiveEscapeDistance())
            EscapeFish();
    }

    private bool ShouldHoldSpotDuringFishing()
    {
        return isWaitingFishingResult ||
               (FishingManager.instance != null && FishingManager.instance.IsFishing);
    }

    private float GetEffectiveEscapeDistance()
    {
        return escapeDistance;
    }

    private Vector3 GetEscapeReferencePosition()
    {
        if (areaVFXPoint != null)
            return areaVFXPoint.position;

        if (areaVFXObject != null)
            return areaVFXObject.transform.position;

        return transform.position;
    }

    private FishScriptableObject[] ResolveAvailableFish()
    {
        if (fishingArea != null && fishingArea.HasFishAvailable)
            return fishingArea.AvailableFish;

        return availableFish;
    }

    [ContextMenu("Refresh Effective Available Fish Preview")]
    private void RefreshEffectiveAvailableFishPreview()
    {
        effectiveAvailableFishPreview = ResolveAvailableFish();
        effectiveFishingAreaPreview = fishingArea != null
            ? GetAreaPreviewName(fishingArea)
            : "Fallback Available Fish";
        effectiveAvailableFishNamesPreview = BuildFishNamesPreview(effectiveAvailableFishPreview);
    }

    private static string GetAreaPreviewName(FishingAreaDefinition _area)
    {
        if (_area == null)
            return "Sem área";

        if (!string.IsNullOrWhiteSpace(_area.DisplayName))
            return _area.DisplayName;

        if (!string.IsNullOrWhiteSpace(_area.AreaId))
            return _area.AreaId;

        return _area.name;
    }

    private static string BuildFishNamesPreview(FishScriptableObject[] _fishList)
    {
        if (_fishList == null || _fishList.Length == 0)
            return "Nenhum peixe disponível";

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < _fishList.Length; i++)
        {
            FishScriptableObject fish = _fishList[i];

            if (i > 0)
                builder.AppendLine();

            if (fish == null)
            {
                builder.Append("- Null");
                continue;
            }

            string fishName = !string.IsNullOrWhiteSpace(fish.fishName)
                ? fish.fishName
                : fish.name;

            builder.Append("- ");
            builder.Append(fishName);
        }

        return builder.ToString();
    }

    private void StartEscapeFade()
    {
        isEscaping = true;

        if (escapeFadeDuration <= 0f)
        {
            DeactivateSpot();
            return;
        }

        if (escapeFadeRoutine != null)
            StopCoroutine(escapeFadeRoutine);

        escapeFadeRoutine = StartCoroutine(EscapeFadeRoutine());
    }

    private IEnumerator EscapeFadeRoutine()
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < escapeFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / escapeFadeDuration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        escapeFadeRoutine = null;
        DeactivateSpot();
    }

    private Vector3 GetFishingReferencePosition(ShipInventory _inventory)
    {
        if (boatTransform == null)
            TryFindBoatTransform();

        if (boatTransform != null)
            return boatTransform.position;

        return _inventory != null ? _inventory.transform.position : transform.position;
    }

    private bool TryFindBoatTransform()
    {
        BoatController boatController = FindFirstObjectByType<BoatController>(FindObjectsInactive.Include);

        if (boatController != null)
        {
            CacheBoatTransform(boatController.transform);
            return true;
        }

        if (string.IsNullOrWhiteSpace(boatTag))
            return false;

        try
        {
            GameObject boatObject = GameObject.FindGameObjectWithTag(boatTag);

            if (boatObject == null)
                return false;

            CacheBoatTransform(boatObject.transform);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private void CacheBoatTransform(Transform _boatTransform)
    {
        if (_boatTransform == null)
            return;

        boatTransform = _boatTransform;
        boatColliders = boatTransform.GetComponentsInChildren<Collider>(true);
    }

    private float GetBoatEscapeDistance(Vector3 _escapePosition)
    {
        if (boatTransform == null)
            return float.MaxValue;

        if (boatColliders == null || boatColliders.Length == 0)
            boatColliders = boatTransform.GetComponentsInChildren<Collider>(true);

        float bestDistance = float.MaxValue;

        if (boatColliders != null)
        {
            for (int i = 0; i < boatColliders.Length; i++)
            {
                Collider boatCollider = boatColliders[i];

                if (boatCollider == null ||
                    !boatCollider.enabled ||
                    !boatCollider.gameObject.activeInHierarchy ||
                    boatCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                Vector3 closestPoint = boatCollider.ClosestPoint(_escapePosition);
                float distance = GetHorizontalDistance(closestPoint, _escapePosition);

                if (distance < bestDistance)
                    bestDistance = distance;
            }
        }

        if (bestDistance < float.MaxValue)
            return bestDistance;

        return GetHorizontalDistance(boatTransform.position, _escapePosition);
    }

    private float GetHorizontalDistance(Vector3 _a, Vector3 _b)
    {
        _a.y = 0f;
        _b.y = 0f;
        return Vector3.Distance(_a, _b);
    }

    private void SetAreaVFXVisible(bool _visible)
    {
        GameObject vfxObject = _visible ? GetAreaVFXObject() : GetExistingAreaVFXObject();

        if (vfxObject == null)
            return;

        vfxObject.SetActive(_visible);

        if (!_visible)
            return;

        UpdateAreaVFXPosition(vfxObject);
        PlayAreaVFX(vfxObject);
    }

    private GameObject GetAreaVFXObject()
    {
        if (areaVFXObject != null)
            return areaVFXObject;

        if (areaVFXInstance != null)
            return areaVFXInstance;

        if (areaVFXPrefab == null)
            return null;

        Transform spawnPoint = areaVFXPoint != null ? areaVFXPoint : transform;
        Transform parent = parentAreaVFXToSpot ? spawnPoint : null;
        areaVFXInstance = Instantiate(areaVFXPrefab, spawnPoint.position, spawnPoint.rotation, parent);

        UpdateAreaVFXPosition(areaVFXInstance);
        return areaVFXInstance;
    }

    private void UpdateAreaVFXPositionForLiveTuning()
    {
        if (!updateAreaVFXPositionEveryFrame)
            return;

        GameObject vfxObject = GetExistingAreaVFXObject();

        if (vfxObject == null || !vfxObject.activeInHierarchy)
            return;

        UpdateAreaVFXPosition(vfxObject);
    }

    private GameObject GetExistingAreaVFXObject()
    {
        if (areaVFXObject != null)
            return areaVFXObject;

        return areaVFXInstance;
    }

    private void UpdateAreaVFXPosition(GameObject _vfxObject)
    {
        if (_vfxObject == null)
            return;

        Transform spawnPoint = areaVFXPoint != null ? areaVFXPoint : transform;
        Vector3 position = spawnPoint.position;

        if (ShouldAlignAreaVFXToWaterSurface() && WaveManager.instance != null)
            position.y = GetAlignedAreaVFXWaterHeight(position);

        _vfxObject.transform.position = position;
    }

    private float GetAlignedAreaVFXWaterHeight(Vector3 _position)
    {
        float offset = areaVFXWaterYOffset;

        if (IsDeepFishingArea(fishingArea))
            offset += deepAreaVFXWaterYOffset;

        return transform.position.y + WaveManager.instance.GetWaveHeight(_position.x, _position.z) + offset;
    }

    private bool ShouldAlignAreaVFXToWaterSurface()
    {
        if (!alignAreaVFXToWaterSurface)
            return false;

        return !alignAreaVFXOnlyInDeepArea || IsDeepFishingArea(fishingArea);
    }

    private static bool IsDeepFishingArea(FishingAreaDefinition _area)
    {
        if (_area == null)
            return false;

        return ContainsDeepKeyword(_area.AreaId) || ContainsDeepKeyword(_area.DisplayName);
    }

    private static bool ContainsDeepKeyword(string _text)
    {
        if (string.IsNullOrWhiteSpace(_text))
            return false;

        string normalizedText = _text.ToLowerInvariant();
        return normalizedText.Contains("deep") || normalizedText.Contains("profund");
    }

    private void ResetRuntimeState()
    {
        hasDeactivated = false;
        isEscaping = false;
        escapeFadeRoutine = null;
        transform.localScale = originalScale == Vector3.zero ? transform.localScale : originalScale;
    }

    private void PlayAreaVFX(GameObject _vfxObject)
    {
        VisualEffect[] visualEffects = _vfxObject.GetComponentsInChildren<VisualEffect>(true);

        foreach (VisualEffect visualEffect in visualEffects)
            visualEffect.Play();

        ParticleSystem[] particleSystems = _vfxObject.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
            particleSystem.Play(true);
    }

    private void DeactivateSpot()
    {
        if (hasDeactivated)
            return;

        hasDeactivated = true;
        isEscaping = false;
        UnregisterFishingResult();

        if (escapeFadeRoutine != null)
        {
            StopCoroutine(escapeFadeRoutine);
            escapeFadeRoutine = null;
        }

        if (spawner != null)
        {
            spawner.HandleSpotDeactivated(this);
            return;
        }

        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showEscapeAreaGizmo || escapeDistance <= 0f)
            return;

        DrawHorizontalCircle(GetEscapeReferencePosition(), escapeDistance, escapeAreaGizmoColor);
    }

    private static void DrawHorizontalCircle(Vector3 _center, float _radius, Color _color)
    {
        const int segments = 64;
        Color previousColor = Gizmos.color;
        Gizmos.color = _color;

        Vector3 previousPoint = _center + new Vector3(_radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segments;
            Vector3 nextPoint = _center + new Vector3(Mathf.Cos(angle) * _radius, 0f, Mathf.Sin(angle) * _radius);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        Gizmos.color = previousColor;
    }
}
