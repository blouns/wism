using Newtonsoft.Json;

namespace Wism.CompanionApp.WinForms
{
    public class ReplayEngine
    {
        private readonly Action<object> _onReplayEvent;

        public ReplayEngine(Action<object> onReplayEvent)
        {
            _onReplayEvent = onReplayEvent;
        }

        public async Task ReplayAsync(string filePath, int delayMs = 500)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Replay file not found.", filePath);

            var json = File.ReadAllText(filePath);
            var events = JsonConvert.DeserializeObject<List<object>>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            foreach (var evt in events)
            {
                _onReplayEvent(evt);
                await Task.Delay(delayMs);
            }
        }
    }
}
