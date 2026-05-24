using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class FishingRod : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform rodTip;
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private ShipInventory shipInventory;

    [Header("Visual")]
    [SerializeField] private GameObject rodVisualRoot;
    [SerializeField] private bool hideRodVisualOutsideFishing = true;
    [SerializeField] private bool autoUseRodTipParentAsVisualRoot = true;
    [SerializeField] private bool disableRodVisualColliders = true;

    [Header("Input")]
    [SerializeField] private bool useInteractToRecallHook = true;

    [Header("Player Facing")]
    [SerializeField] private bool rotatePlayerTowardFishingSpot = true;
    [SerializeField, HideInInspector] private Transform playerFacingRoot;
    [SerializeField] private bool restorePlayerFacingAfterFishing = true;
    [SerializeField] private float playerFacingYawOffset;

    [Header("Hook")]
    [SerializeField] private Transform hookPrefab;
    [SerializeField] private float hookSpeed = 25f;
    [SerializeField] private int arcPoints = 25;
    [SerializeField] private bool useFishingSpotFallbackHit = true;
    [SerializeField] private float spotFallbackWaterProbeHeight = 8f;
    [SerializeField] private float spotFallbackWaterProbeDistance = 24f;
    [SerializeField] private float noSpotWaitBeforeReturn = 1.2f;

    [Header("Splash VFX")]
    [SerializeField] private VisualEffect splashVFXPrefab;
    [SerializeField] private float splashLifetime = 3f;
    [SerializeField] private float splashIntensity = 1f;

    [Header("Fish Movement Visual")]
    [SerializeField] private FishDirectionPull fishDirectionPull;
    [SerializeField] private bool moveHookWithFishPull = true;
    [SerializeField] private bool keepSplashWhileFishing = true;
    [SerializeField] private float fishMovementRadius = 1.4f;
    [SerializeField] private float fishMovementFollowSpeed = 4f;
    [SerializeField] private VisualEffect fishPullVFXPrefab;
    [SerializeField] private float fishPullVFXLifetime = 1.2f;

    [Header("Audio")]
    [SerializeField, InspectorName("Cast SFX")] private AudioClip castSfx;
    [SerializeField, InspectorName("Splash SFX")] private AudioClip splashSfx;
    [SerializeField, InspectorName("Fish Pull SFX")] private AudioClip fishPullSfx;
    [SerializeField, Range(0f, 1f), InspectorName("Cast SFX Volume")] private float castSfxVolume = 1f;
    [SerializeField, Range(0f, 1f), InspectorName("Splash SFX Volume")] private float splashSfxVolume = 1f;
    [SerializeField, Range(0f, 1f), InspectorName("Fish Pull SFX Volume")] private float fishPullSfxVolume = 0.65f;

    private bool hookTraveling;
    private bool hookReturning;
    private bool hookWaitingInWater;

    private FishingSpot currentTargetSpot;
    private VisualEffect currentSplashInstance;
    private Transform currentHook;
    private Coroutine hookRoutine;
    private Vector3 hookWaterOrigin;
    private Vector3 fishMovementTarget;
    private int lastFishMovementPromptId = -1;
    private Vector3[] arcCache;
    private Vector3 currentPlayerFacingTargetPoint;
    private PlayerAnimationController playerAnimationController;
    private bool hasPlayerFacingTarget;

    #endregion

    #region Unity Lifecycle

    private void OnValidate()
    {
        hookSpeed = Mathf.Max(0.1f, hookSpeed);
        arcPoints = Mathf.Max(2, arcPoints);
        spotFallbackWaterProbeHeight = Mathf.Max(0f, spotFallbackWaterProbeHeight);
        spotFallbackWaterProbeDistance = Mathf.Max(0.1f, spotFallbackWaterProbeDistance);
        noSpotWaitBeforeReturn = Mathf.Max(0f, noSpotWaitBeforeReturn);
        splashLifetime = Mathf.Max(0f, splashLifetime);
        splashIntensity = Mathf.Max(0f, splashIntensity);
        fishMovementRadius = Mathf.Max(0f, fishMovementRadius);
        fishMovementFollowSpeed = Mathf.Max(0f, fishMovementFollowSpeed);
        fishPullVFXLifetime = Mathf.Max(0f, fishPullVFXLifetime);
    }

    private void Awake()
    {
        if (shipInventory == null)
            shipInventory = GetComponentInParent<ShipInventory>();

        if (fishDirectionPull == null)
            fishDirectionPull = FindFirstObjectByType<FishDirectionPull>(FindObjectsInactive.Include);

        ResolveRodVisualRoot();
        DisableRodVisualColliders();
        SetRodVisualVisible(!hideRodVisualOutsideFishing);

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void Start()
    {
        if (InputHandler.instance != null && useInteractToRecallHook)
            InputHandler.instance.onInteractPressed += HandleInteractPressed;
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed -= HandleInteractPressed;
    }

    #endregion

    #region Update And Public Fishing API

    private void Update()
    {
        if (IsGameplayInputBlockedByUI())
        {
            UpdateRodVisualVisibility();
            return;
        }

        if (GameManager.instance == null)
        {
            UpdateRodVisualVisibility();
            return;
        }

        UpdateRodVisualVisibility();
        UpdateFishMovementVisual();
        UpdateHookLine();
    }

    private void LateUpdate()
    {
        UpdatePlayerFacingDirection();
    }

    public bool CanCastToSpot(FishingSpot _spot)
    {
        if (_spot == null ||
            shipInventory == null ||
            rodTip == null ||
            hookPrefab == null ||
            lineRenderer == null)
        {
            return false;
        }

        if (IsGameplayInputBlockedByUI())
            return false;

        if (hookTraveling || hookReturning || hookWaitingInWater)
            return false;

        if (GameManager.instance == null ||
            GameManager.instance.currentState != GameManager.GameState.OnBoat)
        {
            return false;
        }

        return _spot.CanStartFishingFromInteraction(shipInventory);
    }

    public bool TryCastToSpot(FishingSpot _spot)
    {
        if (!CanCastToSpot(_spot))
            return false;

        if (rodTip == null || hookPrefab == null || lineRenderer == null)
            return false;

        Vector3 targetPoint = _spot.GetCastTargetPosition(_spot.transform.position);
        currentTargetSpot = _spot;
        StartPlayerFacingTarget(targetPoint);
        hookTraveling = true;
        SetRodVisualVisible(true);
        arcCache = CalculateArcPoints(rodTip.position, targetPoint);

        lineRenderer.enabled = false;

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.Fishing);

        PlayCastSfx();
        hookRoutine = StartCoroutine(AnimateHook());
        return true;
    }

    public void ReturnHookAfterFishing()
    {
        if (!hookWaitingInWater || hookReturning)
            return;

        if (hookRoutine != null)
            StopCoroutine(hookRoutine);

        hookRoutine = StartCoroutine(ReturnHook());
    }

    public Vector3 GetHookWorldPosition()
    {
        return currentHook != null ? currentHook.position : rodTip.position;
    }

    public void PlaySuccessSplash(FishSkillCheck.FeedbackResult _result)
    {
        if (currentHook == null)
            return;

        float intensityMultiplier = 1f;

        switch (_result)
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

    #endregion

    #region Hook Travel And Recall

    private void HandleInteractPressed()
    {
        if (!useInteractToRecallHook)
            return;

        if (!hookWaitingInWater)
            return;

        RecallHook();
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

    private void RecallHook()
    {
        if (!hookWaitingInWater)
            return;

        if (ShouldBlockHookRecall())
            return;

        if (FishingManager.instance != null && FishingManager.instance.IsFishing)
            FishingManager.instance.CancelFishing();

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
            UpdateRodVisualVisibility();
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
        UpdateRodVisualVisibility();

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
    }

    private void ResetHookState()
    {
        hookTraveling = false;
        hookReturning = false;
        hookWaitingInWater = false;
        currentTargetSpot = null;
        lastFishMovementPromptId = -1;
        hookRoutine = null;
        ClearPlayerFacingTarget();
    }

    #endregion

    #region Rod Visual State

    private void ResolveRodVisualRoot()
    {
        if (rodVisualRoot != null ||
            !autoUseRodTipParentAsVisualRoot ||
            rodTip == null ||
            rodTip.parent == null ||
            rodTip.parent == transform)
        {
            return;
        }

        rodVisualRoot = rodTip.parent.gameObject;
    }

    private void DisableRodVisualColliders()
    {
        if (!disableRodVisualColliders || rodVisualRoot == null)
            return;

        Collider[] colliders = rodVisualRoot.GetComponentsInChildren<Collider>(true);

        foreach (Collider rodCollider in colliders)
            rodCollider.enabled = false;
    }

    private void UpdateRodVisualVisibility()
    {
        if (!hideRodVisualOutsideFishing)
            return;

        bool shouldShowRod = hookTraveling ||
                             hookReturning ||
                             hookWaitingInWater ||
                             (FishingManager.instance != null && FishingManager.instance.IsFishing);

        SetRodVisualVisible(shouldShowRod);
    }

    private void SetRodVisualVisible(bool _visible)
    {
        if (rodVisualRoot == null || rodVisualRoot.activeSelf == _visible)
            return;

        rodVisualRoot.SetActive(_visible);
    }

    private bool ShouldBlockHookRecall()
    {
        if (IsGameplayInputBlockedByUI())
            return true;

        return FishingManager.instance != null &&
               FishingManager.instance.IsFishing &&
               FishingManager.instance.HasFishBitten;
    }

    private bool IsGameplayInputBlockedByUI()
    {
        if (PlayerCamera.IsCameraLocked)
            return true;

        return GameManager.instance != null && GameManager.instance.IsGameplayBlocked();
    }

    private void StartPlayerFacingTarget(Vector3 _targetPoint)
    {
        currentPlayerFacingTargetPoint = _targetPoint;
        hasPlayerFacingTarget = true;
        UpdatePlayerFacingDirection();
    }

    private void UpdatePlayerFacingDirection()
    {
        if (!hasPlayerFacingTarget)
            return;

        if (!rotatePlayerTowardFishingSpot)
        {
            ClearPlayerFacingTarget();
            return;
        }

        if (GameManager.instance == null || GameManager.instance.IsGameplayBlocked())
            return;

        bool shouldUpdateFacing = hookTraveling ||
                                  hookWaitingInWater ||
                                  (FishingManager.instance != null && FishingManager.instance.IsFishing);

        if (shouldUpdateFacing)
        {
            PlayerAnimationController animationController = ResolvePlayerAnimationController();
            if (animationController != null)
                animationController.SetFishingFacingTarget(currentPlayerFacingTargetPoint, playerFacingYawOffset);
        }
    }

    private void ClearPlayerFacingTarget()
    {
        hasPlayerFacingTarget = false;

        PlayerAnimationController animationController = ResolvePlayerAnimationController();
        if (animationController != null && restorePlayerFacingAfterFishing)
            animationController.ClearFishingFacingTarget();
    }

    private PlayerAnimationController ResolvePlayerAnimationController()
    {
        if (playerAnimationController != null)
            return playerAnimationController;

        if (playerAnimationController == null)
            playerAnimationController = FindFirstObjectByType<PlayerAnimationController>(FindObjectsInactive.Include);

        return playerAnimationController;
    }

    #endregion

    #region Hook Line And VFX

    private void UpdateHookLine()
    {
        if (lineRenderer == null || !lineRenderer.enabled || currentHook == null)
            return;

        lineRenderer.SetPosition(0, rodTip.position);
        lineRenderer.SetPosition(1, currentHook.position);
    }

    private void SpawnSplash(Vector3 _position)
    {
        if (splashVFXPrefab == null)
            return;

        DestroyCurrentSplash();

        currentSplashInstance = Instantiate(
            splashVFXPrefab,
            _position,
            Quaternion.identity
        );

        if (currentSplashInstance.HasFloat("Intensity"))
            currentSplashInstance.SetFloat("Intensity", splashIntensity);

        currentSplashInstance.Play();

        if (!keepSplashWhileFishing)
            Destroy(currentSplashInstance.gameObject, splashLifetime);

        PlaySplashSfx(_position);
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

        if (instance.HasVector3("Direction"))
            instance.SetVector3("Direction", fishWorldDir);

        instance.Play();

        Destroy(instance.gameObject, fishPullVFXLifetime);
    }

    #endregion

    #region Audio

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

    #endregion

    #region Hook Movement Helpers

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

    private Vector3[] CalculateArcPoints(Vector3 _startPoint, Vector3 _endPoint)
    {
        Vector3[] points = new Vector3[arcPoints];
        float distance = Vector3.Distance(_startPoint, _endPoint);
        float height = distance * 0.5f;

        for (int i = 0; i < arcPoints; i++)
        {
            float t = i / (float)(arcPoints - 1);

            Vector3 point = Vector3.Lerp(_startPoint, _endPoint, t);
            point.y += Mathf.Sin(t * Mathf.PI) * height;

            points[i] = point;
        }

        return points;
    }

    #endregion

    #region Success Splash Feedback

    private void SpawnScaledSplash(Vector3 _position, float _multiplier)
    {
        if (splashVFXPrefab == null)
            return;

        VisualEffect splash = Instantiate(splashVFXPrefab, _position, Quaternion.identity);
        float finalIntensity = splashIntensity * _multiplier;

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

    #endregion

    #region Water Detection

    private bool TryGetWaterHit(out Vector3 _hitPoint)
    {
        _hitPoint = Vector3.zero;

        if (arcCache == null || arcCache.Length < 2)
            return false;

        for (int i = 0; i < arcCache.Length - 1; i++)
        {
            Vector3 a = arcCache[i];
            Vector3 b = arcCache[i + 1];

            Vector3 dir = b - a;
            float dist = dir.magnitude;

            if (Physics.Raycast(a, dir.normalized, out RaycastHit hit, dist, waterLayer, QueryTriggerInteraction.Collide))
            {
                _hitPoint = hit.point;
                return true;
            }
        }

        return false;
    }

    private bool TryGetFishingSpotFallbackHit(out Vector3 _hitPoint)
    {
        _hitPoint = Vector3.zero;

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
            _hitPoint = waterHit.point;
            return true;
        }

        _hitPoint = spotTargetPosition;
        return true;
    }

    #endregion
}
