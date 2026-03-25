using UnityEngine;

public class Dock : MonoBehaviour, IInteractable
{
    [SerializeField] private BoatController boat;
    [SerializeField] private Transform exitPoint;

    public void Interact()
    {
        if (GameManager.instance == null || boat == null)
            return;

        if (GameManager.instance.currentState == GameManager.GameState.OnFoot)
        {
            boat.Interact();
        }
        else if (GameManager.instance.currentState == GameManager.GameState.OnBoat)
        {
            //boat.ExitBoat(exitPoint);
        }
    }
}