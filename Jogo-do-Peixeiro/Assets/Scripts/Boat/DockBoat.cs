using UnityEngine;

public class Dock : MonoBehaviour, IInteractable
{
    [SerializeField] private BoatController boat;
    [SerializeField] private Transform boatParkPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private float dockRange = 6f;

    public void Interact()
    {
        if (GameManager.instance == null || boat == null)
            return;

        if (GameManager.instance.currentState == GameManager.GameState.OnFoot)
        {
            boat.EnterBoat();
        }
        else if (GameManager.instance.currentState == GameManager.GameState.OnBoat)
        {
            float distanceToDock = Vector3.Distance(boat.transform.position, boatParkPoint.position);

            if (distanceToDock > dockRange)
            {
                Debug.Log("Barco muito longe do dock para estacionar.");
                return;
            }

            boat.ParkBoatAndExit(boatParkPoint, exitPoint);
        }
    }

    public bool CanInteract()
    {
        if (GameManager.instance == null || boat == null)
            return false;

        if (GameManager.instance.currentState == GameManager.GameState.OnFoot)
            return true;

        if (GameManager.instance.currentState == GameManager.GameState.OnBoat)
        {
            float distance = Vector3.Distance(boat.transform.position, boatParkPoint.position);
            return distance <= dockRange;
        }

        return false;
    }

    public int GetInteractionPriority()
    {
        return 100;
    }
}