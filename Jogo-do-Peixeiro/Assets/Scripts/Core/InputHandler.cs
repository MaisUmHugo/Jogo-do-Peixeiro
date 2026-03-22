using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public static InputHandler instance;

    private InputActions inputActions;

    public Vector2 moveInput { get; private set; }
    public Vector2 lookInput { get; private set; }

    public bool pausePressed { get; private set; }

    public Action onInteractPressed;

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
        pausePressed = inputActions.Player.Pause.WasPressedThisFrame();

        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            Debug.Log("E pressionado");
            onInteractPressed?.Invoke();
        }
    }
}