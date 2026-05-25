using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishingSpotSpawner : MonoBehaviour
{
    [System.Serializable]
    private class WaterMaterialAreaBinding
    {
        [SerializeField] private Material _material;
        [SerializeField] private Shader _shader;
        [SerializeField] private FishingAreaDefinition _fishingArea;

        public FishingAreaDefinition FishingArea => _fishingArea;

        public bool Matches(Material _waterMaterial)
        {
            if (_waterMaterial == null || _fishingArea == null)
                return false;

            if (_material != null &&
                (_waterMaterial == _material || HaveSameBaseName(_waterMaterial, _material)))
            {
                return true;
            }

            return _shader != null && _waterMaterial.shader == _shader;
        }

        private static bool HaveSameBaseName(Material _a, Material _b)
        {
            return string.Equals(
                GetBaseMaterialName(_a),
                GetBaseMaterialName(_b),
                System.StringComparison.OrdinalIgnoreCase
            );
        }

        private static string GetBaseMaterialName(Material _material)
        {
            if (_material == null)
                return string.Empty;

            return _material.name.Replace(" (Instance)", string.Empty);
        }
    }

    [Header("Área")]
    [SerializeField] private FishingAreaDefinition _fishingArea;
    [SerializeField] private FishingSpot _spotPrefab;

    [Header("Water Area Detection")]
    [SerializeField] private bool _useWaterDepthArea;
    [SerializeField] private GenerateWaterMesh _waterAreaSource;
    [SerializeField] private FishingAreaDefinition _shallowFishingArea;
    [SerializeField] private FishingAreaDefinition _deepFishingArea;

    [Header("Material Area Detection")]
    [SerializeField] private bool _useWaterMaterialArea = true;
    [SerializeField] private WaterMaterialAreaBinding[] _waterMaterialAreas;
    [SerializeField] private float _activeAreaProbeInterval = 0.5f;
    [SerializeField] private bool _fallBackToDepthAreaWhenMaterialUnknown = true;

    [Header("Lava Area Detection")]
    [SerializeField] private Material _lavaMaterial;
    [SerializeField] private Shader _lavaShader;
    [SerializeField] private FishingAreaDefinition _lavaFishingArea;
    [SerializeField] private bool _autoDetectLavaMaterialByName = true;
    [SerializeField] private string _lavaMaterialNameKeyword = "lava";

    [Header("Active Area Focus")]
    [SerializeField] private bool _clearOtherAreaSpotsOnAreaChanged = true;
    [SerializeField] private bool _forceSpawnNearReferenceOnAreaChanged = true;
    [SerializeField] private bool _showAreaChangedWarning;
    [SerializeField] private string _areaChangedWarningFormat = "Nova área de pesca: {0}";

    [Header("Spawn")]
    [SerializeField] private bool _spawnAtStart = true;
    [SerializeField] private int _activeSpotCount = 3;
    [SerializeField] private float _respawnDelay = 12f;
    [Tooltip("Spots gerados somem depois de uma pescaria resolvida com mordida/captura. Cancelar antes da mordida não consome o spot.")]
    [SerializeField] private bool _spawnedSpotsDeactivateAfterFishingStarts = true;
    [Tooltip("Quando ligado, spots que fogem ou foram usados são destruídos. Desligue se for integrar object pool.")]
    [SerializeField] private bool _destroyDeactivatedSpots = true;
    [SerializeField] private float _minDistanceBetweenSpots = 8f;
    [SerializeField] private int _maxSpawnAttempts = 25;

    [Header("Object Pool")]
    [SerializeField] private bool _useFishingSpotPool = true;
    [SerializeField] private string _fishingSpotPoolKey = "FishingSpot";
    [SerializeField, Min(1)] private int _fishingSpotPoolSize = 6;

    [Header("Unfished Spot Lifetime")]
    [SerializeField] private bool _replaceUnfishedSpots = true;
    [SerializeField] private float _spotLifetime = 75f;
    [SerializeField] private bool _forceNearReferenceAfterLongNoFishing = true;
    [SerializeField] private float _longNoFishingTime = 120f;
    [SerializeField] private float _nearReferenceMinDistance = 10f;
    [SerializeField] private float _nearReferenceMaxDistance = 24f;

    [Header("Positions")]
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _spawnPointRadius = 0f;
    [SerializeField] private Vector2 _randomAreaSize = new Vector2(40f, 40f);

    [Header("Reference Relative Spawn")]
    [SerializeField] private bool _spawnNearReference;
    [SerializeField] private bool _fallbackToOpenWaterWhenReferenceSpawnFails = true;
    [SerializeField] private Transform _spawnReference;
    [SerializeField] private float _minDistanceFromReference = 18f;
    [SerializeField] private float _maxDistanceFromReference = 45f;
    [SerializeField, Range(0f, 1f)] private float _frontSpawnChance = 0.65f;
    [SerializeField, Range(0f, 180f)] private float _frontAngle = 50f;
    [SerializeField] private bool _allowSideSpawns = true;
    [SerializeField, Range(0f, 90f)] private float _sideAngleVariation = 30f;

    [Header("Water Projection")]
    [SerializeField] private LayerMask _waterLayer;
    [SerializeField] private bool _fitProjectionToSpotPrefabBounds = true;
    [SerializeField] private float _projectionBoundsPadding = 5f;
    [SerializeField] private float _raycastHeight = 30f;
    [SerializeField] private float _raycastDistance = 80f;

    [Header("Spawn Blocking")]
    [SerializeField] private LayerMask _blockedSpawnLayers;
    [SerializeField, Min(0f)] private float _blockedSpawnRadius = 2f;
    [SerializeField, Min(0f)] private float _blockedSpawnProbeHeight = 4f;
    [SerializeField, Min(0f)] private float _blockedSpawnProbeDepth = 4f;
    [SerializeField] private bool _blockSpawnNearReference = true;
    [SerializeField, Min(0f)] private float _blockedReferenceSpawnRadius = 8f;
    [SerializeField] private bool _blockSpawnNearBoatTag = true;
    [SerializeField] private string _boatTag = "Boat";

    private readonly List<FishingSpot> _activeSpots = new();
    private readonly Dictionary<FishingSpot, int> _activeSpotSpawnVersions = new();
    private Coroutine _respawnRoutine;
    private Coroutine _activeAreaProbeRoutine;
    private float _lastSpotUseTime;
    private FishingAreaDefinition _activeFishingArea;
    private int _nextSpotSpawnVersion;

    public FishingAreaDefinition ActiveFishingArea => _activeFishingArea != null ? _activeFishingArea : _fishingArea;

    private void OnValidate()
    {
        _activeSpotCount = Mathf.Max(0, _activeSpotCount);
        _respawnDelay = Mathf.Max(0f, _respawnDelay);
        _spawnPointRadius = Mathf.Max(0f, _spawnPointRadius);
        _randomAreaSize.x = Mathf.Max(0f, _randomAreaSize.x);
        _randomAreaSize.y = Mathf.Max(0f, _randomAreaSize.y);
        _minDistanceBetweenSpots = Mathf.Max(0f, _minDistanceBetweenSpots);
        _maxSpawnAttempts = Mathf.Max(1, _maxSpawnAttempts);
        _minDistanceFromReference = Mathf.Max(0f, _minDistanceFromReference);
        _maxDistanceFromReference = Mathf.Max(_minDistanceFromReference, _maxDistanceFromReference);
        _spotLifetime = Mathf.Max(0f, _spotLifetime);
        _longNoFishingTime = Mathf.Max(0f, _longNoFishingTime);
        _nearReferenceMinDistance = Mathf.Max(0f, _nearReferenceMinDistance);
        _nearReferenceMaxDistance = Mathf.Max(_nearReferenceMinDistance, _nearReferenceMaxDistance);
        _projectionBoundsPadding = Mathf.Max(0f, _projectionBoundsPadding);
        _raycastHeight = Mathf.Max(0f, _raycastHeight);
        _raycastDistance = Mathf.Max(0.1f, _raycastDistance);
        _activeAreaProbeInterval = Mathf.Max(0.1f, _activeAreaProbeInterval);
        _blockedSpawnRadius = Mathf.Max(0f, _blockedSpawnRadius);
        _blockedSpawnProbeHeight = Mathf.Max(0f, _blockedSpawnProbeHeight);
        _blockedSpawnProbeDepth = Mathf.Max(0f, _blockedSpawnProbeDepth);
        _blockedReferenceSpawnRadius = Mathf.Max(0f, _blockedReferenceSpawnRadius);
        _fishingSpotPoolSize = Mathf.Max(1, _fishingSpotPoolSize);
    }

    private void Start()
    {
        _lastSpotUseTime = Time.time;
        PrepareFishingSpotPool();

        UpdateActiveFishingAreaFromReference(false);

        if (_spawnAtStart)
            SpawnMissingSpots();

        if (_useWaterMaterialArea)
            _activeAreaProbeRoutine = StartCoroutine(ActiveAreaProbeRoutine());
    }

    private void OnDisable()
    {
        if (_activeAreaProbeRoutine != null)
        {
            StopCoroutine(_activeAreaProbeRoutine);
            _activeAreaProbeRoutine = null;
        }
    }

    public void SpawnMissingSpots()
    {
        SpawnMissingSpots(false);
    }

    public void SetActiveFishingArea(FishingAreaDefinition _fishingAreaDefinition, bool _forceNearReference = true)
    {
        if (_fishingAreaDefinition == null)
            return;

        bool areaChanged = _activeFishingArea != _fishingAreaDefinition;
        _activeFishingArea = _fishingAreaDefinition;
        _lastSpotUseTime = Time.time;

        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }

        if (_clearOtherAreaSpotsOnAreaChanged)
            RemoveSpotsOutsideArea(_activeFishingArea);

        bool shouldForceNearReference = areaChanged &&
                                        _forceNearReference &&
                                        _forceSpawnNearReferenceOnAreaChanged;

        SpawnMissingSpots(shouldForceNearReference);

        if (areaChanged && _showAreaChangedWarning && HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(GetAreaChangedWarning(_activeFishingArea));
    }

    public void RefreshActiveFishingAreaFromReference(bool _forceNearReference = true)
    {
        UpdateActiveFishingAreaFromReference(_forceNearReference);
    }

    public string CycleDebugFishingArea()
    {
        if (_activeAreaProbeRoutine != null)
        {
            StopCoroutine(_activeAreaProbeRoutine);
            _activeAreaProbeRoutine = null;
        }

        _useWaterMaterialArea = false;
        _useWaterDepthArea = false;

        FishingAreaDefinition nextArea = ActiveFishingArea == _shallowFishingArea
            ? _deepFishingArea
            : _shallowFishingArea;

        if (nextArea == null)
            nextArea = _deepFishingArea != null ? _deepFishingArea : _fishingArea;

        if (nextArea == null)
            return "Sem área configurada";

        SetActiveFishingArea(nextArea, true);
        return nextArea.DisplayName;
    }

    private IEnumerator ActiveAreaProbeRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_activeAreaProbeInterval);

        while (enabled)
        {
            UpdateActiveFishingAreaFromReference(true);
            yield return wait;
        }
    }

    private void UpdateActiveFishingAreaFromReference(bool _forceNearReference)
    {
        if (!_useWaterMaterialArea)
            return;

        Transform reference = GetSpawnReference();

        if (reference == null)
            return;

        if (!TryProjectToWater(reference.position, out Vector3 waterPosition, out RaycastHit waterHit))
            return;

        FishingAreaDefinition detectedArea = GetFishingAreaForWaterHit(waterPosition, waterHit, false);

        if (detectedArea == null || detectedArea == ActiveFishingArea)
            return;

        SetActiveFishingArea(detectedArea, _forceNearReference);
    }

    private void SpawnMissingSpots(bool _forceNearReference)
    {
        if (_spotPrefab == null)
            return;

        RemoveMissingSpots();

        while (_activeSpots.Count < _activeSpotCount)
        {
            if (!TrySpawnSpot(_forceNearReference))
                break;
        }
    }

    public void HandleSpotDeactivated(FishingSpot _spot)
    {
        if (_spot != null)
        {
            _activeSpots.Remove(_spot);
            ReleaseSpotWithoutRespawn(_spot);
        }

        _lastSpotUseTime = Time.time;

        if (!isActiveAndEnabled)
            return;

        if (_respawnRoutine == null)
            _respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    public FishingSpot GetClosestActiveSpot(Vector3 _referencePosition)
    {
        RemoveMissingSpots();

        FishingSpot closestSpot = null;
        float closestSqrDistance = float.MaxValue;

        foreach (FishingSpot spot in _activeSpots)
        {
            if (spot == null || !spot.gameObject.activeInHierarchy)
                continue;

            float sqrDistance = (spot.transform.position - _referencePosition).sqrMagnitude;

            if (sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            closestSpot = spot;
        }

        return closestSpot;
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(_respawnDelay);

        _respawnRoutine = null;
        SpawnMissingSpots();
    }

    private bool TrySpawnSpot(bool _forceNearReference)
    {
        if (TrySpawnSpotInternal(_forceNearReference, false))
            return true;

        if (_fallbackToOpenWaterWhenReferenceSpawnFails &&
            (_forceNearReference || _spawnNearReference) &&
            TrySpawnSpotInternal(false, true))
        {
            return true;
        }

        Debug.LogWarning("Falha ao gerar FishingSpot: sem posicao valida.");
        return false;
    }

    private bool TrySpawnSpotInternal(bool _forceNearReference, bool _ignoreReferenceSpawn)
    {
        for (int i = 0; i < _maxSpawnAttempts; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition(_forceNearReference, _ignoreReferenceSpawn);

            if (!TryProjectToWater(spawnPosition, out Vector3 waterPosition, out RaycastHit waterHit))
                continue;

            FishingAreaDefinition spawnArea = GetFishingAreaForWaterHit(waterPosition, waterHit, true);

            if (_activeFishingArea != null && spawnArea != _activeFishingArea)
                continue;

            if (IsSpawnNearBlockedReference(waterPosition))
                continue;

            if (IsSpawnBlocked(waterPosition))
                continue;

            if (!IsFarEnoughFromActiveSpots(waterPosition))
                continue;

            FishingSpot spot = GetSpotInstance(waterPosition);

            if (spot == null)
                continue;

            spot.Initialize(spawnArea, this, _spawnedSpotsDeactivateAfterFishingStarts);
            _activeSpots.Add(spot);

            if (_replaceUnfishedSpots && _spotLifetime > 0f)
                StartCoroutine(ExpireUnfishedSpotAfterLifetime(spot, RegisterSpotSpawnVersion(spot)));

            return true;
        }

        return false;
    }

    private Vector3 GetRandomSpawnPosition(bool _forceNearReference)
    {
        return GetRandomSpawnPosition(_forceNearReference, false);
    }

    private Vector3 GetRandomSpawnPosition(bool _forceNearReference, bool _ignoreReferenceSpawn)
    {
        if (!_ignoreReferenceSpawn && (_forceNearReference || _spawnNearReference))
            return GetReferenceRelativeSpawnPosition(_forceNearReference);

        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
            Vector2 offset = Random.insideUnitCircle * _spawnPointRadius;
            return spawnPoint.position + new Vector3(offset.x, 0f, offset.y);
        }

        Vector2 areaOffset = new Vector2(
            Random.Range(-_randomAreaSize.x * 0.5f, _randomAreaSize.x * 0.5f),
            Random.Range(-_randomAreaSize.y * 0.5f, _randomAreaSize.y * 0.5f)
        );

        return transform.position + new Vector3(areaOffset.x, 0f, areaOffset.y);
    }

    private FishingAreaDefinition GetFishingAreaForWaterHit(Vector3 _waterPosition, RaycastHit _waterHit, bool _allowActiveAreaFallback)
    {
        if (_useWaterMaterialArea && TryGetFishingAreaFromWaterMaterial(_waterHit, out FishingAreaDefinition materialArea))
            return materialArea;

        if (_activeFishingArea != null && _allowActiveAreaFallback)
            return _activeFishingArea;

        if (!_useWaterDepthArea || (_useWaterMaterialArea && !_fallBackToDepthAreaWhenMaterialUnknown))
            return _fishingArea;

        if (_waterAreaSource == null)
            _waterAreaSource = FindFirstObjectByType<GenerateWaterMesh>();

        bool isDeepWater = _waterAreaSource != null && _waterAreaSource.IsDeepWater(_waterPosition);

        if (isDeepWater && _deepFishingArea != null)
            return _deepFishingArea;

        if (!isDeepWater && _shallowFishingArea != null)
            return _shallowFishingArea;

        return _fishingArea;
    }

    private bool TryGetFishingAreaFromWaterMaterial(RaycastHit _waterHit, out FishingAreaDefinition _area)
    {
        _area = null;

        if (!TryGetWaterMaterial(_waterHit, out Material waterMaterial))
            return false;

        if (TryGetLavaAreaFromMaterial(waterMaterial, out _area))
            return true;

        if (_waterMaterialAreas == null || _waterMaterialAreas.Length == 0)
            return false;

        for (int i = 0; i < _waterMaterialAreas.Length; i++)
        {
            WaterMaterialAreaBinding binding = _waterMaterialAreas[i];

            if (binding == null || !binding.Matches(waterMaterial))
                continue;

            _area = binding.FishingArea;
            return _area != null;
        }

        return false;
    }

    private bool TryGetLavaAreaFromMaterial(Material _waterMaterial, out FishingAreaDefinition _area)
    {
        _area = null;

        if (_waterMaterial == null || _lavaFishingArea == null)
            return false;

        bool materialMatches = _lavaMaterial != null && MaterialsMatch(_waterMaterial, _lavaMaterial);
        bool shaderMatches = _lavaShader != null && _waterMaterial.shader == _lavaShader;
        bool nameMatches = _autoDetectLavaMaterialByName && MaterialNameContains(_waterMaterial, _lavaMaterialNameKeyword);

        if (!materialMatches && !shaderMatches && !nameMatches)
            return false;

        _area = _lavaFishingArea;
        return true;
    }

    private static bool MaterialsMatch(Material _a, Material _b)
    {
        if (_a == null || _b == null)
            return false;

        return _a == _b || string.Equals(GetBaseMaterialName(_a), GetBaseMaterialName(_b), System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool MaterialNameContains(Material _material, string _keyword)
    {
        if (_material == null || string.IsNullOrWhiteSpace(_keyword))
            return false;

        return GetBaseMaterialName(_material).IndexOf(_keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetBaseMaterialName(Material _material)
    {
        if (_material == null)
            return string.Empty;

        return _material.name.Replace(" (Instance)", string.Empty);
    }

    private bool TryGetWaterMaterial(RaycastHit _waterHit, out Material _material)
    {
        _material = null;

        if (_waterHit.collider == null)
            return false;

        Renderer renderer = _waterHit.collider.GetComponent<Renderer>();

        if (renderer == null)
            renderer = _waterHit.collider.GetComponentInParent<Renderer>();

        if (renderer == null)
            return false;

        Material[] materials = renderer.sharedMaterials;

        if (materials == null || materials.Length == 0)
            return false;

        if (materials.Length == 1)
        {
            _material = materials[0];
            return _material != null;
        }

        int materialIndex = GetHitSubMeshMaterialIndex(_waterHit);
        materialIndex = Mathf.Clamp(materialIndex, 0, materials.Length - 1);
        _material = materials[materialIndex];
        return _material != null;
    }

    private int GetHitSubMeshMaterialIndex(RaycastHit _waterHit)
    {
        MeshCollider meshCollider = _waterHit.collider as MeshCollider;
        Mesh mesh = meshCollider != null ? meshCollider.sharedMesh : null;

        if (mesh == null || _waterHit.triangleIndex < 0)
            return 0;

        int triangleOffset = 0;

        for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
        {
            int triangleCount = mesh.GetTriangles(subMeshIndex).Length / 3;

            if (_waterHit.triangleIndex < triangleOffset + triangleCount)
                return subMeshIndex;

            triangleOffset += triangleCount;
        }

        return 0;
    }

    private void RemoveSpotsOutsideArea(FishingAreaDefinition _targetArea)
    {
        for (int i = _activeSpots.Count - 1; i >= 0; i--)
        {
            FishingSpot spot = _activeSpots[i];

            if (spot == null)
            {
                _activeSpots.RemoveAt(i);
                continue;
            }

            if (spot.FishingArea == _targetArea)
                continue;

            _activeSpots.RemoveAt(i);
            ReleaseSpotWithoutRespawn(spot);
        }
    }

    private void ReleaseSpotWithoutRespawn(FishingSpot _spot)
    {
        if (_spot == null)
            return;

        _activeSpotSpawnVersions.Remove(_spot);

        if (Application.isPlaying)
        {
            if (_useFishingSpotPool && PoolManager.TryReturn(_spot.gameObject))
                return;

            if (_destroyDeactivatedSpots)
            {
                Destroy(_spot.gameObject);
                return;
            }

            _spot.gameObject.SetActive(false);
            return;
        }

        DestroyImmediate(_spot.gameObject);
    }

    private string GetAreaChangedWarning(FishingAreaDefinition _area)
    {
        string areaName = _area != null && !string.IsNullOrWhiteSpace(_area.DisplayName)
            ? _area.DisplayName
            : "nova área";

        if (string.IsNullOrWhiteSpace(_areaChangedWarningFormat))
            return areaName;

        return string.Format(_areaChangedWarningFormat, areaName);
    }

    private Vector3 GetReferenceRelativeSpawnPosition(bool _forceNearReference)
    {
        Transform reference = GetSpawnReference();
        Vector3 forward = reference.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
            forward = transform.forward;

        forward.Normalize();

        float angle = GetReferenceRelativeAngle();
        Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
        float minDistance = _forceNearReference ? _nearReferenceMinDistance : _minDistanceFromReference;
        float maxDistance = _forceNearReference ? _nearReferenceMaxDistance : _maxDistanceFromReference;
        float distance = Random.Range(minDistance, maxDistance);

        return reference.position + direction.normalized * distance;
    }

    private IEnumerator ExpireUnfishedSpotAfterLifetime(FishingSpot _spot, int _spawnVersion)
    {
        yield return new WaitForSeconds(_spotLifetime);

        if (_spot == null)
            yield break;

        if (!_activeSpotSpawnVersions.TryGetValue(_spot, out int currentSpawnVersion) ||
            currentSpawnVersion != _spawnVersion)
        {
            yield break;
        }

        if (!_activeSpots.Contains(_spot) || !_spot.gameObject.activeInHierarchy)
            yield break;

        _activeSpots.Remove(_spot);
        ReleaseSpotWithoutRespawn(_spot);

        bool shouldForceNearReference =
            _forceNearReferenceAfterLongNoFishing &&
            Time.time - _lastSpotUseTime >= _longNoFishingTime;

        SpawnMissingSpots(shouldForceNearReference);
    }

    private Transform GetSpawnReference()
    {
        if (_spawnReference != null)
            return _spawnReference;

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
            return mainCamera.transform;

        return transform;
    }

    private float GetReferenceRelativeAngle()
    {
        bool spawnAtSide = _allowSideSpawns && Random.value > _frontSpawnChance;

        if (!spawnAtSide)
            return Random.Range(-_frontAngle, _frontAngle);

        float sideCenter = Random.value < 0.5f ? -90f : 90f;
        return sideCenter + Random.Range(-_sideAngleVariation, _sideAngleVariation);
    }

    private bool TryProjectToWater(Vector3 _position, out Vector3 _waterPosition)
    {
        return TryProjectToWater(_position, out _waterPosition, out _);
    }

    private bool TryProjectToWater(Vector3 _position, out Vector3 _waterPosition, out RaycastHit _waterHit)
    {
        _waterPosition = _position;
        _waterHit = default;

        if (_waterLayer.value == 0)
            return true;

        float raycastHeight = GetEffectiveRaycastHeight();
        float raycastDistance = GetEffectiveRaycastDistance(raycastHeight);
        Vector3 rayOrigin = _position + Vector3.up * raycastHeight;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, _waterLayer))
            return false;

        _waterPosition = hit.point;
        _waterHit = hit;
        return true;
    }

    private float GetEffectiveRaycastHeight()
    {
        if (!_fitProjectionToSpotPrefabBounds)
            return _raycastHeight;

        return Mathf.Max(_raycastHeight, GetSpotPrefabHeight() + _projectionBoundsPadding);
    }

    private float GetEffectiveRaycastDistance(float _raycastHeight)
    {
        if (!_fitProjectionToSpotPrefabBounds)
            return _raycastDistance;

        return Mathf.Max(_raycastDistance, _raycastHeight + _projectionBoundsPadding);
    }

    private float GetSpotPrefabHeight()
    {
        if (_spotPrefab == null)
            return 0f;

        float height = 0f;

        Renderer[] renderers = _spotPrefab.GetComponentsInChildren<Renderer>();

        foreach (Renderer prefabRenderer in renderers)
            height = Mathf.Max(height, prefabRenderer.bounds.size.y);

        Collider[] colliders = _spotPrefab.GetComponentsInChildren<Collider>();

        foreach (Collider prefabCollider in colliders)
            height = Mathf.Max(height, prefabCollider.bounds.size.y);

        return height;
    }

    private bool IsSpawnBlocked(Vector3 _waterPosition)
    {
        if (_blockedSpawnLayers.value == 0 || _blockedSpawnRadius <= 0f)
            return false;

        Vector3 probeOrigin = _waterPosition + Vector3.up * _blockedSpawnProbeHeight;
        float probeDistance = _blockedSpawnProbeHeight + _blockedSpawnProbeDepth;

        if (probeDistance > 0f &&
            Physics.SphereCast(
                probeOrigin,
                _blockedSpawnRadius,
                Vector3.down,
                out _,
                probeDistance,
                _blockedSpawnLayers,
                QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return Physics.CheckSphere(
            _waterPosition,
            _blockedSpawnRadius,
            _blockedSpawnLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool IsSpawnNearBlockedReference(Vector3 _waterPosition)
    {
        if (!_blockSpawnNearReference || _blockedReferenceSpawnRadius <= 0f)
            return false;

        Transform reference = GetSpawnReference();

        if (reference != null && IsWithinHorizontalDistance(_waterPosition, reference.position, _blockedReferenceSpawnRadius))
            return true;

        if (!_blockSpawnNearBoatTag || string.IsNullOrWhiteSpace(_boatTag))
            return false;

        try
        {
            GameObject[] boats = GameObject.FindGameObjectsWithTag(_boatTag);

            for (int i = 0; i < boats.Length; i++)
            {
                if (boats[i] != null && IsWithinHorizontalDistance(_waterPosition, boats[i].transform.position, _blockedReferenceSpawnRadius))
                    return true;
            }
        }
        catch (UnityException)
        {
            return false;
        }

        return false;
    }

    private static bool IsWithinHorizontalDistance(Vector3 _a, Vector3 _b, float _distance)
    {
        _a.y = 0f;
        _b.y = 0f;
        return Vector3.Distance(_a, _b) < _distance;
    }

    private bool IsFarEnoughFromActiveSpots(Vector3 _position)
    {
        if (_minDistanceBetweenSpots <= 0f)
            return true;

        foreach (FishingSpot spot in _activeSpots)
        {
            if (spot == null)
                continue;

            Vector3 activePosition = spot.transform.position;
            activePosition.y = _position.y;

            if (Vector3.Distance(activePosition, _position) < _minDistanceBetweenSpots)
                return false;
        }

        return true;
    }

    private void RemoveMissingSpots()
    {
        for (int i = _activeSpots.Count - 1; i >= 0; i--)
        {
            if (_activeSpots[i] == null || !_activeSpots[i].gameObject.activeInHierarchy)
            {
                if (_activeSpots[i] != null)
                    _activeSpotSpawnVersions.Remove(_activeSpots[i]);

                _activeSpots.RemoveAt(i);
            }
        }
    }

    private void PrepareFishingSpotPool()
    {
        if (!_useFishingSpotPool || _spotPrefab == null || string.IsNullOrWhiteSpace(_fishingSpotPoolKey))
            return;

        int poolSize = Mathf.Max(_fishingSpotPoolSize, _activeSpotCount);
        PoolManager.GetOrCreate().EnsurePool(_fishingSpotPoolKey, _spotPrefab.gameObject, poolSize, false);
    }

    private FishingSpot GetSpotInstance(Vector3 _waterPosition)
    {
        Quaternion rotation = _spotPrefab.transform.rotation;

        if (_useFishingSpotPool && !string.IsNullOrWhiteSpace(_fishingSpotPoolKey))
        {
            PrepareFishingSpotPool();
            GameObject pooledObject = PoolManager.Instance != null
                ? PoolManager.Instance.GetObject(_fishingSpotPoolKey, _waterPosition, rotation, transform)
                : null;

            if (pooledObject != null && pooledObject.TryGetComponent(out FishingSpot pooledSpot))
                return pooledSpot;

            if (pooledObject != null)
                PoolManager.TryReturn(pooledObject);
        }

        return Instantiate(_spotPrefab, _waterPosition, rotation, transform);
    }

    private int RegisterSpotSpawnVersion(FishingSpot _spot)
    {
        int spawnVersion = ++_nextSpotSpawnVersion;
        _activeSpotSpawnVersions[_spot] = spawnVersion;
        return spawnVersion;
    }
}
