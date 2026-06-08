using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using WismCompanion.State;
using WismCompanion.Transport;

namespace WismCompanion.UI
{
    /// <summary>
    /// Top-level UI controller: queries the loaded UXML, hosts the map/log/inspector views, wires
    /// the connection bar and channel selector, shows a "waiting to connect" hero when no channels
    /// are present, and refreshes panels from <see cref="CompanionState"/>.
    /// </summary>
    public sealed class CompanionController
    {
        private const string NoChannelsPlaceholder = "(no channels)";

        private readonly CompanionState state;
        private readonly MapView mapView;
        private readonly LogView logView;
        private readonly InspectorView inspector;

        private readonly TextField hostField;
        private readonly VisualElement statusDot;
        private readonly Label statusLabel;
        private readonly DropdownField channelDropdown;
        private readonly Label statsLabel;

        private readonly VisualElement emptyState;
        private readonly VisualElement emptySpinner;
        private readonly Label emptyStatus;
        private readonly Label emptyHost;

        private CompanionConnectionStatus lastStatus = CompanionConnectionStatus.Disconnected;
        private bool pulseOn;

        /// <summary>Raised when the user clicks Connect; the argument is the requested host URL.</summary>
        public event Action<string> ConnectRequested;

        public CompanionController(VisualElement root, CompanionState state)
        {
            this.state = state;

            hostField = root.Q<TextField>("host-field");
            var connectButton = root.Q<Button>("connect-button");
            statusDot = root.Q<VisualElement>("status-dot");
            statusLabel = root.Q<Label>("status-label");
            channelDropdown = root.Q<DropdownField>("channel-dropdown");
            statsLabel = root.Q<Label>("stats-label");

            emptyState = root.Q<VisualElement>("empty-state");
            emptySpinner = root.Q<VisualElement>("empty-spinner");
            emptyStatus = root.Q<Label>("empty-status");
            emptyHost = root.Q<Label>("empty-host");

            var mapHost = root.Q<VisualElement>("map-host");
            mapView = new MapView();
            mapHost?.Add(mapView);
            mapView.SelectionChanged += OnMapSelection;

            logView = new LogView(root.Q<ListView>("log-list"), root.Q<Label>("detail-label"));
            inspector = new InspectorView(root.Q<VisualElement>("inspector-content"));
            inspector.ShowEmpty("Click a tile, army, or city on the map.");

            var logModeRaw = root.Q<Button>("log-mode-raw");
            var logModeSimple = root.Q<Button>("log-mode-simple");
            var logFilterField = root.Q<TextField>("log-filter");

            if (logModeRaw != null)
                logModeRaw.clicked += () => SetLogMode(LogViewMode.Raw, logModeRaw, logModeSimple);
            if (logModeSimple != null)
                logModeSimple.clicked += () => SetLogMode(LogViewMode.Simple, logModeRaw, logModeSimple);
            if (logFilterField != null)
                logFilterField.RegisterValueChangedCallback(evt => logView.SetFilter(evt.newValue));

            if (connectButton != null)
            {
                connectButton.clicked += () => ConnectRequested?.Invoke(hostField != null ? hostField.value : null);
            }

            channelDropdown?.RegisterValueChangedCallback(evt =>
            {
                if (!string.IsNullOrEmpty(evt.newValue) && evt.newValue != NoChannelsPlaceholder)
                {
                    state.SelectChannel(evt.newValue);
                }
            });

            state.Changed += Refresh;
            state.ChannelsChanged += RefreshChannels;

            // Pulse the waiting spinner while not connected to data.
            root.schedule.Execute(PulseSpinner).Every(550);

            RefreshChannels();
            Refresh();
        }

        public string HostUrl => hostField != null ? hostField.value : null;

        public void SetHostUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            hostField?.SetValueWithoutNotify(url);
            if (emptyHost != null)
            {
                emptyHost.text = url;
            }
        }

