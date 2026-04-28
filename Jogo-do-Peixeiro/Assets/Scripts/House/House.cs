using UnityEngine;

public class House : MonoBehaviour, IInteractable
{
    [Header("Referências")]
    [SerializeField] private DayCycle dayCycle;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private GameObject sleepUI;

    private Transform player;
    public void Interact()
    {
        player = FindFirstObjectByType<PlayerInteract>().transform;

        sleepUI.SetActive(true);

        GameManager.instance.SetState(GameManager.GameState.InUI);
    }

    public bool CanInteract()
    {
        return true;
    }

    public int GetInteractionPriority()
    {
        return 0;
    }
    public void ConfirmSleep()
    {
        sleepUI.SetActive(false);

        dayCycle.NextDay();

        player.position = exitPoint.position;
        player.rotation = exitPoint.rotation;

        GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }

    public void CancelSleep()
    {
        sleepUI.SetActive(false);

        GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }
}