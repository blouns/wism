using Assets.Scripts.Managers;
using UnityEngine;

namespace Assets.Tests.PlayMode
{
    public class WaitForInputMode : CustomYieldInstruction
    {
        public WaitForInputMode(InputManager inputManager, InputMode mode)
        {
            this.InputManager = inputManager;
            this.Mode = mode;
        }

        public override bool keepWaiting
        {
            get
            {
                return this.Mode != this.Mode;
            }
        }

        public InputManager InputManager { get; }
        public InputMode Mode { get; }
    }
}
