using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using UnityEngine.InputSystem;

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

    [SerializeField] private int arcPoints = 25;
    [SerializeField] private float raycastRadius = 0.5f;

    [SerializeField] private float minCastDistance = 5f;
    [SerializeField] private float maxCastDistance = 20f;
    [SerializeField] private float chargeSpeed = 10f;

    private float currentForce;
    private bool isAiming;
    private bool hookTraveling;
    private bool hookReturning;
    private bool hookWaitingInWater;

    private FishingSpot currentTargetSpot;
    private VisualEffect currentSplashInstance;
    private Transform currentHook;
    private Coroutine hookRoutine;

    private Vector3[] arcCache;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (shipInventory == null)
            shipInventory = GetComponentInParent<ShipInventory>();

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

        UpdateHookLine();

        if (hookWaitingInWater &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            (FishingManager.instance == null || !FishingManager.instance.IsFishing))
        {
            RecallHook();
        }
    }

    private void HandleAimPressed()
    {
        if (hookTraveling || hookReturning || hookWaitingInWater)
            return;

        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
            return;

        isAiming = true;
        currentTargetSpot = null;
        currentForce = minCastDistance;

        lineRenderer.enabled = true;
    }

    private void HandleAimReleased()
    {
        if (!isAiming)
            return;

        isAiming = false;
        hookTraveling = true;

        arcCache = new Vector3[arcPoints];

        for (int i = 0; i < arcPoints; i++)
            arcCache[i] = lineRenderer.GetPosition(i);

        lineRenderer.enabled = false;

        GameManager.instance.SetState(GameManager.GameState.Fishing);

        hookRoutine = StartCoroutine(AnimateHook());
    }

    private void UpdateAim()
    {
        currentForce += chargeSpeed * Time.deltaTime;
        currentForce = Mathf.Clamp(currentForce, minCastDistance, maxCastDistance);

        Vector3 targetPoint = GetAimPoint(out FishingSpot hitSpot);
        currentTargetSpot = hitSpot;

        DrawArc(rodTip.position, targetPoint);
    }

    private IEnumerator AnimateHook()
    {
        if (currentHook != null)
            Destroy(currentHook.gameObject);

        currentHook = Instantiate(hookPrefab, rodTip.position, Quaternion.identity);

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

        if (TryGetWaterHit(out Vector3 waterHit))
        {
            SpawnSplash(waterHit);
            currentHook.position = waterHit;
        }

        hookTraveling = false;
        hookWaitingInWater = true;

        if (currentTargetSpot != null)
            currentTargetSpot.StartFishingFromRod(shipInventory);
    }

    private void RecallHook()
    {
        if (!hookWaitingInWater)
            return;

        if (FishingManager.instance != null && FishingManager.instance.IsFishing)
            return;

        if (hookRoutine != null)
            StopCoroutine(hookRoutine);

        hookRoutine = StartCoroutine(ReturnHook());
    }

    private IEnumerator ReturnHook()
    {
        hookReturning = true;
        hookWaitingInWater = false;

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

        lineRenderer.enabled = false;

        hookTraveling = false;
        hookReturning = false;

        GameManager.instance.SetState(GameManager.GameState.OnBoat);
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

        if (currentSplashInstance != null)
            Destroy(currentSplashInstance.gameObject);

        currentSplashInstance = Instantiate(
            splashVFXPrefab,
            position,
            Quaternion.identity
        );

        float normalizedForce = Mathf.InverseLerp(minCastDistance, maxCastDistance, currentForce);

        if (currentSplashInstance.HasFloat("Intensity"))
            currentSplashInstance.SetFloat("Intensity", normalizedForce);

        currentSplashInstance.Play();

        Destroy(currentSplashInstance.gameObject, splashLifetime);
    }

    private Vector3 GetAimPoint(out FishingSpot hitSpot)
    {
        hitSpot = null;

        Ray ray = playerCamera.ScreenPointToRay(
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
        );

        Vector3 origin = rodTip.position;
        Vector3 direction = ray.direction;

        Vector3 target = origin + direction * currentForce;

        if (Physics.SphereCast(origin, raycastRadius, direction, out RaycastHit hit, currentForce, fishingSpotLayer))
        {
            hitSpot = hit.collider.GetComponent<FishingSpot>();
            return hit.point;
        }

        return target;
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
}