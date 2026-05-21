using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputDeviceDetector : MonoBehaviour
{
    public static InputDeviceDetector Instance { get; private set; }
    public static event Action<InputDeviceType> DeviceTypeChanged;

    public static InputDeviceType CurrentDeviceType { get; private set; } = InputDeviceType.Keyboard;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (WasKeyboardOrMouseUsed())
        {
            SetDeviceType(InputDeviceType.Keyboard);
            return;
        }

        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            SetDeviceType(InputDeviceType.GenericController);
        }
    }

    private static void SetDeviceType(InputDeviceType _deviceType)
    {
        if (CurrentDeviceType == _deviceType)
            return;

        CurrentDeviceType = _deviceType;
        DeviceTypeChanged?.Invoke(CurrentDeviceType);
    }

    private bool WasKeyboardOrMouseUsed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current == null)
            return false;

        if (Mouse.current.leftButton.wasPressedThisFrame ||
            Mouse.current.rightButton.wasPressedThisFrame ||
            Mouse.current.middleButton.wasPressedThisFrame)
            return true;

        if (Mouse.current.scroll.ReadValue().sqrMagnitude > 0.01f)
            return true;

        return Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;
    }
}
