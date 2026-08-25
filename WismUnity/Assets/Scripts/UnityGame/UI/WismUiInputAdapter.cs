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
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                return true;
            }
#endif
            return Input.GetMouseButtonDown(0);
        }
    }
}
