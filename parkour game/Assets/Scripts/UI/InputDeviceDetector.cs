using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputDeviceDetector : MonoBehaviour
{
    public enum InputMode { keyboardMouse, Gamepad, None }
    public static InputMode CurrentInputMode { get; private set; } = InputMode.keyboardMouse;

    private void Update()
    {
        // GamePad check
        if (Gamepad.current != null)
        {
            if (AnyGamepadButtonPressedThisFrame() ||
                Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f)
            {
                CurrentInputMode = InputMode.Gamepad;
                /*Debug.Log("gamePad");*/
            }
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.delta.ReadValue() != Vector2.zero ||
                   Mouse.current.leftButton.wasPressedThisFrame ||
                   Mouse.current.rightButton.wasPressedThisFrame)
            {
                CurrentInputMode = InputMode.keyboardMouse;
               /* Debug.Log("mouse");*/
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                CurrentInputMode = InputMode.keyboardMouse;
               /* Debug.Log("Keyboard");*/
            }
        }
    }

    private bool AnyGamepadButtonPressedThisFrame()
    {
        foreach (var control in Gamepad.current.allControls)
        {
            if (control is ButtonControl button && button.wasPressedThisFrame)
                return true;
        }
        return false;
    }
}