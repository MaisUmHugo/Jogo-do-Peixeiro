using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public static InputHandler instance;

    private InputActions inputActions;

    public Vector2 moveInput { get; private set; }
    public Vector2 lookInput { get; private set; }
    public float zoomInput { get; private set; }

    public Action onPausePressed;
    public Action onInteractPressed;
    public Action onAimPressed;
    public Action onAimReleased;
    public Action onInventoryPressed;
    public Action onAnyButtonPressed;

    [Header("Movement Input")]
    [SerializeField, Range(0f, 0.5f)] private float moveDeadzone = 0.15f;

    [Header("Aim Input")]
    [SerializeField, Range(0f, 1f)] private float aimPressThreshold = 0.55f;
    [SerializeField, Range(0f, 1f)] private float aimReleaseThreshold = 0.35f;
    [SerializeField] private bool suppressZoomWhileAiming = true;
    [SerializeField] private bool ignoreRightTriggerForZoom = true;

    public bool IsAimHeld { get; private set; }

    private void OnValidate()
    {
        moveDeadzone = Mathf.Clamp(moveDeadzone, 0f, 0.5f);
        aimPressThreshold = Mathf.Clamp01(aimPressThreshold);
        aimReleaseThreshold = Mathf.Clamp(aimReleaseThreshold, 0f, aimPressThreshold);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        inputActions = new InputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        moveInput = GetProcessedMoveInput(inputActions.Player.Move.ReadValue<Vector2>());
        lookInput = inputActions.Player.Look.ReadValue<Vector2>();
        UpdateAimInput();

        zoomInput = inputActions.Player.Zoom.ReadValue<float>();
        zoomInput = GetProcessedZoomInput(zoomInput);

        if (suppressZoomWhileAiming && IsAimHeld)
            zoomInput = 0f;

        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            onInteractPressed?.Invoke();
        }

        if (inputActions.Player.Pause.WasPressedThisFrame())
        {
            onPausePressed?.Invoke();
        }

        if (inputActions.Player.Inventory.WasPressedThisFrame())
        {
            onInventoryPressed?.Invoke();
        }

        if (inputActions.Player.AnyButton.WasPressedThisFrame())
        {
           onAnyButtonPressed?.Invoke();
        }
    }

    private void UpdateAimInput()
    {
        if (IsGameplayInputBlocked())
        {
            ReleaseAimIfHeld();
            return;
        }

        float aimValue = inputActions.Player.Aim.ReadValue<float>();

        if (!IsAimHeld && aimValue >= aimPressThreshold)
        {
            IsAimHeld = true;
            onAimPressed?.Invoke();
            return;
        }

        if (IsAimHeld && aimValue <= aimReleaseThreshold)
        {
            IsAimHeld = false;
            onAimReleased?.Invoke();
        }
    }

    private void ReleaseAimIfHeld()
    {
        if (!IsAimHeld)
            return;

        IsAimHeld = false;
        onAimReleased?.Invoke();
    }

    private bool IsGameplayInputBlocked()
    {
        if (PlayerCamera.IsCameraLocked)
            return true;

        return GameManager.instance != null && GameManager.instance.IsGameplayBlocked();
    }

    private float GetProcessedZoomInput(float _zoomInput)
    {
        if (!ignoreRightTriggerForZoom || Gamepad.current == null)
            return _zoomInput;

        return _zoomInput - Gamepad.current.rightTrigger.ReadValue();
    }

    private Vector2 GetProcessedMoveInput(Vector2 _moveInput)
    {
        if (_moveInput.magnitude < moveDeadzone)
            return Vector2.zero;

        return _moveInput;
    }

    public void ResetGameplayInput()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        zoomInput = 0f;
        IsAimHeld = false;
    }
}
