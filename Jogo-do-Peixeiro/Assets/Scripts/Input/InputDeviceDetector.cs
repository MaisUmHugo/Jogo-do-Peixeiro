using UnityEngine;
using UnityEngine.InputSystem;

public class InputDeviceDetector : MonoBehaviour
{
    public static InputDeviceType CurrentDeviceType { get; private set; } = InputDeviceType.Keyboard;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            CurrentDeviceType = InputDeviceType.Keyboard;
            return;
        }

        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            CurrentDeviceType = InputDeviceType.GenericController;
        }
    }
}