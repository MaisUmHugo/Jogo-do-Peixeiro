using UnityEngine;

public class BoatController : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject boatCamera;
    [SerializeField] private Transform seatPoint;

    [Header("Player Components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController characterController;

    private bool isPlayerOnBoat;
    private Transform originalParent;

    public void Interact()
    {
        if (!isPlayerOnBoat)
            EnterBoat();
        else
            ExitBoat();
    }

    private void EnterBoat()
    {
        Debug.Log("Entrou no barco");

        isPlayerOnBoat = true;
        GameManager.instance.SetState(GameManager.GameState.OnBoat);

        originalParent = player.transform.parent;

        if (characterController != null)
            characterController.enabled = false;

        player.transform.SetParent(seatPoint);
        player.transform.position = seatPoint.position;
        player.transform.rotation = seatPoint.rotation;

        if (playerController != null)
            playerController.enabled = false;

        if (boatCamera != null)
            boatCamera.SetActive(true);
    }

    public void ExitBoat()
    {
        Debug.Log("Saiu do barco");

        isPlayerOnBoat = false;
        GameManager.instance.SetState(GameManager.GameState.OnFoot);

        player.transform.SetParent(originalParent);
        player.transform.position = transform.position + transform.right * 2f;

        if (playerController != null)
            playerController.enabled = true;

        if (characterController != null)
            characterController.enabled = true;

        if (boatCamera != null)
            boatCamera.SetActive(false);
    }

    //public void ExitBoat(Transform _exitPoint)
    //{
    //    Debug.Log("Saiu do barco");

    //    isPlayerOnBoat = false;
    //    GameManager.instance.SetState(GameManager.GameState.OnFoot);

    //    player.transform.SetParent(originalParent);

    //    if (_exitPoint != null)
    //    {
    //        player.transform.position = _exitPoint.position;
    //        player.transform.rotation = _exitPoint.rotation;
    //    }
    //    else
    //    {
    //        player.transform.position = transform.position + transform.right * 2f;
    //    }

    //    if (playerController != null)
    //        playerController.enabled = true;

    //    if (characterController != null)
    //        characterController.enabled = true;

    //    if (boatCamera != null)
    //        boatCamera.SetActive(false);
    //}
}