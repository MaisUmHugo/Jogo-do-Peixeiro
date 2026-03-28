using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private InteractionUI interactionUI;
    [SerializeField] private Camera playerCamera;

    [Header("Focus Settings")]
    [SerializeField] private float maxInteractDistance = 4f;
    [SerializeField] private float minViewDot = 0.35f;

    private IInteractable currentInteractable;
    private Transform currentInteractableTransform;
    private Transform currentPromptPoint;

    private readonly List<MonoBehaviour> interactablesInRange = new List<MonoBehaviour>();

    private void Start()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed += TryInteract;

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed -= TryInteract;
    }

    private void OnTriggerEnter(Collider _other)
    {
        if (_other.transform.root == transform.root)
            return;

        MonoBehaviour[] components = _other.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IInteractable && !interactablesInRange.Contains(component))
            {
                interactablesInRange.Add(component);
            }
        }
    }

    private void OnTriggerExit(Collider _other)
    {
        if (_other.transform.root == transform.root)
            return;

        MonoBehaviour[] components = _other.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IInteractable && interactablesInRange.Contains(component))
            {
                interactablesInRange.Remove(component);
            }
        }

        if (currentInteractable != null && currentInteractableTransform == _other.transform)
        {
            ClearCurrentInteractable();
        }
    }

    private void Update()
    {
        UpdateBestInteractable();

        if (currentInteractable != null &&
     interactionUI != null &&
     GameManager.instance != null &&
     GameManager.instance.currentState != GameManager.GameState.Fishing)
        {
            interactionUI.Show(currentInteractableTransform, transform.root, currentPromptPoint);
        }
        else
        {
            if (interactionUI != null)
                interactionUI.Hide();
        }
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

            Dock dock = component as Dock;
            if (dock != null && !dock.CanInteract())
                continue;

            Transform interactableTransform = component.transform;

            float distance = Vector3.Distance(transform.root.position, interactableTransform.position);
            if (distance > maxInteractDistance)
                continue;

            Vector3 directionToTarget = (interactableTransform.position - playerCamera.transform.position).normalized;
            float viewDot = Vector3.Dot(playerCamera.transform.forward, directionToTarget);

            if (viewDot < minViewDot)
                continue;

            int priority = interactable.GetInteractionPriority();
            float score = priority + (viewDot * 100f) - distance;

            if (score > bestScore)
            {
                bestScore = score;
                bestInteractable = interactable;
                bestTransform = interactableTransform;

                InteractablePromptPoint promptPointComponent = interactableTransform.GetComponent<InteractablePromptPoint>();
                bestPromptPoint = promptPointComponent != null ? promptPointComponent.PromptPoint : null;
            }
        }

        currentInteractable = bestInteractable;
        currentInteractableTransform = bestTransform;
        currentPromptPoint = bestPromptPoint;
    }

    private void TryInteract()
    {
        if (GameManager.instance == null)
            return;

        if (currentInteractable == null)
            return;

        Dock dock = currentInteractable as Dock;
        if (dock != null && !dock.CanInteract())
            return;

        currentInteractable.Interact();
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