using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Telemetry;
using Wism.CompanionApp.WinForms;

namespace Wism.Companion.WinForms
{
    public partial class DebugConsoleForm : Form
    {
        private readonly SignalRClient _client;
        private readonly CommandLogger _logger = new();
        private readonly TelemetryChannelRegistry _channels = new();
        private readonly TelemetryLogBuffer _logBuffer = new();
        private string? _selectedChannel;

        public DebugConsoleForm()
        {
            InitializeComponent();
            ConfigureLogGrid();
            _client = new SignalRClient(
                SetStatus,
                AddLogEntry,
                _logger.Log,
                ReceiveMapSnapshot,
                ReceiveCommand);
        }

        private async void DebugConsoleForm_Load(object sender, EventArgs e)
        {
            SetStatus("Connecting to SignalR host...");
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

                var file = Path.Combine(folder, $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                _logger.Save(file);
                buttonRecord.Text = "Record";
                SetStatus($"Recording saved to {file}");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_selectedChannel))
                {
                    SetStatus("Select a channel before recording");
                    return;
                }

                _logger.Clear();
                _logger.Start(_selectedChannel);
                buttonRecord.Text = "Stop";
                SetStatus($"Recording {_selectedChannel}...");
            }
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
                SetStatus($"Replaying {Path.GetFileName(open.FileName)}...");
                await engine.ReplayAsync(open.FileName, 500);
                SetStatus("Replay finished");
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            _logBuffer.Clear(_selectedChannel);
            RefreshLogGrid();
            SetStatus(string.IsNullOrWhiteSpace(_selectedChannel)
                ? "No channel selected"
                : $"Cleared log for {_selectedChannel}");
        }

        private void ReceiveCommand(CommandExecutedEvent command)
        {
            var channel = TelemetryContext.ChannelIdOrDefault(command.Telemetry);
            RegisterChannelOption(channel);

            if (command.TargetPosition is not null && ShouldRenderChannel(channel))
            {
                mapRenderer.TrackArmyAt(command.TargetPosition);
            }
        }

        private void ReceiveMapSnapshot(MapSnapshot snapshot)
        {
            var channel = _channels.Register(snapshot);
            RegisterChannelOption(channel);
            if (ShouldRenderChannel(channel))
            {
                RenderSnapshot(snapshot);
            }
        }

        private void ReplayEvent(object evt)
        {
            var entry = TelemetryLogEntry.Replay(evt);
            AddLogEntry(entry);

            if (evt is CommandExecutedEvent command)
            {
                ReceiveCommand(command);
            }
            else if (evt is MapSnapshot map)
            {
                ReceiveMapSnapshot(map);
            }
            else
            {
                RegisterChannelOption(entry.ChannelId);
            }
        }

        private void AddLogEntry(TelemetryLogEntry entry)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => AddLogEntry(entry));
                return;
            }

            RegisterChannelOption(entry.ChannelId);
            var count = _logBuffer.Add(entry);

            if (ShouldRenderChannel(entry.ChannelId))
            {
                InsertVisibleEntry(entry);
                labelLogStats.Text = $"{count} events";
            }
        }

        private void comboChannels_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedChannel = comboChannels.SelectedItem?.ToString();
            var snapshot = _channels.GetLatestMap(_selectedChannel);
            if (snapshot is not null)
            {
                RenderSnapshot(snapshot);
            }

            RefreshLogGrid();
            SetStatus(string.IsNullOrWhiteSpace(_selectedChannel)
                ? "No channel selected"
                : $"Viewing {_selectedChannel}");
        }

        private void dataGridLog_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridLog.CurrentRow?.DataBoundItem is TelemetryLogEntry entry)
            {
                textLogDetail.Text = entry.Detail;
            }
        }

        private void RegisterChannelOption(string channel)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => RegisterChannelOption(channel));
                return;
            }

            if (!comboChannels.Items.Contains(channel))
            {
                comboChannels.Items.Add(channel);
            }

            if (_selectedChannel is null)
            {
                _selectedChannel = channel;
                comboChannels.SelectedItem = channel;
            }
        }

        private bool ShouldRenderChannel(string channel)
        {
            return string.Equals(_selectedChannel, channel, StringComparison.OrdinalIgnoreCase);
        }

        private void RenderSnapshot(MapSnapshot snapshot)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => RenderSnapshot(snapshot));
                return;
            }

            mapRenderer.InvertYAxis = snapshot.InvertYAxis;
            mapRenderer.UpdateMap(snapshot);
        }

        private void ConfigureLogGrid()
        {
            dataGridLog.AutoGenerateColumns = false;
            dataGridLog.Columns.Clear();
            dataGridLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TelemetryLogEntry.LocalTime),
                HeaderText = "Time",
                Width = 72
            });
            dataGridLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TelemetryLogEntry.Category),
                HeaderText = "Kind",
                Width = 116
            });
            dataGridLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TelemetryLogEntry.Summary),
                HeaderText = "Event",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            dataGridLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TelemetryLogEntry.Result),
                HeaderText = "Result",
                Width = 130
            });
        }

        private void InsertVisibleEntry(TelemetryLogEntry entry)
        {
            var entries = _logBuffer.GetEntries(_selectedChannel).ToList();
            dataGridLog.DataSource = entries;
            dataGridLog.ClearSelection();
            if (entries.Count > 0)
            {
                dataGridLog.Rows[0].Selected = true;
                dataGridLog.CurrentCell = dataGridLog.Rows[0].Cells[0];
                textLogDetail.Text = entries[0].Detail;
            }
        }

        private void RefreshLogGrid()
        {
            var entries = _logBuffer.GetEntries(_selectedChannel).ToList();
            dataGridLog.DataSource = entries;
            labelLogStats.Text = string.IsNullOrWhiteSpace(_selectedChannel)
                ? "0 events"
                : $"{_logBuffer.GetCount(_selectedChannel)} events";
            textLogDetail.Text = entries.Count == 0 ? string.Empty : entries[0].Detail;
        }

        private void SetStatus(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => SetStatus(message));
                return;
            }

            labelStatus.Text = message;
        }
    }
}