        public void SetConnectionStatus(CompanionConnectionStatus status, string detail)
        {
            lastStatus = status;

            // Top bar shows a short label (full text would clip); hover for the detail.
            if (statusLabel != null)
            {
                statusLabel.text = ShortStatus(status);
                statusLabel.tooltip = detail;
            }

            if (statusDot != null)
            {
                statusDot.style.backgroundColor = StatusColor(status);
                statusDot.tooltip = detail;
            }

            // The hero has room for the full message.
            if (emptyStatus != null)
            {
                emptyStatus.text = detail;
            }

            if (emptySpinner != null)
            {
                emptySpinner.style.backgroundColor = StatusColor(status);
            }
        }

        private void RefreshChannels()
        {
            if (channelDropdown != null)
            {
                if (state.Channels.Count == 0)
                {
                    channelDropdown.choices = new List<string>();
                    channelDropdown.SetValueWithoutNotify(NoChannelsPlaceholder);
                    channelDropdown.SetEnabled(false);
                }
                else
                {
                    channelDropdown.SetEnabled(true);
                    channelDropdown.choices = new List<string>(state.Channels);
                    channelDropdown.SetValueWithoutNotify(state.SelectedChannel);
                }
            }

            UpdateEmptyState();
        }

        private void Refresh()
        {
            var channel = state.SelectedChannel;
            var map = state.GetLatestMap(channel);

            mapView.SetSnapshot(map);
            logView.SetEntries(state.GetLog(channel));

            if (statsLabel != null)
            {
                if (map == null)
                {
                    statsLabel.text = $"{state.TotalEventsReceived} events";
                }
                else
                {
                    var armies = map.Armies?.Count ?? 0;
                    var cities = map.Cities?.Count ?? 0;
                    statsLabel.text = $"{map.Width}x{map.Height} · {armies} armies · {cities} cities · {state.TotalEventsReceived} events";
                }
            }

            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            if (emptyState == null)
            {
                return;
            }

            var hasData = state.Channels.Count > 0;
            emptyState.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void PulseSpinner()
        {
            if (emptySpinner == null || emptyState == null)
            {
                return;
            }

            if (emptyState.resolvedStyle.display == DisplayStyle.None)
            {
                return;
            }

            var animate = lastStatus != CompanionConnectionStatus.Connected;
            if (!animate)
            {
                emptySpinner.style.opacity = 1f;
                return;
            }

            pulseOn = !pulseOn;
            emptySpinner.style.opacity = pulseOn ? 1f : 0.3f;
        }

        private void OnMapSelection(MapSelection selection)
        {
            inspector.Show(selection, state.GetLatestMap(state.SelectedChannel));
        }

        private void SetLogMode(LogViewMode mode, Button rawBtn, Button simpleBtn)
        {
            logView.SetMode(mode);
            rawBtn?.EnableInClassList("log-mode-btn--active", mode == LogViewMode.Raw);
            simpleBtn?.EnableInClassList("log-mode-btn--active", mode == LogViewMode.Simple);
        }

        private static string ShortStatus(CompanionConnectionStatus status)
        {
            return status switch
            {
                CompanionConnectionStatus.Connected => "Connected",
                CompanionConnectionStatus.Connecting => "Waiting…",
                CompanionConnectionStatus.Reconnecting => "Reconnecting…",
                CompanionConnectionStatus.Faulted => "Error",
                _ => "Disconnected"
            };
        }

        private static Color StatusColor(CompanionConnectionStatus status)
        {
            return status switch
            {
                CompanionConnectionStatus.Connected => new Color(0.30f, 0.78f, 0.36f),
                CompanionConnectionStatus.Connecting => new Color(0.90f, 0.72f, 0.22f),
                CompanionConnectionStatus.Reconnecting => new Color(0.90f, 0.52f, 0.22f),
                CompanionConnectionStatus.Faulted => new Color(0.88f, 0.30f, 0.30f),
                _ => new Color(0.50f, 0.50f, 0.50f)
            };
        }
    }
}
