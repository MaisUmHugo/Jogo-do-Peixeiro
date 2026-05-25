using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private InteractionUI interactionUI;
    [SerializeField] private Camera playerCamera;

    [Header("Player Reference")]
    [Tooltip("Arraste o GameObject do Player aqui. Necessario quando o trigger e filho do Player.")]
    [SerializeField] private Transform playerRoot;

    [Header("Focus Settings")]
    [SerializeField] private float minViewDot = 0.35f;
    [SerializeField] private float screenCenterWeight = 50f;
    [SerializeField, Min(0.1f)] private float maxInteractionDistance = 4f;

    [Header("Refresh Settings")]
    [SerializeField] private float teleportRefreshRadius = 8f;

    private IInteractable currentInteractable;
    private Transform currentInteractableTransform;
    private Transform currentPromptPoint;

    private readonly List<MonoBehaviour> interactablesInRange = new List<MonoBehaviour>();

    private void Start()
    {
        // Fallback: se playerRoot nao foi preenchido, sobe na hierarquia ate o pai com CharacterController.
        if (playerRoot == null)
        {
            CharacterController cc = GetComponentInParent<CharacterController>();
            if (cc != null)
                playerRoot = cc.transform;
        }

        // Ultimo fallback.
        if (playerRoot == null)
        {
            playerRoot = transform.parent != null ? transform.parent : transform;
            Debug.LogWarning("[PlayerInteract] playerRoot não encontrado automaticamente. Preencha o campo no Inspector.");
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
            Debug.LogWarning("[PlayerInteract] Nenhuma camera encontrada. Preencha o campo playerCamera no Inspector ou adicione a tag MainCamera.");

        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed += TryInteract;
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed -= TryInteract;
    }

    private void OnTriggerEnter(Collider _other)
    {
        TryAddInteractablesFromCollider(_other);
    }

    private void OnTriggerExit(Collider _other)
    {
        // Usa IsChildOf(playerRoot) em vez de transform.root para funcionar com qualquer hierarquia
        if (playerRoot != null && _other.transform.IsChildOf(playerRoot))
            return;

        MonoBehaviour[] components = _other.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IInteractable && interactablesInRange.Contains(component))
                interactablesInRange.Remove(component);
        }

        ClearCurrentInteractable();
    }

    public void RefreshInteractablesAfterTeleport()
    {
        interactablesInRange.Clear();
        ClearCurrentInteractable();

        Physics.SyncTransforms();

        if (playerRoot == null)
            return;

        Collider[] nearbyColliders = Physics.OverlapSphere(
            playerRoot.position,
            teleportRefreshRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider nearbyCollider in nearbyColliders)
            TryAddInteractablesFromCollider(nearbyCollider);
    }

    private void TryAddInteractablesFromCollider(Collider _other)
    {
        if (_other == null)
            return;

        if (playerRoot != null && _other.transform.IsChildOf(playerRoot))
            return;

        MonoBehaviour[] components = _other.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IInteractable && !interactablesInRange.Contains(component))
                interactablesInRange.Add(component);
        }
    }

    private void Update()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (IsInteractionBlocked())
        {
            ClearCurrentInteractable();
            return;
        }

        UpdateBestInteractable();

        bool shouldShowUI =
            currentInteractable != null &&
            interactionUI != null;

        if (shouldShowUI)
            interactionUI.Show(currentInteractableTransform, playerRoot, currentPromptPoint);
        else if (interactionUI != null)
            interactionUI.Hide();
    }

    private void UpdateBestInteractable()
    {
        IInteractable bestInteractable = null;
        Transform bestTransform = null;
        Transform bestPromptPoint = null;
        float bestScore = float.MinValue;

        for (int i = interactablesInRange.Count - 1; i >= 0; i--)
        {
            MonoBehaviour component = interactablesInRange[i];

            if (component == null)
            {
                interactablesInRange.RemoveAt(i);
                continue;
            }

            if (component is not IInteractable interactable)
                continue;

            if (!interactable.CanInteract())
                continue;

            Transform interactableTransform = component.transform;
            Transform promptPoint = GetBestPromptPoint(interactableTransform, interactable, out float score);

            if (score == float.MinValue)
                continue;

            if (score > bestScore)
            {
                bestScore = score;
                bestInteractable = interactable;
                bestTransform = interactableTransform;
                bestPromptPoint = promptPoint;
            }
        }

        currentInteractable = bestInteractable;
        currentInteractableTransform = bestTransform;
        currentPromptPoint = bestPromptPoint;
    }

    private Transform GetBestPromptPoint(Transform _interactableTransform, IInteractable _interactable, out float _bestScore)
    {
        _bestScore = float.MinValue;
        Transform bestPromptPoint = null;

        InteractablePromptPoint[] promptPointComponents = _interactableTransform.GetComponentsInChildren<InteractablePromptPoint>();

        if (promptPointComponents.Length == 0)
        {
            if (TryScoreInteractableArea(_interactableTransform, _interactable.GetInteractionPriority(), out _bestScore))
                return null;

            return null;
        }

        foreach (InteractablePromptPoint promptPointComponent in promptPointComponents)
        {
            foreach (Transform candidatePromptPoint in promptPointComponent.GetPromptPoints())
            {
                if (candidatePromptPoint == null)
                    continue;

                if (!TryScorePrompt(candidatePromptPoint.position, _interactable.GetInteractionPriority(), out float score))
                    continue;

                if (score > _bestScore)
                {
                    _bestScore = score;
                    bestPromptPoint = candidatePromptPoint;
                }
            }
        }

        return bestPromptPoint;
    }

    private void TryInteract()
    {
        if (IsInteractionBlocked())
            return;

        if (currentInteractable == null)
            return;

        if (!currentInteractable.CanInteract())
            return;

        if (!IsCurrentInteractableInRange())
        {
            ClearCurrentInteractable();
            return;
        }

        currentInteractable.Interact();
    }

    private bool TryScorePrompt(Vector3 _worldPosition, int _priority, out float _score)
    {
        _score = float.MinValue;

        if (playerRoot == null)
            return false;

        float distance = Vector3.Distance(playerRoot.position, _worldPosition);

        if (distance > maxInteractionDistance)
            return false;

        if (playerCamera == null)
        {
            _score = _priority - distance;
            return true;
        }

        Vector3 directionToTarget = (_worldPosition - playerCamera.transform.position).normalized;
        float viewDot = Vector3.Dot(playerCamera.transform.forward, directionToTarget);

        if (viewDot < minViewDot)
            return false;

        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(_worldPosition);

        if (viewportPoint.z <= 0f)
            return false;

        if (viewportPoint.x < 0f || viewportPoint.x > 1f ||
            viewportPoint.y < 0f || viewportPoint.y > 1f)
            return false;

        Vector2 viewportPosition = new Vector2(viewportPoint.x, viewportPoint.y);
        float screenCenterDistance = Vector2.Distance(viewportPosition, new Vector2(0.5f, 0.5f));

        _score = _priority + (viewDot * 100f) - distance - (screenCenterDistance * screenCenterWeight);
        return true;
    }

    private bool TryScoreInteractableArea(Transform _interactableTransform, int _priority, out float _score)
    {
        Vector3 scorePosition = _interactableTransform.position;

        if (TryGetClosestInteractablePoint(_interactableTransform, out Vector3 closestPoint))
            scorePosition = closestPoint;

        return TryScorePrompt(scorePosition, _priority, out _score);
    }

    private bool TryGetClosestInteractablePoint(Transform _interactableTransform, out Vector3 _closestPoint)
    {
        _closestPoint = Vector3.zero;

        if (_interactableTransform == null || playerRoot == null)
            return false;

        Collider[] colliders = _interactableTransform.GetComponentsInChildren<Collider>(true);
        bool foundPoint = false;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider == null ||
                !collider.enabled ||
                !collider.gameObject.activeInHierarchy ||
                collider.transform.IsChildOf(playerRoot))
            {
                continue;
            }

            Vector3 closestPoint = collider.ClosestPoint(playerRoot.position);
            float sqrDistance = (closestPoint - playerRoot.position).sqrMagnitude;

            if (sqrDistance >= bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            _closestPoint = closestPoint;
            foundPoint = true;
        }

        return foundPoint;
    }

    private bool IsInteractionBlocked()
    {
        if (GameManager.instance == null)
            return true;

        if (GameManager.instance.currentState == GameManager.GameState.Fishing)
            return true;

        return GameManager.instance.IsGameplayBlocked();
    }

    private bool IsCurrentInteractableInRange()
    {
        if (playerRoot == null)
            return false;

        Vector3 referencePosition = currentPromptPoint != null
            ? currentPromptPoint.position
            : TryGetClosestInteractablePoint(currentInteractableTransform, out Vector3 closestPoint)
                ? closestPoint
                : currentInteractableTransform != null
                    ? currentInteractableTransform.position
                    : playerRoot.position;

        return Vector3.Distance(playerRoot.position, referencePosition) <= maxInteractionDistance;
    }

    private void ClearCurrentInteractable()
    {
        currentInteractable = null;
        currentInteractableTransform = null;
        currentPromptPoint = null;

        if (interactionUI != null)
            interactionUI.Hide();
    }
}
