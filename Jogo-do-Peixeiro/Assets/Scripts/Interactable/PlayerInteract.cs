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

    private void OnTriggerStay(Collider _other)
    {
        // ignora o próprio player
        if (_other.transform.root == transform.root)
            return;

        MonoBehaviour[] components = _other.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IInteractable interactable)
            {
                Dock dock = component as Dock;

                // se for dock e não puder interagir agora, esconde UI e limpa se precisar
                if (dock != null && !dock.CanInteract())
                {
                    if (interactionUI != null)
                        interactionUI.Hide();

                    return;
                }

                currentInteractable = interactable;
                currentInteractableTransform = _other.transform;

                InteractablePromptPoint promptPointComponent = _other.GetComponent<InteractablePromptPoint>();
                currentPromptPoint = promptPointComponent != null ? promptPointComponent.PromptPoint : null;

                if (interactionUI != null &&
                    GameManager.instance != null &&
                    GameManager.instance.currentState == GameManager.GameState.OnFoot)
                {
                    interactionUI.Show(currentInteractableTransform, transform.root, currentPromptPoint);
                }

                return;
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
            if (component is IInteractable interactable && interactable == currentInteractable)
            {
                currentInteractable = null;
                currentInteractableTransform = null;
                currentPromptPoint = null;

                if (interactionUI != null)
                    interactionUI.Hide();

                break;
            }
        }
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
}