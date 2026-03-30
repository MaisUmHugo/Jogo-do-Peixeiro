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
    public Action onAnyButtonPressed;

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
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        lookInput = inputActions.Player.Look.ReadValue<Vector2>();
        zoomInput = inputActions.Player.Zoom.ReadValue<float>();

        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            onInteractPressed?.Invoke();
        }

        if (inputActions.Player.Pause.WasPressedThisFrame())
        {
            onPausePressed?.Invoke();
        }

        if (inputActions.Player.Aim.WasPressedThisFrame())
        {
            onAimPressed?.Invoke();
        }

        if (inputActions.Player.Aim.WasReleasedThisFrame())
        {
            onAimReleased?.Invoke();
        }

        if (inputActions.Player.AnyButton.WasPressedThisFrame())
        {
           onAnyButtonPressed?.Invoke();
        }
    }
}