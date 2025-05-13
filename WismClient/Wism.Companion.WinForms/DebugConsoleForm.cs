using Wism.CompanionApp.WinForms;

namespace Wism.Companion.WinForms
{
    public partial class DebugConsoleForm : Form
    {
        private readonly SignalRClient _client;
        private readonly CommandLogger _logger = new();


        public DebugConsoleForm()
        {
            InitializeComponent();
            _client = new SignalRClient(LogMessage, _logger.Log, mapRenderer.UpdateMap);
        }

        private async void DebugConsoleForm_Load(object sender, EventArgs e)
        {
            await _client.ConnectAsync();
        }

        private void buttonRecord_Click(object sender, EventArgs e)
        {
            if (_logger.IsRecording)
            {
                _logger.Stop();
                var recordingFolder = "Recording";
                var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, recordingFolder);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                var file = Path.Combine(folder, $"recording_{ DateTime.Now:yyyyMMdd_HHmmss}.json");
                _logger.Save(file);
                buttonRecord.Text = "Record";
                LogMessage($"[Logger] Recording saved to {file}");
            }
            else
            {
                _logger.Clear();
                _logger.Start();
                buttonRecord.Text = "Stop";
                LogMessage("[Logger] Recording started...");
            }
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => LogMessage(message));
                return;
            }

            listBoxLog.Items.Insert(0, $"{DateTime.Now:T} - {message}");
        }

        private async void buttonReplay_Click(object sender, EventArgs e)
        {
            using var open = new OpenFileDialog
            {
                Filter = "JSON logs|*.json",
                Title = "Open Replay Log"
            };

            if (open.ShowDialog() == DialogResult.OK)
            {
                var engine = new ReplayEngine(ReplayEvent);
                LogMessage($"[Replay] Starting replay from {Path.GetFileName(open.FileName)}...");
                await engine.ReplayAsync(open.FileName, 500);
                LogMessage($"[Replay] Finished replay.");
            }
        }

        private void ReplayEvent(object evt)
        {
            if (evt is Wism.Companion.Shared.Events.CommandExecutedEvent cmd)
            {
                if (cmd.TargetPosition != null)
                {
                    // Center map on target
                    mapRenderer.TrackArmyAt(cmd.TargetPosition);
                }

                string actor = cmd.ActorId ?? "Unknown";
                string target = cmd.TargetPosition != null
                    ? $"({cmd.TargetPosition.X},{cmd.TargetPosition.Y})"
                    : cmd.TargetId ?? "None";

                string details = cmd.Parameters.Count > 0
                    ? string.Join(", ", cmd.Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : "no params";

                LogMessage($"[REPLAY:{cmd.CommandType}] {actor} → {target} [{details}] → {cmd.Result}");
            }
            else if (evt is Wism.Companion.Shared.Events.MapSnapshot map)
            {
                mapRenderer.UpdateMap(map);
                LogMessage($"[REPLAY:MAP] {map.Width}x{map.Height} with {map.Armies.Count} armies");
            }
            else
            {
                LogMessage("[REPLAY] Unknown event type");
            }
        }


    }
}
