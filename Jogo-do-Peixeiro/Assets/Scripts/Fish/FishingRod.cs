using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class FishingRod : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform rodTip;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask fishingSpotLayer;
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private ShipInventory shipInventory;

    [SerializeField] private Transform hookPrefab;
    [SerializeField] private float hookSpeed = 25f;

    [SerializeField] private VisualEffect splashVFXPrefab;
    [SerializeField] private float splashLifetime = 3f;

    [Header("Fish Movement Visual")]
    [SerializeField] private FishDirectionPull fishDirectionPull;
    [SerializeField] private bool moveHookWithFishPull = true;
    [SerializeField] private bool keepSplashWhileFishing = true;
    [SerializeField] private float fishMovementRadius = 1.4f;
    [SerializeField] private float fishMovementFollowSpeed = 4f;

    [Tooltip("VFX tocado quando o peixe muda de direção (diferente do splash de arremesso). Opcional.")]
    [SerializeField] private VisualEffect fishPullVFXPrefab;
    [SerializeField] private float fishPullVFXLifetime = 1.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip castSfx;
    [SerializeField] private AudioClip splashSfx;
    [SerializeField] private AudioClip fishPullSfx;
    [SerializeField, Range(0f, 1f)] private float castSfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float splashSfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float fishPullSfxVolume = 0.65f;

    [SerializeField] private int arcPoints = 25;
    [SerializeField] private float raycastRadius = 0.5f;
    [SerializeField] private float fishingSpotSnapRadius = 2.5f;
    [SerializeField] private bool useFishingSpotFallbackHit = true;
    [SerializeField] private float spotFallbackWaterProbeHeight = 8f;
    [SerializeField] private float spotFallbackWaterProbeDistance = 24f;

    [SerializeField] private float minCastDistance = 5f;
    [SerializeField] private float maxCastDistance = 35f;
    [SerializeField] private float chargeSpeed = 14f;
    [SerializeField] private float noSpotWaitBeforeReturn = 1.2f;
    [SerializeField] private bool cancelAimOnQuickRelease = true;
    [SerializeField] private bool cancelAimOnSecondPress = true;
    [SerializeField] private float minAimHoldBeforeCast = 0.25f;

    private float currentForce;
    private bool isAiming;
    private bool hookTraveling;
    private bool hookReturning;
    private bool hookWaitingInWater;
    private bool hasValidCastTarget;
    private float aimStartTime;

    private FishingSpot currentTargetSpot;
    private VisualEffect currentSplashInstance;
    private Transform currentHook;
    private Coroutine hookRoutine;
    private Vector3 hookWaterOrigin;
    private Vector3 fishMovementTarget;
    private int lastFishMovementPromptId = -1;

    private Vector3[] arcCache;

    private void OnValidate()
    {
        minCastDistance = Mathf.Max(0.1f, minCastDistance);
        maxCastDistance = Mathf.Max(minCastDistance, maxCastDistance);
        chargeSpeed = Mathf.Max(0.1f, chargeSpeed);
        minAimHoldBeforeCast = Mathf.Max(0f, minAimHoldBeforeCast);
        noSpotWaitBeforeReturn = Mathf.Max(0f, noSpotWaitBeforeReturn);
        hookSpeed = Mathf.Max(0.1f, hookSpeed);
        arcPoints = Mathf.Max(2, arcPoints);
        raycastRadius = Mathf.Max(0.01f, raycastRadius);
        fishingSpotSnapRadius = Mathf.Max(0f, fishingSpotSnapRadius);
        spotFallbackWaterProbeHeight = Mathf.Max(0f, spotFallbackWaterProbeHeight);
        spotFallbackWaterProbeDistance = Mathf.Max(0.1f, spotFallbackWaterProbeDistance);
    }

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (shipInventory == null)
            shipInventory = GetComponentInParent<ShipInventory>();

        if (fishDirectionPull == null)
            fishDirectionPull = FindFirstObjectByType<FishDirectionPull>(FindObjectsInactive.Include);

        lineRenderer.enabled = false;
    }

    private void Start()
    {
        if (InputHandler.instance != null)
        {
            InputHandler.instance.onAimPressed += HandleAimPressed;
            InputHandler.instance.onAimReleased += HandleAimReleased;
        }
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
        {
            InputHandler.instance.onAimPressed -= HandleAimPressed;
            InputHandler.instance.onAimReleased -= HandleAimReleased;
        }
    }

    private void Update()
    {
        if (GameManager.instance == null)
            return;

        if (isAiming)
            UpdateAim();

        UpdateFishMovementVisual();
        UpdateHookLine();

    }

    private void HandleAimPressed()
    {
        if (isAiming && cancelAimOnSecondPress)
        {
            CancelAim();
            return;
        }

        if (hookWaitingInWater)
        {
            RecallHook();
            return;
        }

        if (hookTraveling || hookReturning)
            return;

        if (GameManager.instance == null)
            return;

        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
            return;

        isAiming = true;
        currentTargetSpot = null;
        currentForce = minCastDistance;
        hasValidCastTarget = false;
        aimStartTime = Time.time;

        lineRenderer.enabled = true;
    }

    private void HandleAimReleased()
    {
        if (!isAiming)
            return;

        if (cancelAimOnQuickRelease && Time.time - aimStartTime < minAimHoldBeforeCast)
        {
            CancelAim();
            return;
        }

        if (!hasValidCastTarget)
        {
            CancelAim();
            return;
        }

        isAiming = false;
        hookTraveling = true;

        arcCache = new Vector3[arcPoints];

        for (int i = 0; i < arcPoints; i++)
            arcCache[i] = lineRenderer.GetPosition(i);

        lineRenderer.enabled = false;

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.Fishing);

        PlayCastSfx();
        hookRoutine = StartCoroutine(AnimateHook());
    }

    private void UpdateAim()
    {
        currentForce += chargeSpeed * Time.deltaTime;
        currentForce = Mathf.Clamp(currentForce, minCastDistance, maxCastDistance);

        hasValidCastTarget = TryGetAimPoint(out Vector3 targetPoint, out FishingSpot hitSpot);
        currentTargetSpot = hitSpot;

        DrawArc(rodTip.position, targetPoint);
    }

    private IEnumerator AnimateHook()
    {
        if (currentHook != null)
            Destroy(currentHook.gameObject);

        currentHook = Instantiate(hookPrefab, rodTip.position, Quaternion.identity);
        PrepareHookForManualMovement(currentHook);

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = true;

        for (int i = 0; i < arcCache.Length; i++)
        {
            Vector3 target = arcCache[i];

            while (Vector3.Distance(currentHook.position, target) > 0.05f)
            {
                currentHook.position = Vector3.MoveTowards(
                    currentHook.position,
                    target,
                    hookSpeed * Time.deltaTime
                );

                yield return null;
            }
        }

        if (!TryGetWaterHit(out Vector3 waterHit) && !TryGetFishingSpotFallbackHit(out waterHit))
        {
            hookTraveling = false;
            yield return ReturnHook();
            yield break;
        }

        SpawnSplash(waterHit);
        currentHook.position = waterHit;
        hookWaterOrigin = waterHit;
        fishMovementTarget = waterHit;
        lastFishMovementPromptId = -1;

        hookTraveling = false;
        hookWaitingInWater = true;

        bool startedFishing = currentTargetSpot != null &&
                              currentTargetSpot.TryStartFishingFromRod(shipInventory);

        if (!startedFishing)
        {
            yield return new WaitForSeconds(noSpotWaitBeforeReturn);

            if (!hookWaitingInWater)
                yield break;

            yield return ReturnHook();
            yield break;
        }

        hookRoutine = null;
    }

    private void CancelAim()
    {
        isAiming = false;
        hasValidCastTarget = false;
        currentTargetSpot = null;
        currentForce = minCastDistance;

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void RecallHook()
    {
        if (!hookWaitingInWater)
            return;

        if (FishingManager.instance != null && FishingManager.instance.IsFishing)
            FishingManager.instance.CancelFishing();

        if (hookRoutine != null)
            StopCoroutine(hookRoutine);

        hookRoutine = StartCoroutine(ReturnHook());
    }

    public void ReturnHookAfterFishing()
    {
        if (!hookWaitingInWater || hookReturning)
            return;

        if (hookRoutine != null)
            StopCoroutine(hookRoutine);

        hookRoutine = StartCoroutine(ReturnHook());
    }

    private IEnumerator ReturnHook()
    {
        hookReturning = true;
        hookWaitingInWater = false;

        if (currentHook == null)
        {
            DestroyCurrentSplash();
            ResetHookState();
            yield break;
        }

        while (Vector3.Distance(currentHook.position, rodTip.position) > 0.05f)
        {
            currentHook.position = Vector3.MoveTowards(
                currentHook.position,
                rodTip.position,
                hookSpeed * 1.5f * Time.deltaTime
            );

            yield return null;
        }

        Destroy(currentHook.gameObject);
        currentHook = null;

        DestroyCurrentSplash();
        lineRenderer.enabled = false;

        ResetHookState();

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
    }

    private void ResetHookState()
    {
        hookTraveling = false;
        hookReturning = false;
        hookWaitingInWater = false;
        isAiming = false;
        hasValidCastTarget = false;
        currentTargetSpot = null;
        lastFishMovementPromptId = -1;
        hookRoutine = null;
    }

    // Expõe a posição do anzol para sistemas externos (ex: FishingDirectionVFX, câmera)
    public Vector3 GetHookWorldPosition()
    {
        return currentHook != null ? currentHook.position : rodTip.position;
    }

    private void UpdateHookLine()
    {
        if (!lineRenderer.enabled || currentHook == null)
            return;

        lineRenderer.SetPosition(0, rodTip.position);
        lineRenderer.SetPosition(1, currentHook.position);
    }

    private void SpawnSplash(Vector3 position)
    {
        if (splashVFXPrefab == null)
            return;

        DestroyCurrentSplash();

        currentSplashInstance = Instantiate(
            splashVFXPrefab,
            position,
            Quaternion.identity
        );

        float normalizedForce = Mathf.InverseLerp(minCastDistance, maxCastDistance, currentForce);

        if (currentSplashInstance.HasFloat("Intensity"))
            currentSplashInstance.SetFloat("Intensity", normalizedForce);

        currentSplashInstance.Play();

        if (!keepSplashWhileFishing)
            Destroy(currentSplashInstance.gameObject, splashLifetime);

        PlaySplashSfx(position);
    }

    private void UpdateFishMovementVisual()
    {
        if (!moveHookWithFishPull ||
            !hookWaitingInWater ||
            currentHook == null ||
            fishDirectionPull == null ||
            FishingManager.instance == null ||
            !FishingManager.instance.IsFishing)
        {
            return;
        }

        if (fishDirectionPull.CurrentPromptId != lastFishMovementPromptId)
        {
            Vector2 fishMovement = fishDirectionPull.FishMovementVector;
            Vector3 worldMovement = new Vector3(fishMovement.x, 0f, fishMovement.y);

            fishMovementTarget = hookWaterOrigin + (worldMovement * fishMovementRadius);
            lastFishMovementPromptId = fishDirectionPull.CurrentPromptId;

            if (currentSplashInstance != null)
                currentSplashInstance.Play();

            SpawnFishPullVFX();
            PlayFishPullSfx(currentHook.position);
        }

        currentHook.position = Vector3.Lerp(
            currentHook.position,
            fishMovementTarget,
            Time.deltaTime * fishMovementFollowSpeed
        );

        if (currentSplashInstance != null)
            currentSplashInstance.transform.position = currentHook.position;
    }

    private void DestroyCurrentSplash()
    {
        if (currentSplashInstance == null)
            return;

        Destroy(currentSplashInstance.gameObject);
        currentSplashInstance = null;
    }

    private void SpawnFishPullVFX()
    {
        if (fishPullVFXPrefab == null || currentHook == null)
            return;

        // Direção que o peixe está se movendo (world space, plano horizontal)
        Vector2 fishMove = fishDirectionPull != null
            ? fishDirectionPull.FishMovementVector
            : Vector2.zero;

        Vector3 fishWorldDir = new Vector3(fishMove.x, 0f, fishMove.y);

        Quaternion spawnRotation = fishWorldDir != Vector3.zero
            ? Quaternion.LookRotation(fishWorldDir, Vector3.up)
            : Quaternion.identity;

        VisualEffect instance = Instantiate(
            fishPullVFXPrefab,
            currentHook.position,
            spawnRotation
        );

        // Passa a direção para o VFX Graph caso use o parâmetro "Direction"
        if (instance.HasVector3("Direction"))
            instance.SetVector3("Direction", fishWorldDir);

        instance.Play();

        Destroy(instance.gameObject, fishPullVFXLifetime);
    }

    private void PlayCastSfx()
    {
        if (AudioManager.Instance == null || castSfx == null)
            return;

        AudioManager.Instance.PlaySfx(castSfx, castSfxVolume);
    }

    private void PlaySplashSfx(Vector3 _position)
    {
        if (AudioManager.Instance == null || splashSfx == null)
            return;

        AudioManager.Instance.PlaySfxAtPosition(splashSfx, _position, splashSfxVolume);
    }

    private void PlayFishPullSfx(Vector3 _position)
    {
        if (AudioManager.Instance == null || fishPullSfx == null)
            return;

        AudioManager.Instance.PlaySfxAtPosition(fishPullSfx, _position, fishPullSfxVolume);
    }

    private bool TryGetAimPoint(out Vector3 targetPoint, out FishingSpot hitSpot)
    {
        hitSpot = null;

        Ray ray = playerCamera.ScreenPointToRay(
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
        );

        Vector3 origin = rodTip.position;
        Vector3 direction = ray.direction;

        targetPoint = origin + direction * currentForce;

        if (Physics.SphereCast(origin, raycastRadius, direction, out RaycastHit hit, currentForce, fishingSpotLayer, QueryTriggerInteraction.Collide))
        {
            if (TryGetFishingSpotFromCollider(hit.collider, out hitSpot))
                targetPoint = hitSpot.GetCastTargetPosition(hit.point);
            else
                targetPoint = hit.point;

            return true;
        }

        if (Physics.Raycast(origin, direction, out RaycastHit waterHit, currentForce, waterLayer))
        {
            targetPoint = waterHit.point;

            if (TryFindNearbyFishingSpot(waterHit.point, out hitSpot))
                targetPoint = hitSpot.GetCastTargetPosition(waterHit.point);

            return true;
        }

        return false;
    }

    private bool TryGetFishingSpotFromCollider(Collider _collider, out FishingSpot _spot)
    {
        _spot = null;

        if (_collider == null)
            return false;

        _spot = _collider.GetComponentInParent<FishingSpot>();
        return _spot != null;
    }

    private bool TryFindNearbyFishingSpot(Vector3 _position, out FishingSpot _spot)
    {
        _spot = null;

        if (fishingSpotSnapRadius <= 0f)
            return false;

        Collider[] colliders = Physics.OverlapSphere(
            _position,
            fishingSpotSnapRadius,
            fishingSpotLayer,
            QueryTriggerInteraction.Collide
        );

        float closestSqrDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            if (!TryGetFishingSpotFromCollider(collider, out FishingSpot candidate))
                continue;

            float sqrDistance = (candidate.transform.position - _position).sqrMagnitude;

            if (sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            _spot = candidate;
        }

        return _spot != null;
    }

    private void PrepareHookForManualMovement(Transform _hook)
    {
        if (_hook == null)
            return;

        Rigidbody[] rigidbodies = _hook.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody hookRigidbody in rigidbodies)
        {
            hookRigidbody.useGravity = false;
            hookRigidbody.linearVelocity = Vector3.zero;
            hookRigidbody.angularVelocity = Vector3.zero;
            hookRigidbody.isKinematic = true;
        }
    }

    private void DrawArc(Vector3 startPoint, Vector3 endPoint)
    {
        lineRenderer.positionCount = arcPoints;

        float distance = Vector3.Distance(startPoint, endPoint);
        float height = distance * 0.5f;

        for (int i = 0; i < arcPoints; i++)
        {
            float t = i / (float)(arcPoints - 1);

            Vector3 point = Vector3.Lerp(startPoint, endPoint, t);
            point.y += Mathf.Sin(t * Mathf.PI) * height;

            lineRenderer.SetPosition(i, point);
        }
    }
    public void PlaySuccessSplash(FishSkillCheck.FeedbackResult result)
    {
        if (currentHook == null)
            return;

        float intensityMultiplier = 1f;

        switch (result)
        {
            case FishSkillCheck.FeedbackResult.Good:
                intensityMultiplier = 0.8f;
                break;

            case FishSkillCheck.FeedbackResult.Great:
                intensityMultiplier = 1.2f;
                break;

            case FishSkillCheck.FeedbackResult.Perfect:
                intensityMultiplier = 1.6f;
                StartCoroutine(PerfectPull());
                break;
        }

        SpawnScaledSplash(currentHook.position, intensityMultiplier);
    }

    private void SpawnScaledSplash(Vector3 position, float multiplier)
    {
        if (splashVFXPrefab == null)
            return;

        var splash = Instantiate(splashVFXPrefab, position, Quaternion.identity);

        float normalizedForce = Mathf.InverseLerp(minCastDistance, maxCastDistance, currentForce);
        float finalIntensity = normalizedForce * multiplier;

        if (splash.HasFloat("Intensity"))
            splash.SetFloat("Intensity", finalIntensity);

        splash.Play();

        Destroy(splash.gameObject, splashLifetime);
    }

    private IEnumerator PerfectPull()
    {
        if (currentHook == null)
            yield break;

        Vector3 start = currentHook.position;
        Vector3 pullTarget = start + (rodTip.position - start).normalized * 0.7f;

        float t = 0f;
        float duration = 0.12f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            currentHook.position = Vector3.Lerp(start, pullTarget, t);

            yield return null;
        }

        t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            currentHook.position = Vector3.Lerp(pullTarget, start, t);

            yield return null;
        }
    }

    private bool TryGetWaterHit(out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        for (int i = 0; i < arcCache.Length - 1; i++)
        {
            Vector3 a = arcCache[i];
            Vector3 b = arcCache[i + 1];

            Vector3 dir = b - a;
            float dist = dir.magnitude;

            if (Physics.Raycast(a, dir.normalized, out RaycastHit hit, dist, waterLayer))
            {
                hitPoint = hit.point;
                return true;
            }
        }

        return false;
    }

    private bool TryGetFishingSpotFallbackHit(out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        if (!useFishingSpotFallbackHit || currentTargetSpot == null)
            return false;

        Vector3 fallbackPosition = arcCache != null && arcCache.Length > 0
            ? arcCache[^1]
            : currentTargetSpot.transform.position;

        Vector3 spotTargetPosition = currentTargetSpot.GetCastTargetPosition(fallbackPosition);
        Vector3 rayOrigin = spotTargetPosition + Vector3.up * spotFallbackWaterProbeHeight;

        if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit waterHit,
                spotFallbackWaterProbeDistance,
                waterLayer,
                QueryTriggerInteraction.Collide))
        {
            hitPoint = waterHit.point;
            return true;
        }

        hitPoint = spotTargetPosition;
        return true;
    }
}
