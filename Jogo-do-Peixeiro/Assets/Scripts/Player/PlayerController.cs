using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerMove playerMove;

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
    }

    private void Update()
    {
        if (GameManager.instance == null)
            return;

        if (InputHandler.instance == null)
            return;

        if (GameManager.instance.currentState != GameManager.GameState.OnFoot)
            return;

        playerMove.HandleMove(InputHandler.instance.moveInput);
    }
}