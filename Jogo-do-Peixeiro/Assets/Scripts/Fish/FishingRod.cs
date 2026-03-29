using UnityEngine;
using UnityEngine.InputSystem;

public class FishingRod : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform rodTip;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask fishingSpotLayer;

    [Header("Cast Settings")]
    [SerializeField] private int linePoints = 25;

    [Header("Force")]
    [SerializeField] private float minCastDistance = 5f;
    [SerializeField] private float maxCastDistance = 20f;
    [SerializeField] private float chargeSpeed = 10f;

    private float currentForce;

    [Header("Temporary Input")]
    [SerializeField] private bool allowMouseTest = true;

    private bool isAiming;
    private FishingSpot currentTargetSpot;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (!allowMouseTest)
            return;

        if (GameManager.instance == null)
            return;

        if (GameManager.instance.currentState != GameManager.GameState.OnBoat &&
            GameManager.instance.currentState != GameManager.GameState.Fishing)
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            StartAim();

        if (isAiming)
            UpdateAim();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            ReleaseCast();
    }

    private void StartAim()
    {
        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
            return;

        isAiming = true;
        currentTargetSpot = null;
        currentForce = minCastDistance;

        if (lineRenderer != null)
            lineRenderer.enabled = true;
    }

    private void UpdateAim()
    {
        // aumenta força enquanto segura
        currentForce += chargeSpeed * Time.deltaTime;
        currentForce = Mathf.Clamp(currentForce, minCastDistance, maxCastDistance);

        Vector3 targetPoint = GetAimPoint(out FishingSpot hitSpot);
        currentTargetSpot = hitSpot;

        DrawArc(rodTip.position, targetPoint);
    }

   private void ReleaseCast()
{
    if (!isAiming)
        return;

    isAiming = false;

    if (lineRenderer != null)
        lineRenderer.enabled = false;

    if (currentTargetSpot != null)
    {
        Debug.Log("Acertou o FishingSpot com a vara");
        currentTargetSpot.StartFishingFromRod();
    }
    else
    {
        Debug.Log("Errou o FishingSpot");
    }
}

    private Vector3 GetAimPoint(out FishingSpot hitSpot)
    {
        hitSpot = null;

        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        Vector3 direction = ray.direction;
        Vector3 origin = rodTip.position;

        // usa a força ao invés de distância fixa
        Vector3 target = origin + direction * currentForce;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, currentForce, fishingSpotLayer))
        {
            hitSpot = hit.collider.GetComponent<FishingSpot>();
            return hit.point;
        }

        return target;
    }

    private void DrawArc(Vector3 startPoint, Vector3 endPoint)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = linePoints;

        float distance = Vector3.Distance(startPoint, endPoint);

        // altura baseada na distância
        float dynamicHeight = distance * 0.5f;

        for (int i = 0; i < linePoints; i++)
        {
            float t = i / (float)(linePoints - 1);

            Vector3 point = Vector3.Lerp(startPoint, endPoint, t);

            float heightOffset = Mathf.Sin(t * Mathf.PI) * dynamicHeight;
            point.y += heightOffset;

            lineRenderer.SetPosition(i, point);
        }
    }
}