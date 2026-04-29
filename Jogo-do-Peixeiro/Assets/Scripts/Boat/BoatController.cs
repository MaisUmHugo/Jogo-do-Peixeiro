using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatController : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject boatCamera;
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Vector3 OffsetRotacao;

    [Header("Player Components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController characterController;

    private bool isPlayerOnBoat;
    private Transform originalParent;
    private Rigidbody rb;
    private BoatMotor boatMotor;
    private Floater[] floaters;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boatMotor = GetComponent<BoatMotor>();
        floaters = GetComponentsInChildren<Floater>();

        bool isMainMenu = SceneManager.GetActiveScene().name == "Main Menu";

        if (isMainMenu)
        {
            // força o barco a sempre flutuar no menu
            SetBoatPhysics(true);

            // garante que o rigidbody NÃO seja kinematic
            if (rb != null)
                rb.isKinematic = false;
        }
        else
        {
            // comportamento normal
            if (!isPlayerOnBoat)
                SetBoatPhysics(false);
        }
    }

    private void SetBoatPhysics(bool active)
    {
        if (rb != null)
        {
            // Trava o Rigidbody (isKinematic = true) quando o player NÃO está no barco
            rb.isKinematic = !active;

            if (!active)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }

        // Avisa cada floater se ele deve calcular flutuação ou não
        if (!active && boatMotor != null)
            boatMotor.ResetMotorState();

        if (floaters != null)
        {
            foreach (var f in floaters)
            {
                f.canFloat = active;
            }
        }
    }

    public void EnterBoat()
    {
        if (isPlayerOnBoat) return;

        Debug.Log("Entrou no barco");
        isPlayerOnBoat = true;

        // 1. Liga a física e flutuação
        SetBoatPhysics(true);

        // 2. Lógica de Jogo e Parentesco
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

        TutorialEvents.NotifyBoatEntered();
    }

    public void ParkBoatAndExit(Transform _parkPoint, Transform _exitPoint)
    {
        if (!isPlayerOnBoat) return;

        Debug.Log("Barco estacionado e player saiu");

        // 1. Desliga a física (Barco congela)
        // 2. Reposiciona o barco no ponto de estacionamento se houver um
        if (_parkPoint != null)
        {
            transform.position = _parkPoint.position;
            transform.rotation = _parkPoint.rotation * Quaternion.Euler(OffsetRotacao);
        }

        // 3. Lógica do Player saindo
        if (player != null && _exitPoint != null)
        {
            player.transform.position = _exitPoint.position;
            player.transform.rotation = _exitPoint.rotation * Quaternion.Euler(OffsetRotacao);
        }
        else if (player != null)
        {
            player.transform.position = transform.position + transform.right * 2f;
        }

        ExitBoatState();
    }

    public void ExitBoatWithoutMovingPlayer()
    {
        if (!isPlayerOnBoat)
            return;

        Debug.Log("Player saiu do barco sem reposicionar.");
        ExitBoatState();
    }

    public void ExitBoatForTeleport()
    {
        if (!isPlayerOnBoat)
            return;

        Debug.Log("Player saiu do barco para teleporte.");
        ExitBoatState(false, false, false);
    }

    private void ExitBoatState(
        bool _restorePlayerControl = true,
        bool _notifyTutorial = true,
        bool _setGameState = true)
    {
        isPlayerOnBoat = false;
        SetBoatPhysics(false);

        if (_setGameState && GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);

        if (player != null)
            player.transform.SetParent(originalParent, true);

        if (playerController != null)
            playerController.enabled = _restorePlayerControl;

        if (characterController != null)
            characterController.enabled = _restorePlayerControl;

        if (boatCamera != null)
            boatCamera.SetActive(false);

        if (_notifyTutorial)
            TutorialEvents.NotifyBoatExited();
    }

    public bool IsPlayerOnBoat()
    {
        return isPlayerOnBoat;
    }
}
