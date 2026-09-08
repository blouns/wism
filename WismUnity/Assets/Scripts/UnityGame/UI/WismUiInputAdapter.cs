using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Assets.Scripts.UI
{
    public static class WismUiInputAdapter
    {
        public static Vector2 PointerPosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Pointer.current != null)
                {
                    return Pointer.current.position.ReadValue();
                }
#endif
                return Input.mousePosition;
            }
        }

        public static bool PrimaryPressedThisFrame()
        {
            return TryGetPrimaryPress(out _);
        }

        public static bool TryGetPrimaryPress(out Vector2 position)
        {
            return TryGetPrimaryPress(out position, out _);
        }

        public static bool TryGetPrimaryPress(out Vector2 position, out int deviceId)
        {
            deviceId = -1;
            // Never pair a legacy press with a different device's coordinates.
            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                position = Pointer.current.position.ReadValue();
                deviceId = Pointer.current.deviceId;
                return true;
            }
#endif
            position = default;
            return false;
        }

        public static bool NextArmyPressedThisFrame(out int deviceId)
        {
            deviceId = -1;
            if (Input.GetKeyDown(KeyCode.N)) return true;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
            {
                deviceId = Keyboard.current.deviceId;
                return true;
            }
#endif
            return false;
        }

        public static bool DefendPressedThisFrame()
        {
            if (Input.GetKeyDown(KeyCode.D)) return true;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static bool EndTurnPressedThisFrame()
        {
            if (Input.GetKeyDown(KeyCode.E) &&
                (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) return true;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.eKey.wasPressedThisFrame &&
                (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
#else
            return false;
#endif
        }

        public static bool ItemActionPressed(bool taking)
        {
            if (Input.GetKeyDown(taking ? KeyCode.T : KeyCode.O)) return true;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && (taking ? Keyboard.current.tKey : Keyboard.current.oKey).wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static bool ItemPickerConfirmPressed()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) return true;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
            return false;
#endif
        }

        public static bool ItemPickerCancelPressed()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) return true;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static int ItemPickerDirection()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) return -1;
            if (Input.GetKeyDown(KeyCode.DownArrow)) return 1;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame) return -1;
                if (Keyboard.current.downArrowKey.wasPressedThisFrame) return 1;
            }
#endif
            return 0;
        }
    }
}
