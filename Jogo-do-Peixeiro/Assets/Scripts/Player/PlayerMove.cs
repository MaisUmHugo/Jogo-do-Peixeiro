using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VFX;

public class PlayerMove : MonoBehaviour
{
    private const string FootstepAudioSourceName = "FootstepAudioSource";

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform cameraTransform;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [Header("Step Feedback")]
    [SerializeField] private VisualEffect stepVFXPrefab;
    [SerializeField] private Transform stepPoint;
    [SerializeField] private float stepInterval = 0.25f;
    [SerializeField] private float moveThreshold = 0.1f;
    [SerializeField] private float stepBackOffset = 0.12f;
    [SerializeField] private float stepHeight = 0.05f;
    [SerializeField] private float stepVFXLifetime = 2f;
    [SerializeField] private int maxStepVFXInstances = 10;
    [SerializeField] private AudioClip footstepSfx;
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField, Range(0f, 1f)] private float footstepSfxVolume = 0.7f;
    [SerializeField] private float footstepPitchMin = 0.9f;
    [SerializeField] private float footstepPitchMax = 1.1f;
    [SerializeField, Range(0f, 1f)] private float footstepSpatialBlend = 1f;

    private CharacterController characterController;
    private Vector3 posicaoInicial;
    private float verticalVelocity;
    private float stepTimer;
    private Transform PlayerPosition;

    private readonly List<VisualEffect> activeStepVFX = new();

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        SetupFootstepAudioSource();
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
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection) * Quaternion.Euler(0f, 0f, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        HandleStepVFX(moveDirection);
    }

    public void ResetMovementState()
    {
        verticalVelocity = 0f;
        stepTimer = 0f;
        posicaoInicial = transform.position;
        ResetStepFeedback();
    }

    private void HandleStepVFX(Vector3 _moveDirection)
    {
        if (stepVFXPrefab == null && footstepSfx == null)
            return;

        bool isMoving = _moveDirection.magnitude > moveThreshold;
        bool isGrounded = characterController.isGrounded;

        if (!isMoving || !isGrounded)
        {
            ResetStepFeedback();
            return;
        }

        Vector3 spawnPosition = GetStepFeedbackPosition(_moveDirection);
        Quaternion spawnRotation = Quaternion.LookRotation(_moveDirection.normalized, Vector3.up);

        stepTimer -= Time.deltaTime;

        if (stepTimer > 0f)
            return;

        PlayStepFeedback(spawnPosition, spawnRotation);

        stepTimer = stepInterval;
    }

    private void PlayStepFeedback(Vector3 position, Quaternion rotation)
    {
        SpawnStepVFX(position, rotation);
        PlayFootstepSfx(position);
    }

    private void SpawnStepVFX(Vector3 position, Quaternion rotation)
    {
        if (stepVFXPrefab == null)
            return;

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

    private void PlayFootstepSfx(Vector3 _position)
    {
        if (footstepSfx == null || footstepAudioSource == null)
            return;

        footstepAudioSource.transform.position = _position;
        footstepAudioSource.pitch = Random.Range(footstepPitchMin, footstepPitchMax);
        footstepAudioSource.PlayOneShot(footstepSfx, footstepSfxVolume);
    }

    private void Update()
    {
        if (ShouldStopStepFeedback())
            ResetStepFeedback();

        if (transform.position.y <= -5f)
            transform.position = posicaoInicial;
    }

    private void SetupFootstepAudioSource()
    {
        if (footstepAudioSource == null)
        {
            Transform audioSourceTransform = transform.Find(FootstepAudioSourceName);

            if (audioSourceTransform != null)
                footstepAudioSource = audioSourceTransform.GetComponent<AudioSource>();
        }

        if (footstepAudioSource == null)
        {
            GameObject audioSourceObject = new GameObject(FootstepAudioSourceName);
            audioSourceObject.transform.SetParent(transform, false);
            footstepAudioSource = audioSourceObject.AddComponent<AudioSource>();
        }

        footstepAudioSource.playOnAwake = false;
        footstepAudioSource.loop = false;
        footstepAudioSource.spatialBlend = footstepSpatialBlend;
    }

    private Vector3 GetStepFeedbackPosition(Vector3 _moveDirection)
    {
        Vector3 basePosition = stepPoint != null && stepPoint != transform
            ? stepPoint.position
            : transform.position;

        Vector3 backwardOffset = -_moveDirection.normalized * stepBackOffset;
        return basePosition + backwardOffset + (Vector3.up * stepHeight);
    }

    private bool ShouldStopStepFeedback()
    {
        if (Time.timeScale <= 0f)
            return true;

        if (GameManager.instance == null)
            return false;

        return GameManager.instance.currentState != GameManager.GameState.OnFoot;
    }

    private void ResetStepFeedback()
    {
        stepTimer = 0f;

        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            footstepAudioSource.Stop();
    }
}
