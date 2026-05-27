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

    [Header("First Boat Dialog")]
    [SerializeField] private bool playFirstBoatDialog = true;
    [SerializeField] private DialogSequenceAsset firstBoatDialog;
    [SerializeField] private RoteiroDialogLibrary roteiroDialogLibrary;

    private Transform cachedPlayerTransform;
    private bool hasPlayedFirstBoatDialog;

    public Transform BoatParkPoint
    {
        get
        {
            ResolveReferences();
            return boatParkPoint;
        }
    }

    public Transform ExitPoint
    {
        get
        {
            ResolveReferences();
            return exitPoint;
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
            if (!IsPlayerInDockRange())
                return;

            if (TutorialEvents.TryHandleBoatEntryRequest(() => boat.EnterBoat()))
            {
                hasPlayedFirstBoatDialog = true;
                return;
            }

            if (TutorialEvents.ShouldBlockBoatEntry())
            {
                TutorialEvents.NotifyBoatEntryBlocked();
                return;
            }

            if (TryPlayFirstBoatDialogThenEnter())
                return;

            boat.EnterBoat();
        }
        else if (GameManager.instance.currentState == GameManager.GameState.OnBoat)
        {
            if (boatParkPoint == null)
            {
            Debug.LogWarning("[Dock] boatParkPoint não configurado. Preencha no Inspector ou use Reference Dock.");
                return;
            }

            if (!IsBoatInDockRange())
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
            return IsPlayerInDockRange() && boat.CanEnterBoat();

        if (GameManager.instance.currentState == GameManager.GameState.OnBoat)
            return IsBoatInDockRange();

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

    private bool IsPlayerInDockRange()
    {
        Transform playerTransform = ResolvePlayerTransform();
        Transform referencePoint = exitPoint != null ? exitPoint : boatParkPoint != null ? boatParkPoint : transform;

        if (playerTransform == null || referencePoint == null)
            return false;

        return Vector3.Distance(playerTransform.position, referencePoint.position) <= dockRange;
    }

    private bool IsBoatInDockRange()
    {
        if (boat == null || boatParkPoint == null)
            return false;

        return Vector3.Distance(boat.transform.position, boatParkPoint.position) <= dockRange;
    }

    private Transform ResolvePlayerTransform()
    {
        if (cachedPlayerTransform != null)
            return cachedPlayerTransform;

        PlayerController playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (playerController != null)
        {
            cachedPlayerTransform = playerController.transform;
            return cachedPlayerTransform;
        }

        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);

        if (playerMove != null)
            cachedPlayerTransform = playerMove.transform;

        return cachedPlayerTransform;
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

    private bool TryPlayFirstBoatDialogThenEnter()
    {
        if (!playFirstBoatDialog || hasPlayedFirstBoatDialog)
            return false;

        DialogSequenceAsset dialog = ResolveFirstBoatDialog();

        if (dialog == null || !dialog.HasLines)
            return false;

        hasPlayedFirstBoatDialog = true;
        return RoteiroDialogPlayback.TryPlaySequence(new[] { dialog }, () => boat.EnterBoat());
    }

    private DialogSequenceAsset ResolveFirstBoatDialog()
    {
        if (firstBoatDialog != null)
            return firstBoatDialog;

        if (roteiroDialogLibrary == null)
            roteiroDialogLibrary = RoteiroDialogPlayback.LoadLibrary();

        return roteiroDialogLibrary != null ? roteiroDialogLibrary.EdgeBarcoAntesIntro : null;
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
