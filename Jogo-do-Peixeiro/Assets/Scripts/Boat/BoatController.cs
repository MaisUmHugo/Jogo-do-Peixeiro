using UnityEngine;

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
    private Floater[] floaters;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Busca todos os scripts de flutuação no barco ou nos filhos
        floaters = GetComponentsInChildren<Floater>();

        // Estado inicial: Se o jogador não está no barco, desativa a física
        if (!isPlayerOnBoat)
            SetBoatPhysics(false);
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
            }
        }

        // Avisa cada floater se ele deve calcular flutuação ou não
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
    }

    public void ParkBoatAndExit(Transform _parkPoint, Transform _exitPoint)
    {
        if (!isPlayerOnBoat) return;

        Debug.Log("Barco estacionado e player saiu");
        isPlayerOnBoat = false;

        // 1. Desliga a física (Barco congela)
        SetBoatPhysics(false);

        // 2. Reposiciona o barco no ponto de estacionamento se houver um
        if (_parkPoint != null)
        {
            transform.position = _parkPoint.position;
            transform.rotation = _parkPoint.rotation * Quaternion.Euler(OffsetRotacao);
        }

        // 3. Lógica do Player saindo
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