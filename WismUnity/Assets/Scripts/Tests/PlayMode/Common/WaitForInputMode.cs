using Assets.Scripts.Managers;
using UnityEngine;

namespace Assets.Tests.PlayMode
{
    public class WaitForInputMode : CustomYieldInstruction
    {
        public WaitForInputMode(InputManager inputManager, InputMode mode)
        {
            InputManager = inputManager;
            Mode = mode;
        }

        public override bool keepWaiting
        {
            get
            {
                return this.Mode != Mode;
            }
        }

        public InputManager InputManager { get; }
        public InputMode Mode { get; }
    }
}
