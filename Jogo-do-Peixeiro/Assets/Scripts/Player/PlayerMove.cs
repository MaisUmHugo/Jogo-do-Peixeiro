using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform cameraTransform;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [Header("Step VFX")]
    [SerializeField] private VisualEffect stepVFXPrefab;
    [SerializeField] private Transform stepPoint;
    [SerializeField] private float stepInterval = 0.25f;
    [SerializeField] private float moveThreshold = 0.1f;
    [SerializeField] private float stepBackOffset = 0.12f;
    [SerializeField] private float stepHeight = 0.05f;
    [SerializeField] private float stepVFXLifetime = 2f;
    [SerializeField] private int maxStepVFXInstances = 10;

    private CharacterController characterController;
    private Vector3 posicaoInicial;
    private float verticalVelocity;
    private float stepTimer;

    private readonly List<VisualEffect> activeStepVFX = new();

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        posicaoInicial = transform.position;
    }

    public void HandleMove(Vector2 _moveInput)
    {
        if (cameraTransform == null)
            return;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * _moveInput.y + cameraRight * _moveInput.x;

        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 finalMove = moveDirection * moveSpeed;
        finalMove.y = verticalVelocity;

        characterController.Move(finalMove * Time.deltaTime);

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        HandleStepVFX(moveDirection);
    }

    private void HandleStepVFX(Vector3 _moveDirection)
    {
        if (stepVFXPrefab == null || stepPoint == null)
            return;

        bool isMoving = _moveDirection.magnitude > moveThreshold;
        bool isGrounded = characterController.isGrounded;

        if (!isMoving || !isGrounded)
        {
            stepTimer = 0f;
            return;
        }

        Vector3 backwardOffset = -_moveDirection.normalized * stepBackOffset;

        stepPoint.localPosition = new Vector3(
            backwardOffset.x,
            stepHeight,
            backwardOffset.z
        );

        stepTimer -= Time.deltaTime;

        if (stepTimer > 0f)
            return;

        SpawnStepVFX(stepPoint.position, stepPoint.rotation);

        stepTimer = stepInterval;
    }

    private void SpawnStepVFX(Vector3 position, Quaternion rotation)
    {
        if (activeStepVFX.Count >= maxStepVFXInstances)
        {
            if (activeStepVFX[0] != null)
                Destroy(activeStepVFX[0].gameObject);

            activeStepVFX.RemoveAt(0);
        }

        VisualEffect instance = Instantiate(
            stepVFXPrefab,
            position,
            rotation,
            transform
        );

        instance.Play();

        activeStepVFX.Add(instance);

        Destroy(instance.gameObject, stepVFXLifetime);
    }

    private void Update()
    {
        if (transform.position.y <= -5f)
            transform.position = posicaoInicial;
    }
}