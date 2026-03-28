using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform cameraTransform;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void HandleMove(Vector2 _moveInput)
    {
        if (cameraTransform == null)
            return;

        // DIREÇÃO BASEADA NA CAMERA
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * _moveInput.y + cameraRight * _moveInput.x;

        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        // GRAVIDADE
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0)
                verticalVelocity = -2f; // gruda no chão
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // aplica movimento + gravidade
        Vector3 finalMove = moveDirection * moveSpeed;
        finalMove.y = verticalVelocity;

        characterController.Move(finalMove * Time.deltaTime);

        // ROTAÇÃO
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}