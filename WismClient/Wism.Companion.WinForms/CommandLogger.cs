using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Wism.CompanionApp.WinForms
{
    public class CommandLogger
    {
        private readonly List<object> _eventLog = new();

        public bool IsRecording { get; private set; } = false;

        public void Start() => IsRecording = true;
        public void Stop() => IsRecording = false;

        public void Log(object evt)
        {
            if (IsRecording)
                _eventLog.Add(evt);
        }

        public void Save(string filePath)
        {
            var json = JsonConvert.SerializeObject(_eventLog, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            File.WriteAllText(filePath, json);
        }


        public void Clear() => _eventLog.Clear();
    }
}
