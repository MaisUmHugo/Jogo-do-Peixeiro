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
    public Action onSkillCheckPressed;
    public Action onInventoryPressed;
    public Action onAnyButtonPressed;

    [Header("Movement Input")]
    [SerializeField, Range(0f, 0.5f)] private float moveDeadzone = 0.15f;

    private void OnValidate()
    {
        moveDeadzone = Mathf.Clamp(moveDeadzone, 0f, 0.5f);
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

        zoomInput = inputActions.Player.Zoom.ReadValue<float>();

        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            onInteractPressed?.Invoke();
        }

        if (inputActions.Player.SkillCheck.WasPressedThisFrame())
        {
            onSkillCheckPressed?.Invoke();
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
    }
}
