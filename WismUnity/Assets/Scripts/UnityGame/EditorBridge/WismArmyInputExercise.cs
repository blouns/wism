using System;
using System.Linq;
using Assets.Scripts.Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Wism.Client.Core;

namespace WismUnity.EditorBridge
{
    public static class WismArmyInputExercise
    {
        public sealed class Request
        {
            public string modality;
            public float x;
            public float y;
        }

        private static InputDevice device;
        private static InputManager input;
        private static Request request;
        private static int startFrame;
        private static int pressedFrame;
        private static double deadline;
        private static bool released;
        private static InputSettings originalSettings;
        private static InputSettings exerciseSettings;
        private static object status = new { status = "Idle", executed = false };

        static WismArmyInputExercise() => AssemblyReloadEvents.beforeAssemblyReload += Finish;

        public static object Begin(Request value)
        {
            if (device != null) return new { status = "Busy", executed = false };
            if (!EditorApplication.isPlaying || !Game.IsInitialized())
                return new { status = "PlayModeRequired", executed = false };
            if (value == null || !(value.modality == "mouse" || value.modality == "touch" || value.modality == "keyboard"))
                return new { status = "ExplicitModalityRequired", executed = false };
            if (value.modality != "keyboard" &&
                (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                 value.x < 0 || value.y < 0 || value.x >= Screen.width || value.y >= Screen.height))
                return new { status = "OutsideViewport", executed = false };

            input = UnityEngine.Object.FindFirstObjectByType<InputManager>();
            if (input == null || input.InputMode != InputMode.Game || input.UnityManager.ExecutionMode != ExecutionMode.Running)
                return new { status = "GameInputRequired", executed = false };

            request = value;
            startFrame = Time.frameCount;
            pressedFrame = -1;
            released = false;
            deadline = EditorApplication.timeSinceStartup + 5;
            originalSettings = InputSystem.settings;
            exerciseSettings = UnityEngine.Object.Instantiate(originalSettings);
            exerciseSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            exerciseSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = exerciseSettings;
            device = value.modality == "mouse" ? (InputDevice)InputSystem.AddDevice<Mouse>("WismExerciseMouse") :
                value.modality == "touch" ? InputSystem.AddDevice<Touchscreen>("WismExerciseTouch") :
                InputSystem.AddDevice<Keyboard>("WismExerciseKeyboard");
            status = new { status = "Queued", executed = false, modality = value.modality, startFrame };
            Queue(true);
            EditorApplication.update += Tick;
            return status;
        }

        public static object Status() => status;

        private static void Queue(bool pressed)
        {
            var point = new Vector2(request.x, request.y);
            if (device is Mouse mouse)
                InputSystem.QueueStateEvent(mouse, pressed ? new MouseState { position = point }.WithButton(MouseButton.Left) : new MouseState { position = point });
            else if (device is Touchscreen touch)
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, position = point,
                    phase = pressed ? UnityEngine.InputSystem.TouchPhase.Began : UnityEngine.InputSystem.TouchPhase.Ended });
            else if (device is Keyboard keyboard)
                InputSystem.QueueStateEvent(keyboard, pressed ? new KeyboardState(Key.N) : new KeyboardState());
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || input == null || EditorApplication.timeSinceStartup > deadline)
            {
                status = new { status = "Aborted", executed = false };
                Finish();
                return;
            }
            if (Time.frameCount <= startFrame) return;
            if (!released)
            {
                Queue(false);
                released = true;
                pressedFrame = Time.frameCount;
                return;
            }
            if (Time.frameCount <= pressedFrame) return;
            bool observed = input.LastPrimaryActionFrame > startFrame && input.LastPrimaryDeviceId == device.deviceId;
            status = new
            {
                status = observed ? "Observed" : "NoDispatch",
                executed = observed,
                modality = request.modality,
                action = observed ? input.LastPrimaryAction : null,
                accepted = observed && input.LastPrimaryAction != "rejected",
                inputFrame = input.LastPrimaryActionFrame,
                selectedArmyIds = Game.Current.GetSelectedArmies()?.Select(army => army.Id).ToArray() ?? Array.Empty<int>(),
                note = "Observed input dispatch only; gameplay completion and control/state coverage require assertions."
            };
            Finish();
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            if (device != null && device.added) InputSystem.RemoveDevice(device);
            device = null;
            if (originalSettings != null) InputSystem.settings = originalSettings;
            if (exerciseSettings != null) UnityEngine.Object.DestroyImmediate(exerciseSettings);
            originalSettings = null;
            exerciseSettings = null;
        }
    }
}
