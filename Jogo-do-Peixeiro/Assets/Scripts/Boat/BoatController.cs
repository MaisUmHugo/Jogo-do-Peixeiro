using UnityEngine;

public class BoatController : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject boatCamera;
    [SerializeField] private Transform seatPoint;

    [Header("Player Components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController characterController;

    private bool isPlayerOnBoat;
    private Transform originalParent;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void EnterBoat()
    {
        if (isPlayerOnBoat)
            return;

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

    public void ParkBoatAndExit(Transform _parkPoint, Transform _exitPoint)
    {
        if (!isPlayerOnBoat)
            return;

        Debug.Log("Barco estacionado e player saiu");

        if (_parkPoint != null)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            transform.position = _parkPoint.position;
            transform.rotation = _parkPoint.rotation;
        }

        isPlayerOnBoat = false;
        GameManager.instance.SetState(GameManager.GameState.OnFoot);

        player.transform.SetParent(originalParent);

        if (_exitPoint != null)
        {
            player.transform.position = _exitPoint.position;
            player.transform.rotation = _exitPoint.rotation;
        }
        else
        {
            player.transform.position = transform.position + transform.right * 2f;
        }

        if (playerController != null)
            playerController.enabled = true;

        if (characterController != null)
            characterController.enabled = true;

        if (boatCamera != null)
            boatCamera.SetActive(false);
    }

    public bool IsPlayerOnBoat()
    {
        return isPlayerOnBoat;
    }
}