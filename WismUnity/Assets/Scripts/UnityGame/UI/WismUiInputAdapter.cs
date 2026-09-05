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
    }
}
