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
    [SerializeField] private PlayerAnimationController playerAnimationController;
    [SerializeField] private bool autoFindPlayerAnimationController = true;

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
    [SerializeField] private bool useStepVFXPool = true;
    [SerializeField] private string stepVFXPoolKey = "StepVFX";
    [SerializeField, Min(1)] private int stepVFXPoolSize = 10;
    [SerializeField] private bool syncStepFeedbackToMovementHop = true;
    [SerializeField] private bool scaleStepIntervalWithMoveSpeed = true;
    [SerializeField, Min(0.01f)] private float stepIntervalReferenceMoveSpeed = 5f;
    [SerializeField, Range(0.1f, 1f)] private float minAnalogStepRate = 0.35f;
    [SerializeField, Range(0.1f, 3f)] private float minStepRate = 0.45f;
    [SerializeField, Range(0.1f, 4f)] private float maxStepRate = 2.25f;
    [SerializeField, InspectorName("Footstep SFX")] private AudioClip footstepSfx;
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField, Range(0f, 1f), InspectorName("Footstep SFX Volume")] private float footstepSfxVolume = 0.7f;
    [SerializeField] private float footstepPitchMin = 0.9f;
    [SerializeField] private float footstepPitchMax = 1.1f;
    [SerializeField, Range(0f, 1f)] private float footstepSpatialBlend = 1f;

    private CharacterController characterController;
    private Vector3 posicaoInicial;
    private float verticalVelocity;
    private float stepTimer;
    private Transform PlayerPosition;
    private int lastConsumedMovementStepPulseId;

    private readonly List<GameObject> activeStepVFX = new();

    public float MoveSpeed => moveSpeed;
    public float CurrentPlanarSpeed { get; private set; }
    public float CurrentMoveAmount { get; private set; }
    public Vector3 CurrentMoveDirection { get; private set; }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        stepInterval = Mathf.Max(0.01f, stepInterval);
        moveThreshold = Mathf.Max(0f, moveThreshold);
        stepIntervalReferenceMoveSpeed = Mathf.Max(0.01f, stepIntervalReferenceMoveSpeed);
        minStepRate = Mathf.Max(0.1f, minStepRate);
        maxStepRate = Mathf.Max(minStepRate, maxStepRate);
        minAnalogStepRate = Mathf.Clamp(minAnalogStepRate, 0.1f, 1f);
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        ResolvePlayerAnimationController();
        SetupFootstepAudioSource();
        PrepareStepVFXPool();
    }

    private void Start()
    {
        posicaoInicial = transform.position;
    }

    public void HandleMove(Vector2 _moveInput)
    {
        if (cameraTransform == null || characterController == null || !characterController.enabled)
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

        CurrentMoveDirection = moveDirection;
        CurrentMoveAmount = Mathf.Clamp01(moveDirection.magnitude);
        CurrentPlanarSpeed = CurrentMoveAmount * moveSpeed;

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
        CurrentPlanarSpeed = 0f;
        CurrentMoveAmount = 0f;
        CurrentMoveDirection = Vector3.zero;
        lastConsumedMovementStepPulseId = 0;

        if (CanUseCurrentPositionAsSafeRespawn())
            SetSafeRespawnPosition(transform.position);

        ResetStepFeedback();
    }

    public void SetSafeRespawnPosition(Vector3 _position)
    {
        posicaoInicial = _position;
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

        if (TryPlaySyncedStepFeedback(spawnPosition, spawnRotation))
            return;

        stepTimer -= Time.deltaTime;

        if (stepTimer > 0f)
            return;

        PlayStepFeedback(spawnPosition, spawnRotation);

        stepTimer = GetEffectiveStepInterval(_moveDirection);
    }

    private bool TryPlaySyncedStepFeedback(Vector3 _position, Quaternion _rotation)
    {
        if (!syncStepFeedbackToMovementHop)
            return false;

        ResolvePlayerAnimationController();

        if (playerAnimationController == null ||
            !playerAnimationController.CanSyncStepFeedbackToMovementHop)
        {
            return false;
        }

        int pulseId = playerAnimationController.MovementStepPulseId;

        if (pulseId == lastConsumedMovementStepPulseId)
            return true;

        lastConsumedMovementStepPulseId = pulseId;
        PlayStepFeedback(_position, _rotation);
        return true;
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

        activeStepVFX.RemoveAll(_vfxObject => _vfxObject == null || !_vfxObject.activeInHierarchy);

        if (activeStepVFX.Count >= maxStepVFXInstances)
        {
            if (activeStepVFX[0] != null)
                PooledVisualEffectUtility.Release(activeStepVFX[0]);

            activeStepVFX.RemoveAt(0);
        }

        VisualEffect instance = PooledVisualEffectUtility.Spawn(
            stepVFXPrefab,
            stepVFXPoolKey,
            position,
            rotation,
            transform,
            useStepVFXPool,
            stepVFXPoolSize,
            stepVFXLifetime,
            true,
            out GameObject rootObject
        );

        if (instance == null)
            return;

        instance.Play();
        activeStepVFX.Add(rootObject != null ? rootObject : instance.gameObject);
    }

    private void PlayFootstepSfx(Vector3 _position)
    {
        if (footstepSfx == null || footstepAudioSource == null)
            return;

        AudioManager.Instance?.ApplySfxOutput(footstepAudioSource);
        footstepAudioSource.transform.position = _position;
        footstepAudioSource.pitch = Random.Range(footstepPitchMin, footstepPitchMax);
        footstepAudioSource.PlayOneShot(footstepSfx, footstepSfxVolume);
    }

    private void Update()
    {
        if (ShouldStopStepFeedback())
            ResetStepFeedback();

        if (transform.position.y <= -5f && ShouldRunFallFailsafe())
            RespawnAtSafePosition();
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
        AudioManager.Instance?.ApplySfxOutput(footstepAudioSource);
    }

    private void PrepareStepVFXPool()
    {
        int poolSize = Mathf.Max(1, stepVFXPoolSize, maxStepVFXInstances);

        PooledVisualEffectUtility.EnsurePool(
            stepVFXPoolKey,
            stepVFXPrefab,
            poolSize,
            useStepVFXPool,
            true
        );
    }

    private void ResolvePlayerAnimationController()
    {
        if (playerAnimationController != null || !autoFindPlayerAnimationController)
            return;

        playerAnimationController = GetComponentInChildren<PlayerAnimationController>(true);

        if (playerAnimationController == null)
            playerAnimationController = GetComponentInParent<PlayerAnimationController>();
    }

    private float GetEffectiveStepInterval(Vector3 _moveDirection)
    {
        if (!scaleStepIntervalWithMoveSpeed)
            return stepInterval;

        float stepRate = moveSpeed / Mathf.Max(0.01f, stepIntervalReferenceMoveSpeed);
        float analogRate = Mathf.Lerp(minAnalogStepRate, 1f, Mathf.Clamp01(_moveDirection.magnitude));
        stepRate *= analogRate;
        stepRate = Mathf.Clamp(stepRate, minStepRate, maxStepRate);

        return stepInterval / stepRate;
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

    private bool CanUseCurrentPositionAsSafeRespawn()
    {
        if (transform.position.y <= -5f)
            return false;

        if (GameManager.instance == null)
            return true;

        return GameManager.instance.currentState == GameManager.GameState.OnFoot;
    }

    private bool ShouldRunFallFailsafe()
    {
        if (GameManager.instance == null)
            return true;

        return GameManager.instance.currentState == GameManager.GameState.OnFoot;
    }

    private void RespawnAtSafePosition()
    {
        bool shouldReenableController = characterController != null && characterController.enabled;

        if (characterController != null)
            characterController.enabled = false;

        transform.position = posicaoInicial;
        verticalVelocity = 0f;

        ResetAttachedRigidbodies();

        if (characterController != null)
            characterController.enabled = shouldReenableController;

        ResetStepFeedback();
        Physics.SyncTransforms();
    }

    private void ResetAttachedRigidbodies()
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody body = rigidbodies[i];

            if (body == null)
                continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void ResetStepFeedback()
    {
        stepTimer = 0f;

        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            footstepAudioSource.Stop();
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            RespawnAtSafePosition();
        }
    }
}
