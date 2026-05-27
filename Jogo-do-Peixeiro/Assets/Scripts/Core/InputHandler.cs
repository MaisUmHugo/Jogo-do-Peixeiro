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
    public bool WasBackInputStartedOverUi { get; private set; }

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
        DontDestroyOnLoad(gameObject);
        inputActions = new InputActions();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
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
        bool fadeBlocking = SceneTransitionFadeController.IsGameplayBlocking;
        bool gameplayInputBlocked = GameManager.instance != null && GameManager.instance.IsGameplayBlocked();

        moveInput = gameplayInputBlocked
            ? Vector2.zero
            : GetProcessedMoveInput(inputActions.Player.Move.ReadValue<Vector2>());
        lookInput = gameplayInputBlocked
            ? Vector2.zero
            : inputActions.Player.Look.ReadValue<Vector2>();
        zoomInput = gameplayInputBlocked
            ? 0f
            : inputActions.Player.Zoom.ReadValue<float>();

        if (!fadeBlocking && inputActions.Player.Interact.WasPressedThisFrame())
        {
            onInteractPressed?.Invoke();
        }

        if (!fadeBlocking && inputActions.Player.SkillCheck.WasPressedThisFrame())
        {
            onSkillCheckPressed?.Invoke();
        }

        if (!fadeBlocking && inputActions.Player.Pause.WasPressedThisFrame())
        {
            DispatchPausePressed();
        }
        else if (!fadeBlocking && inputActions.UI.Cancel.WasPressedThisFrame() && CanCancelOpenUi())
        {
            DispatchPausePressed();
        }

        if (!fadeBlocking && inputActions.Player.Inventory.WasPressedThisFrame())
        {
            onInventoryPressed?.Invoke();
        }

        if (!fadeBlocking && inputActions.Player.AnyButton.WasPressedThisFrame())
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

    private void DispatchPausePressed()
    {
        WasBackInputStartedOverUi =
            UIModalManager.HasOpenModal ||
            (GameManager.instance != null && GameManager.instance.currentState == GameManager.GameState.Paused) ||
            (GameManager.instance != null && GameManager.instance.currentState == GameManager.GameState.InUI);

        onPausePressed?.Invoke();
        WasBackInputStartedOverUi = false;
    }

    private bool CanCancelOpenUi()
    {
        if (UIModalManager.HasOpenModal)
            return true;

        if (GameManager.instance == null)
            return false;

        return GameManager.instance.currentState == GameManager.GameState.InUI ||
               GameManager.instance.currentState == GameManager.GameState.Paused;
    }

    public void ResetGameplayInput()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        zoomInput = 0f;
    }
}
