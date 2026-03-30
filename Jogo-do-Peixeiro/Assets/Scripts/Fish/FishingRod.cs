using UnityEngine;

public class FishingRod : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform rodTip;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask fishingSpotLayer;
    [SerializeField] private ShipInventory shipInventory;

    [Header("Cast Settings")]
    [SerializeField] private int linePoints = 25;
    [SerializeField] private float raycastRadius = 0.5f;

    [Header("Force")]
    [SerializeField] private float minCastDistance = 5f;
    [SerializeField] private float maxCastDistance = 20f;
    [SerializeField] private float chargeSpeed = 10f;

    private float currentForce;
    private bool isAiming;
    private FishingSpot currentTargetSpot;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (shipInventory == null)
            shipInventory = GetComponentInParent<ShipInventory>();

        if (lineRenderer != null)
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

        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
            return;

        if (isAiming)
            UpdateAim();
    }

    private void HandleAimPressed()
    {
        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
            return;

        StartAim();
    }

    private void HandleAimReleased()
    {
        if (!isAiming)
            return;

        ReleaseCast();
    }

    private void StartAim()
    {
        if (rodTip == null || playerCamera == null)
            return;

        isAiming = true;
        currentTargetSpot = null;
        currentForce = minCastDistance;

        if (lineRenderer != null)
            lineRenderer.enabled = true;
    }

    private void UpdateAim()
    {
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
            currentTargetSpot.StartFishingFromRod(shipInventory);
        }
        else
        {
            Debug.Log("Errou o FishingSpot");
        }
    }

    private Vector3 GetAimPoint(out FishingSpot hitSpot)
    {
        hitSpot = null;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

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
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = linePoints;

        float distance = Vector3.Distance(startPoint, endPoint);
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