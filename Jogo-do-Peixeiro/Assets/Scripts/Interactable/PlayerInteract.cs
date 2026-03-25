using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private InteractionUI interactionUI;

    private IInteractable currentInteractable;
    private Transform currentInteractableTransform;
    private Transform currentPromptPoint;

    private void Start()
    {
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
        IInteractable interactable = _other.GetComponent<IInteractable>();

        if (interactable == null)
            return;

        currentInteractable = interactable;
        currentInteractableTransform = _other.transform;

        InteractablePromptPoint promptPointComponent = _other.GetComponent<InteractablePromptPoint>();
        currentPromptPoint = promptPointComponent != null ? promptPointComponent.PromptPoint : null;

        if (interactionUI != null && GameManager.instance.currentState == GameManager.GameState.OnFoot)
            interactionUI.Show(currentInteractableTransform, transform, currentPromptPoint);
    }

    private void OnTriggerExit(Collider _other)
    {
        IInteractable interactable = _other.GetComponent<IInteractable>();

        if (interactable == currentInteractable)
        {
            currentInteractable = null;
            currentInteractableTransform = null;
            currentPromptPoint = null;

            if (interactionUI != null)
                interactionUI.Hide();
        }
    }

    private void TryInteract()
    {
        if (GameManager.instance == null)
            return;

        if (currentInteractable == null)
            return;

        currentInteractable.Interact();
    }
}