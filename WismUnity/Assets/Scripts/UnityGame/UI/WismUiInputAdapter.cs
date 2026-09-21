using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
#endif

namespace Assets.Scripts.UI
{
    public static class WismUiInputAdapter
    {
        public static bool CapitalPressedThisFrame()
        {
            if (Input.GetKeyDown(KeyCode.C)) return true;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static bool ApplicationExitPressedThisFrame()
        {
            if (Input.GetKeyDown(KeyCode.X) ||
                (Input.GetKeyDown(KeyCode.Q) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))))
                return true;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.xKey.wasPressedThisFrame ||
                (keyboard.qKey.wasPressedThisFrame && keyboard.ctrlKey.isPressed))) return true;
#endif
            return false;
        }

        public static void ConfigureGameEventSystem()
        {
#if ENABLE_INPUT_SYSTEM
            var events = EventSystem.current;
            if (events == null) return;
            var legacy = events.GetComponent<StandaloneInputModule>();
            if (legacy == null || !legacy.enabled) return;

            // Use Unity's UI action processing for the same devices as map input.
            // Keep authored EventSystem selection and navigation settings intact.
            var module = events.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = events.gameObject.AddComponent<InputSystemUIInputModule>();
            module.moveRepeatDelay = legacy.repeatDelay;
            if (legacy.inputActionsPerSecond > 0) module.moveRepeatRate = 1f / legacy.inputActionsPerSecond;
            legacy.enabled = false;
            module.enabled = true;
            var submit = module.submit?.action;
            if (submit != null)
            {
                bool hasNumpadEnter = false;
                foreach (var binding in submit.bindings)
                    hasNumpadEnter |= string.Equals(binding.effectivePath, "<Keyboard>/numpadEnter", System.StringComparison.OrdinalIgnoreCase);
                if (!hasNumpadEnter)
                {
                    bool wasEnabled = submit.enabled;
                    submit.Disable();
                    submit.AddBinding("<Keyboard>/numpadEnter");
                    if (wasEnabled) submit.Enable();
                }
            }
#endif
        }

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

        public static bool ProductionPressedThisFrame()
        {
            if (Input.GetKeyDown(KeyCode.P)) return true;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static bool SavePressedThisFrame()
        {
            if (Input.GetKeyDown(KeyCode.S)) return true;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static bool RazePressedThisFrame()
        {
            if (Input.GetKeyDown(KeyCode.R)) return true;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static bool ManagementOrLoadPressedThisFrame(out bool shiftHeld)
        {
            shiftHeld = false;
            if (Input.GetKeyDown(KeyCode.L))
            {
                shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                return true;
            }
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame)
            {
                shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
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
            if (Input.GetKeyDown(KeyCode.E)) return true;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.eKey.wasPressedThisFrame;
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
