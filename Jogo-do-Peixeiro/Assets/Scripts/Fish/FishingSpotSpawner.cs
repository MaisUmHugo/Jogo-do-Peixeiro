using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishingSpotSpawner : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] private FishingAreaDefinition _fishingArea;
    [SerializeField] private FishingSpot _spotPrefab;

    [Header("Spawn")]
    [SerializeField] private bool _spawnAtStart = true;
    [SerializeField] private int _activeSpotCount = 3;
    [SerializeField] private float _respawnDelay = 12f;
    [SerializeField] private bool _spawnedSpotsDeactivateAfterFishingStarts = true;
    [SerializeField] private float _minDistanceBetweenSpots = 8f;
    [SerializeField] private int _maxSpawnAttempts = 25;

    [Header("Positions")]
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _spawnPointRadius = 0f;
    [SerializeField] private Vector2 _randomAreaSize = new Vector2(40f, 40f);

    [Header("Reference Relative Spawn")]
    [SerializeField] private bool _spawnNearReference;
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

    private readonly List<FishingSpot> _activeSpots = new();
    private Coroutine _respawnRoutine;

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
        _projectionBoundsPadding = Mathf.Max(0f, _projectionBoundsPadding);
        _raycastHeight = Mathf.Max(0f, _raycastHeight);
        _raycastDistance = Mathf.Max(0.1f, _raycastDistance);
    }

    private void Start()
    {
        if (_spawnAtStart)
            SpawnMissingSpots();
    }

    public void SpawnMissingSpots()
    {
        if (_spotPrefab == null)
            return;

        RemoveMissingSpots();

        while (_activeSpots.Count < _activeSpotCount)
        {
            if (!TrySpawnSpot())
                break;
        }
    }

    public void HandleSpotDeactivated(FishingSpot _spot)
    {
        if (_spot != null)
            _activeSpots.Remove(_spot);

        if (!isActiveAndEnabled)
            return;

        if (_respawnRoutine == null)
            _respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(_respawnDelay);

        _respawnRoutine = null;
        SpawnMissingSpots();
    }

    private bool TrySpawnSpot()
    {
        for (int i = 0; i < _maxSpawnAttempts; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();

            if (!TryProjectToWater(spawnPosition, out Vector3 waterPosition))
                continue;

            if (!IsFarEnoughFromActiveSpots(waterPosition))
                continue;

            FishingSpot spot = Instantiate(_spotPrefab, waterPosition, _spotPrefab.transform.rotation, transform);
            spot.Initialize(_fishingArea, this, _spawnedSpotsDeactivateAfterFishingStarts);
            _activeSpots.Add(spot);
            return true;
        }

        Debug.LogWarning("Falha ao gerar FishingSpot: sem posição válida.");
        return false;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (_spawnNearReference)
            return GetReferenceRelativeSpawnPosition();

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

    private Vector3 GetReferenceRelativeSpawnPosition()
    {
        Transform reference = GetSpawnReference();
        Vector3 forward = reference.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
            forward = transform.forward;

        forward.Normalize();

        float angle = GetReferenceRelativeAngle();
        Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
        float distance = Random.Range(_minDistanceFromReference, _maxDistanceFromReference);

        return reference.position + direction.normalized * distance;
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
        _waterPosition = _position;

        if (_waterLayer.value == 0)
            return true;

        float raycastHeight = GetEffectiveRaycastHeight();
        float raycastDistance = GetEffectiveRaycastDistance(raycastHeight);
        Vector3 rayOrigin = _position + Vector3.up * raycastHeight;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, _waterLayer))
            return false;

        _waterPosition = hit.point;
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
            if (_activeSpots[i] == null || !_activeSpots[i].gameObject.activeSelf)
                _activeSpots.RemoveAt(i);
        }
    }
}
