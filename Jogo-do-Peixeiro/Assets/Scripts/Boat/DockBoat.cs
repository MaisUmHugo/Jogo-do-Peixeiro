using UnityEngine;

public class Dock : MonoBehaviour, IInteractable
{
    private const string BoatParkPointName = "boatParkPoint";
    private const string BoatParkPointFallbackName = "BoatParkPoint";
    private const string ExitPointName = "exitPoint";
    private const string ExitPointFallbackName = "ExitPoint";

    [SerializeField] private BoatController boat;
    [SerializeField] private Transform boatParkPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Dock referenceDock;
    [SerializeField] private float dockRange = 6f;

    public Transform BoatParkPoint
    {
        get
        {
            ResolveReferences();
            return boatParkPoint;
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    public void Interact()
    {
        ResolveReferences();

        if (GameManager.instance == null || boat == null)
            return;

        if (GameManager.instance.currentState == GameManager.GameState.OnFoot)
        {
            if (TutorialEvents.ShouldBlockBoatEntry())
            {
                TutorialEvents.NotifyBoatEntryBlocked();
                return;
            }

            boat.EnterBoat();
        }
        else if (GameManager.instance.currentState == GameManager.GameState.OnBoat)
        {
            if (boatParkPoint == null)
            {
                Debug.LogWarning("[Dock] boatParkPoint nao configurado. Preencha no Inspector ou use Reference Dock.");
                return;
            }

            float distanceToDock = Vector3.Distance(boat.transform.position, boatParkPoint.position);

            if (distanceToDock > dockRange)
            {
                Debug.Log("Barco muito longe do dock para estacionar.");
                return;
            }

            boat.ParkBoatAndExit(boatParkPoint, exitPoint);
        }
    }

    // Implementação da interface — sem downcast necessário em PlayerInteract
    public bool CanInteract()
    {
        ResolveReferences();

        if (GameManager.instance == null || boat == null)
            return false;

        if (GameManager.instance.currentState == GameManager.GameState.OnFoot)
            return true;

        if (GameManager.instance.currentState == GameManager.GameState.OnBoat)
        {
            if (boatParkPoint == null)
                return false;

            float distance = Vector3.Distance(boat.transform.position, boatParkPoint.position);
            return distance <= dockRange;
        }

        return false;
    }

    public int GetInteractionPriority()
    {
        return 100;
    }

    private void ResolveReferences()
    {
        if (referenceDock != null)
            CopyMissingReferences(referenceDock);

        if (boat == null)
            boat = FindFirstObjectByType<BoatController>(FindObjectsInactive.Include);

        if (boatParkPoint == null)
            boatParkPoint = FindChildByName(transform.root, BoatParkPointName, BoatParkPointFallbackName);

        if (exitPoint == null)
            exitPoint = FindChildByName(transform.root, ExitPointName, ExitPointFallbackName);

        if (boatParkPoint == null || exitPoint == null)
            CopyMissingReferencesFromOtherDock();
    }

    private void CopyMissingReferences(Dock _dock)
    {
        if (_dock == null || _dock == this)
            return;

        if (boat == null)
            boat = _dock.boat;

        if (boatParkPoint == null)
            boatParkPoint = _dock.boatParkPoint;

        if (exitPoint == null)
            exitPoint = _dock.exitPoint;
    }

    private void CopyMissingReferencesFromOtherDock()
    {
        Dock[] docks = FindObjectsByType<Dock>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Dock dock in docks)
        {
            if (dock == null || dock == this)
                continue;

            if (dock.boatParkPoint == null && dock.exitPoint == null)
                continue;

            CopyMissingReferences(dock);

            if (boatParkPoint != null && exitPoint != null)
                return;
        }
    }

    private Transform FindChildByName(Transform _root, string _primaryName, string _fallbackName)
    {
        if (_root == null)
            return null;

        Transform[] children = _root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == _primaryName || child.name == _fallbackName)
                return child;
        }

        return null;
    }
}
