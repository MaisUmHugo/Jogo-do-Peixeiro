public interface IInteractable
{
    void Interact();

    int GetInteractionPriority();

    bool CanInteract() => true;
}
