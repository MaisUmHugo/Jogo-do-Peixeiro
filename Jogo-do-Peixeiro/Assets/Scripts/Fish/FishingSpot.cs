using UnityEngine;

public class FishingSpot : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] private FishingAreaDefinition fishingArea;
    [SerializeField] private FishScriptableObject[] availableFish;
    [SerializeField] private bool deactivateAfterFishingStarts;

    [SerializeField] private float minHorizontalDistance = 4f;

    [Header("Fish Escape")]
    [SerializeField] private bool ignoreEscapeForDebug;
    [SerializeField] private string boatTag = "Boat";
    [SerializeField] private string escapeWarningMessage = "Os peixes fugiram";

    [Header("Boat Proximity Fade")]
    [SerializeField] private bool fadeOutByBoatDistance = true;
    [SerializeField] private bool blockFishingWhileFading = true;
    [SerializeField] private float fadeStartDistance = 12f;
    [SerializeField] private float escapeDistance = 4f;
    [SerializeField] private float minimumFadeScale = 0.15f;

    [Header("Cast Target")]
    [SerializeField] private Transform castTargetPoint;

    private FishingSpotSpawner spawner;
    private Transform boatTransform;
    private Vector3 originalScale;
    private bool isFadingByBoatDistance;
    private bool hasDeactivated;

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

        Transform player = _inventory.transform.root;

        if (ShouldBlockFishingForBoatProximity(player.position))
            return false;

        Vector3 a = player.position;
        Vector3 b = transform.position;

        a.y = 0f;
        b.y = 0f;

        float horizontalDistance = Vector3.Distance(a, b);

        if (horizontalDistance < minHorizontalDistance)
            return false;

        FishScriptableObject[] fishList = GetAvailableFish();
        bool startedFishing = FishingManager.instance.StartFishing(_inventory, fishList);

        if (startedFishing && deactivateAfterFishingStarts)
            DeactivateSpot();

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

        if (horizontalDistance <= escapeDistance)
            EscapeFish();
    }

    private bool ShouldBlockFishingForBoatProximity(Vector3 _playerPosition)
    {
        if (!blockFishingWhileFading || !fadeOutByBoatDistance || ignoreEscapeForDebug)
            return false;

        return isFadingByBoatDistance || GetHorizontalDistance(_playerPosition, transform.position) < fadeStartDistance;
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

    private void DeactivateSpot()
    {
        if (hasDeactivated)
            return;

        hasDeactivated = true;

        if (spawner != null)
            spawner.HandleSpotDeactivated(this);

        gameObject.SetActive(false);
    }
}
