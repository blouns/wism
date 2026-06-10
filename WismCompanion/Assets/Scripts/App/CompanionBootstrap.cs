using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using WismCompanion.State;
using WismCompanion.Transport;
using WismCompanion.UI;

namespace WismCompanion.App
{
    /// <summary>
    /// Entry point for the standalone companion. Auto-starts on play/build (no scene wiring needed),
    /// loads the UI Toolkit document from Resources, connects to the SignalR host, and pumps inbound
    /// hub messages onto the Unity main thread each frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CompanionBootstrap : MonoBehaviour
    {
        // Default to the WebSocket transport (connects to Wism.SignalR.Host). A direct named-pipe
        // transport is still available by entering "pipe://wism-commands" in the connection field.
        private const string DefaultEndpoint = "ws://localhost:5000/gameHub";

        private const float ChannelPruneInterval = 15f;
        private const float ChannelStaleSecs = 60f;

        private CompanionState state;
        private CompanionController controller;
        private ICompanionTransport client;
        private float channelPruneTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("WismCompanion");
            DontDestroyOnLoad(go);
            go.AddComponent<CompanionBootstrap>();
        }

        private void Awake()
        {
            // Keep updating (and streaming telemetry) even when the window isn't focused — otherwise
            // Unity throttles Update() in the background and the view only refreshes when you click it.
            Application.runInBackground = true;

            state = new CompanionState();

            if (!BuildUi())
            {
                return;
            }

            controller.ConnectRequested += Reconnect;
            StartClient(DefaultEndpoint);
        }

        private void Update()
        {
            if (client != null)
            {
                while (client.TryDequeue(out var message))
                {
                    if (message.Kind == InboundMessage.MessageKind.MapSnapshot)
                        state.ApplyMap(message.Map);
                    else
                        state.ApplyCommand(message.Command);
                }

                controller?.SetConnectionStatus(client.Status, client.StatusDetail);
            }

            channelPruneTimer += Time.deltaTime;
            if (channelPruneTimer >= ChannelPruneInterval)
            {
                channelPruneTimer = 0f;
                state.PruneStaleChannels(TimeSpan.FromSeconds(ChannelStaleSecs));
            }
        }

        private void OnDestroy()
        {
            client?.Stop();
        }

        private void Reconnect(string endpoint)
        {
            client?.Stop();
            StartClient(string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint);
        }

        private void StartClient(string endpoint)
        {
            client = CreateTransport(endpoint);
            controller?.SetHostUrl(client.Endpoint);
            controller?.SetConnectionStatus(CompanionConnectionStatus.Connecting, client.StatusDetail);
            client.Start();
        }

        /// <summary>
        /// Picks a transport from the endpoint scheme: <c>ws/wss/http/https</c> → SignalR WebSocket
        /// (remote/multi-client), otherwise a direct named pipe (<c>pipe://name</c> or a bare name).
        /// </summary>
        private static ICompanionTransport CreateTransport(string endpoint)
        {
            var value = string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint.Trim();

            if (value.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("wss://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return new SignalRJsonClient(value);
            }

            if (value.StartsWith("pipe://", StringComparison.OrdinalIgnoreCase))
            {
                return new NamedPipeTransport(value.Substring("pipe://".Length));
            }

            if (value.StartsWith("pipe:", StringComparison.OrdinalIgnoreCase))
            {
                return new NamedPipeTransport(value.Substring("pipe:".Length));
            }

            return new NamedPipeTransport(value);
        }

        private bool BuildUi()
        {
            var uxml = Resources.Load<VisualTreeAsset>("CompanionView");
            if (uxml == null)
            {
                Debug.LogError("[WismCompanion] Resources/CompanionView.uxml not found. " +
                               "Run menu 'WISM > Create Companion Assets' once, then press Play.");
                return false;
            }

            var panelSettings = Resources.Load<PanelSettings>("CompanionPanelSettings") ?? CreateFallbackPanelSettings();

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = uxml;

            var root = document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[WismCompanion] UIDocument produced no root visual element.");
                return false;
            }

            var theme = Resources.Load<StyleSheet>("CompanionTheme");
            if (theme != null)
            {
                root.styleSheets.Add(theme);
            }

            ApplyDefaultFont(root);

            controller = new CompanionController(root, state);
            return true;
        }

        /// <summary>
        /// Assigns a concrete, main-thread-resolved <see cref="FontAsset"/> to the root as an inherited
        /// style. Without this, UITK's multithreaded text job lazily resolves the font on a worker
        /// thread and throws "EnsureRunningOnMainThread" every frame text is generated.
        /// </summary>
        private static void ApplyDefaultFont(VisualElement root)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
            {
                return;
            }

            var fontAsset = FontAsset.CreateFontAsset(font);
            if (fontAsset != null)
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(fontAsset));
            }
        }

        private static PanelSettings CreateFallbackPanelSettings()
        {
            Debug.LogWarning("[WismCompanion] Resources/CompanionPanelSettings.asset not found; using a " +
                             "runtime fallback. Run menu 'WISM > Create Companion Assets' for the themed setup.");

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;

            var theme = Resources.Load<ThemeStyleSheet>("CompanionRuntimeTheme");
            if (theme != null)
            {
                settings.themeStyleSheet = theme;
            }

            return settings;
        }
    }
}
