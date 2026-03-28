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
    [SerializeField] private float maxCastDistance = 20f;
    [SerializeField] private float arcHeight = 4f;
    [SerializeField] private int linePoints = 25;

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

        GameManager.instance.SetState(GameManager.GameState.Fishing);

        if (lineRenderer != null)
            lineRenderer.enabled = true;
    }

    private void UpdateAim()
    {
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
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
        }
    }

    private Vector3 GetAimPoint(out FishingSpot hitSpot)
    {
        hitSpot = null;

        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, maxCastDistance, fishingSpotLayer))
        {
            hitSpot = hit.collider.GetComponent<FishingSpot>();
            return hit.point;
        }

        return ray.origin + ray.direction * maxCastDistance;
    }

    private void DrawArc(Vector3 startPoint, Vector3 endPoint)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = linePoints;

        for (int i = 0; i < linePoints; i++)
        {
            float t = i / (float)(linePoints - 1);

            Vector3 point = Vector3.Lerp(startPoint, endPoint, t);
            float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            point.y += heightOffset;

            lineRenderer.SetPosition(i, point);
        }
    }
}