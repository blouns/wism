using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Common;
using IWismLogger = Wism.Client.Common.IWismLogger;

namespace Assets.Scripts.Managers
{
    public class DebugManager : MonoBehaviour
    {
        private const int maxLines = 50;
        private IWismLogger logger;
        private Text debugText;
        private string lastMessage;
        private bool debug;
        private readonly List<string> logLines = new List<string>();
        private readonly StringBuilder logBuilder = new StringBuilder();

        public void LateUpdate()
        {
            if (this.debugText != null)
            {
                this.debugText.gameObject.SetActive(IsDebugOn());
            }

            if (IsDebugOn())
            {
                ShowLog();
            }
        }

        private void ShowLog()
        {
            this.logBuilder.Clear();
            foreach (var line in this.logLines)
            {
                this.logBuilder.AppendLine(line);
            }
            this.debugText.text = this.logBuilder.ToString();
        }

        public void Initialize(IWismLoggerFactory loggerFactory)
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
            this.logger.LogInformation(logMessage);

            AddLogLine(logMessage);
        }

        public void LogError(string message, params object[] args)
        {
            string logMessage = string.Format(message, args);
            Debug.LogError(logMessage);
            this.logger.LogError(logMessage);

            AddLogLine(logMessage);
        }

        public void LogWarning(string message, params object[] args)
        {
            string logMessage = string.Format(message, args);
            Debug.LogWarning(logMessage);
            this.logger.LogWarning(logMessage);

            AddLogLine(logMessage);
        }

        private void AddLogLine(string logMessage)
        {
            if (logMessage != this.lastMessage) // Skip repeats
            {
                this.logLines.Insert(0, logMessage);
                if (this.logLines.Count > maxLines)
                {
                    this.logLines.RemoveAt(maxLines - 1);
                }

                this.lastMessage = logMessage;
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
