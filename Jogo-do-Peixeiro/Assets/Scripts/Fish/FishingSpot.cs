using UnityEngine;
using UnityEngine.VFX;

public class FishingSpot : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] private FishingAreaDefinition fishingArea;
    [SerializeField] private FishScriptableObject[] availableFish;
    [Tooltip("Quando ligado, o spot some depois de uma pescaria resolvida com mordida/captura. Cancelar antes da mordida nao consome o spot.")]
    [SerializeField] private bool deactivateAfterFishingStarts;

    [SerializeField] private float minHorizontalDistance = 4f;

    [Header("Fish Escape")]
    [SerializeField] private bool ignoreEscapeForDebug;
    [SerializeField] private string boatTag = "Boat";
    [SerializeField] private string escapeWarningMessage = "Os peixes fugiram";

    [Header("Boat Proximity Fade")]
    [SerializeField] private bool fadeOutByBoatDistance = true;
    [Tooltip("Evita spot visivel mas impossivel de pescar: se chegar perto demais para pescar, os peixes fogem e o spot some.")]
    [SerializeField] private bool blockFishingWhileFading = true;
    [SerializeField] private float fadeStartDistance = 12f;
    [SerializeField] private float escapeDistance = 4f;
    [SerializeField] private float minimumFadeScale = 0.15f;

    [Header("Cast Target")]
    [SerializeField] private Transform castTargetPoint;

    [Header("Area VFX")]
    [SerializeField] private GameObject areaVFXObject;
    [SerializeField] private GameObject areaVFXPrefab;
    [SerializeField] private Transform areaVFXPoint;
    [SerializeField] private Vector3 areaVFXLocalOffset;
    [SerializeField] private bool parentAreaVFXToSpot = true;
    [SerializeField] private bool playAreaVFXOnEnable = true;

    private FishingSpotSpawner spawner;
    private Transform boatTransform;
    private Vector3 originalScale;
    private bool isFadingByBoatDistance;
    private bool hasDeactivated;
    private bool isWaitingFishingResult;
    private GameObject areaVFXInstance;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnValidate()
    {
        minHorizontalDistance = Mathf.Max(0f, minHorizontalDistance);
        fadeStartDistance = Mathf.Max(0.1f, fadeStartDistance);
        escapeDistance = Mathf.Clamp(escapeDistance, 0f, fadeStartDistance);
        minimumFadeScale = Mathf.Clamp(minimumFadeScale, 0f, 1f);
    }

    private void OnEnable()
    {
        hasDeactivated = false;
        isFadingByBoatDistance = false;
        transform.localScale = originalScale == Vector3.zero ? transform.localScale : originalScale;
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
        UpdateBoatProximityFade();
    }

    public void Initialize(FishingAreaDefinition _fishingArea, FishingSpotSpawner _spawner, bool _deactivateAfterFishingStarts)
    {
        fishingArea = _fishingArea;
        spawner = _spawner;
        deactivateAfterFishingStarts = _deactivateAfterFishingStarts;
        hasDeactivated = false;
    }

    public bool TryStartFishingFromRod(ShipInventory _inventory)
    {
        if (FishingManager.instance == null)
            return false;

        if (_inventory == null)
            return false;

        if (GameManager.instance == null)
            return false;

        Vector3 fishingReferencePosition = GetFishingReferencePosition(_inventory);

        if (ShouldBlockFishingForBoatProximity(fishingReferencePosition))
            return false;

        Vector3 a = fishingReferencePosition;
        Vector3 b = transform.position;

        a.y = 0f;
        b.y = 0f;

        float horizontalDistance = Vector3.Distance(a, b);

        if (horizontalDistance < minHorizontalDistance)
        {
            EscapeFishIfFishingWouldBeBlocked(horizontalDistance);
            return false;
        }

        FishScriptableObject[] fishList = GetAvailableFish();
        bool startedFishing = FishingManager.instance.StartFishing(_inventory, fishList);

        if (startedFishing && deactivateAfterFishingStarts)
            RegisterFishingResult();

        return startedFishing;
    }

    public FishScriptableObject[] GetAvailableFish()
    {
        if (fishingArea != null && fishingArea.HasFishAvailable)
            return fishingArea.AvailableFish;

        return availableFish;
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

        if (!IsBoatCollider(_other))
            return;

        if (fadeOutByBoatDistance)
        {
            boatTransform = _other.transform.root;
            return;
        }

        EscapeFish();
    }

    private bool IsBoatCollider(Collider _collider)
    {
        Transform current = _collider.transform;

        while (current != null)
        {
            if (current.CompareTag(boatTag))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void EscapeFish()
    {
        if (HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(escapeWarningMessage);

        DeactivateSpot();
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

    private void UpdateBoatProximityFade()
    {
        if (!fadeOutByBoatDistance || ignoreEscapeForDebug || hasDeactivated)
            return;

        if (boatTransform == null && !TryFindBoatTransform())
            return;

        float horizontalDistance = GetHorizontalDistance(boatTransform.position, transform.position);
        isFadingByBoatDistance = horizontalDistance < fadeStartDistance;
        float fadeNormalized = Mathf.InverseLerp(escapeDistance, fadeStartDistance, horizontalDistance);
        float scale = Mathf.Lerp(minimumFadeScale, 1f, fadeNormalized);
        transform.localScale = originalScale * scale;

        if (horizontalDistance <= GetEffectiveEscapeDistance())
            EscapeFish();
    }

    private bool ShouldBlockFishingForBoatProximity(Vector3 _playerPosition)
    {
        if (!blockFishingWhileFading || !fadeOutByBoatDistance || ignoreEscapeForDebug)
            return false;

        float horizontalDistance = GetHorizontalDistance(_playerPosition, transform.position);

        if (horizontalDistance > GetEffectiveEscapeDistance())
            return false;

        EscapeFish();
        return true;
    }

    private void EscapeFishIfFishingWouldBeBlocked(float _horizontalDistance)
    {
        if (!blockFishingWhileFading || ignoreEscapeForDebug || hasDeactivated)
            return;

        if (_horizontalDistance <= GetEffectiveEscapeDistance())
            EscapeFish();
    }

    private float GetEffectiveEscapeDistance()
    {
        if (!blockFishingWhileFading)
            return escapeDistance;

        return Mathf.Max(escapeDistance, minHorizontalDistance);
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
        if (string.IsNullOrWhiteSpace(boatTag))
            return false;

        try
        {
            GameObject boatObject = GameObject.FindGameObjectWithTag(boatTag);

            if (boatObject == null)
                return false;

            boatTransform = boatObject.transform;
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private float GetHorizontalDistance(Vector3 _a, Vector3 _b)
    {
        _a.y = 0f;
        _b.y = 0f;
        return Vector3.Distance(_a, _b);
    }

    private void SetAreaVFXVisible(bool _visible)
    {
        GameObject vfxObject = GetAreaVFXObject();

        if (vfxObject == null)
            return;

        vfxObject.SetActive(_visible);

        if (!_visible)
            return;

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

        if (parentAreaVFXToSpot)
            areaVFXInstance.transform.localPosition += areaVFXLocalOffset;
        else
            areaVFXInstance.transform.position += areaVFXLocalOffset;

        return areaVFXInstance;
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
        UnregisterFishingResult();

        if (spawner != null)
            spawner.HandleSpotDeactivated(this);

        gameObject.SetActive(false);
    }
}
