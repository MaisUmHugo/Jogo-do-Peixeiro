using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private InteractionUI interactionUI;

    private IInteractable currentInteractable;

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
        currentInteractable = _other.GetComponent<IInteractable>();

        if (currentInteractable != null && interactionUI != null)
            interactionUI.Show();
    }

    private void OnTriggerExit(Collider _other)
    {
        IInteractable interactable = _other.GetComponent<IInteractable>();

        if (interactable == currentInteractable)
        {
            currentInteractable = null;

            if (interactionUI != null)
                interactionUI.Hide();
        }
    }

    private void TryInteract()
    {
        if (GameManager.instance == null)
            return;

        if (GameManager.instance.currentState != GameManager.GameState.OnFoot)
            return;

        if (currentInteractable == null)
            return;

        currentInteractable.Interact();
    }
}