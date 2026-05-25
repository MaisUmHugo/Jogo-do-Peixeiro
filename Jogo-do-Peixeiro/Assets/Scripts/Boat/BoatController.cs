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
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private PlayerAnimationController playerAnimationController;

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
        ResolvePlayerReferences();

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
        if (!CanEnterBoat())
            return;

        Debug.Log("Entrou no barco");
        isPlayerOnBoat = true;
        ResolvePlayerReferences();
        ResetPlayerRuntimeState();
        EnsurePlayerAnimatorEnabled();

        // 1. Liga a física e flutuação
        SetBoatPhysics(true);

        // 2. Lógica de Jogo e Parentesco
        GameManager.instance.SetState(GameManager.GameState.OnBoat);
        originalParent = player.transform.parent;

        if (characterController != null)
            characterController.enabled = false;

        player.transform.SetParent(seatPoint, false);
        ApplyPlayerSeatTransform();

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
        ExitBoatState();
        PlacePlayerAtDockExit(_exitPoint, _parkPoint);

        if (playerMove != null && player != null)
            playerMove.SetSafeRespawnPosition(player.transform.position);
    }

    public void ReturnToParkPoint(Transform _parkPoint)
    {
        if (_parkPoint == null)
            return;

        SetBoatPhysics(false);

        transform.position = _parkPoint.position;
        transform.rotation = _parkPoint.rotation * Quaternion.Euler(OffsetRotacao);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        if (boatMotor != null)
            boatMotor.ResetMotorState();
    }

    public void PlaceForSceneTransition(Transform _boatPoint, Transform _playerPoint, bool _putPlayerOnBoat)
    {
        ResolvePlayerReferences();

        if (isPlayerOnBoat)
            ExitBoatForTeleport();

        Transform boatPoint = _boatPoint != null ? _boatPoint : transform;
        transform.SetPositionAndRotation(
            boatPoint.position,
            boatPoint.rotation * Quaternion.Euler(OffsetRotacao)
        );

        SetBoatPhysics(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        if (boatMotor != null)
            boatMotor.ResetMotorState();

        if (_putPlayerOnBoat)
        {
            if (GameManager.instance != null)
                GameManager.instance.SetState(GameManager.GameState.OnFoot);

            EnterBoat();
            return;
        }

        PlacePlayerAtSceneTransitionPoint(_playerPoint);

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);
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

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        if (boatMotor != null)
            boatMotor.ResetMotorState();
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

        if (playerAnimationController != null)
            playerAnimationController.ResetBoatVisualOffset();

        if (player != null)
            player.transform.SetParent(originalParent, true);

        if (playerController != null)
            playerController.enabled = _restorePlayerControl;

        if (characterController != null)
            characterController.enabled = _restorePlayerControl;

        if (_restorePlayerControl)
        {
            ResetPlayerRuntimeState();
            EnsurePlayerAnimatorEnabled();
        }

        if (boatCamera != null)
            boatCamera.SetActive(false);

        if (_notifyTutorial)
            TutorialEvents.NotifyBoatExited();
    }

    public bool IsPlayerOnBoat()
    {
        return isPlayerOnBoat;
    }

    public bool CanEnterBoat()
    {
        ResolvePlayerReferences();

        return !isPlayerOnBoat &&
               GameManager.instance != null &&
               GameManager.instance.currentState == GameManager.GameState.OnFoot &&
               player != null &&
               seatPoint != null;
    }

    private void ResolvePlayerReferences()
    {
        if (player == null)
            return;

        if (playerController == null)
            playerController = player.GetComponent<PlayerController>();

        if (characterController == null)
            characterController = player.GetComponent<CharacterController>();

        if (playerMove == null)
            playerMove = player.GetComponent<PlayerMove>();

        if (playerAnimator == null)
            playerAnimator = player.GetComponentInChildren<Animator>(true);

        if (playerAnimationController == null)
            playerAnimationController = player.GetComponentInChildren<PlayerAnimationController>(true);
    }

    private void ResetPlayerRuntimeState()
    {
        if (playerMove != null)
            playerMove.ResetMovementState();

        if (playerAnimationController != null)
        {
            playerAnimationController.ResetFishingAnimationState();
            playerAnimationController.ResetFishingVisualOffset();
            playerAnimationController.ResetBoatVisualOffset();
        }
    }

    private void ApplyPlayerSeatTransform()
    {
        if (player == null || seatPoint == null)
            return;

        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        if (playerAnimationController != null)
            playerAnimationController.ApplyBoatVisualOffset();
    }

    private void PlacePlayerAtDockExit(Transform _exitPoint, Transform _parkPoint)
    {
        if (player == null)
            return;

        bool wasCharacterControllerEnabled = characterController != null && characterController.enabled;

        if (characterController != null && wasCharacterControllerEnabled)
            characterController.enabled = false;

        if (_exitPoint != null)
        {
            player.transform.SetPositionAndRotation(
                _exitPoint.position,
                _exitPoint.rotation * Quaternion.Euler(OffsetRotacao)
            );
        }
        else
        {
            Transform fallbackPoint = _parkPoint != null ? _parkPoint : transform;

            player.transform.SetPositionAndRotation(
                fallbackPoint.position + fallbackPoint.right * 2f,
                fallbackPoint.rotation * Quaternion.Euler(OffsetRotacao)
            );
        }

        if (characterController != null)
            characterController.enabled = wasCharacterControllerEnabled;

        Physics.SyncTransforms();
    }

    private void PlacePlayerAtSceneTransitionPoint(Transform _playerPoint)
    {
        if (player == null)
            return;

        Transform target = _playerPoint != null ? _playerPoint : transform;
        bool wasCharacterControllerEnabled = characterController != null && characterController.enabled;

        if (characterController != null && wasCharacterControllerEnabled)
            characterController.enabled = false;

        player.transform.SetParent(originalParent, true);
        player.transform.SetPositionAndRotation(target.position, target.rotation * Quaternion.Euler(OffsetRotacao));

        if (playerController != null)
            playerController.enabled = true;

        if (characterController != null)
            characterController.enabled = true;

        if (boatCamera != null)
            boatCamera.SetActive(false);

        ResetPlayerRuntimeState();
        EnsurePlayerAnimatorEnabled();

        if (playerMove != null)
            playerMove.SetSafeRespawnPosition(player.transform.position);

        Physics.SyncTransforms();
    }

    private void EnsurePlayerAnimatorEnabled()
    {
        if (playerAnimator != null && !playerAnimator.enabled)
            playerAnimator.enabled = true;
    }
}
