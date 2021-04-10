using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Common;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.Managers
{
    public class DebugManager : MonoBehaviour
    {
        private ILogger logger;
        private Text debugText;
        private string lastMessage;
        private bool debug;

        public void LateUpdate()
        {
            this.debugText.gameObject.SetActive(IsDebugOn());
        }

        public void Initialize(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.debugText = UnityUtilities.GameObjectHardFind("DebugText")
                .GetComponent<Text>();
        }

        public void LogInformation(string message, params object[] args)
        {
            string logMessage = string.Format(message, args);
            Debug.Log(logMessage);
            logger.LogInformation(logMessage);

            if (IsDebugOn() &&
               (logMessage != lastMessage)) // Skip repeats
            {
                this.debugText.text = $"{logMessage}\n{this.debugText.text}";
                lastMessage = logMessage;
            }
        }

        public void ToggleDebug()
        {
            this.debug = !this.debug;

            if (!this.debug)
            {
                this.debugText.text = "";
            }
        }

        public bool IsDebugOn()
        {
            return (this.debugText != null && this.debug);
        }
    }
}
