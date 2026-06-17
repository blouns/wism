using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using WismCompanion.Transport;
using Debug = UnityEngine.Debug;

namespace WismCompanion.App
{
    /// <summary>
    /// Starts the repo-local SignalR bridge for explicit <c>signalr://</c> Companion endpoints when no
    /// server is already listening. Plain <c>ws://</c> endpoints are reserved for WismUnity's direct
    /// socket publisher and are never auto-owned by this launcher.
    /// </summary>
    public static class SignalRHostLauncher
    {
        private const int ConnectTimeoutMs = 250;

        private static bool launchAttempted;

        public static void EnsureRunningForEndpoint(string endpoint)
        {
            if (!TryGetBridgeEndpoint(endpoint, out var host, out var port))
            {
                return;
            }

            if (IsTcpPortOpen(host, port, ConnectTimeoutMs) || launchAttempted)
            {
                return;
            }

            launchAttempted = true;

            var repoRoot = FindRepoRoot(Application.dataPath);
            if (repoRoot == null)
            {
                Debug.LogWarning("[WismCompanion] Could not locate Wism.SignalR.Host beside the Companion build.");
                return;
            }

            if (!TryBuildStartInfo(repoRoot, out var startInfo))
            {
                Debug.LogWarning("[WismCompanion] Could not locate Wism.SignalR.Host project or executable.");
                return;
            }

            try
            {
                Process.Start(startInfo);
                Debug.Log("[WismCompanion] Started Wism.SignalR.Host for local socket endpoint.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WismCompanion] Failed to start Wism.SignalR.Host: {ex.Message}");
            }
        }

        public static bool TryGetBridgeEndpoint(string endpoint, out string host, out int port)
        {
            host = null;
            port = 0;

            if (!TryNormalizeBridgeUrl(endpoint, out var normalized) ||
                !Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!IsLoopbackHost(uri.Host))
            {
                return false;
            }

            host = uri.Host;
            port = uri.Port > 0 ? uri.Port : string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase) ? 443 : 80;
            return true;
        }

        public static bool TryNormalizeBridgeUrl(string endpoint, out string normalized)
        {
            normalized = null;

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return false;
            }

            var value = endpoint.Trim();
            if (value.StartsWith("signalr://", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "ws://" + value.Substring("signalr://".Length);
                return true;
            }

            if (value.StartsWith("signalr+ws://", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "ws://" + value.Substring("signalr+ws://".Length);
                return true;
            }

            if (value.StartsWith("signalr+wss://", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "wss://" + value.Substring("signalr+wss://".Length);
                return true;
            }

            return false;
        }

        public static string FindRepoRoot(string startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
            {
                return null;
            }

            var directory = Directory.Exists(startPath)
                ? new DirectoryInfo(startPath)
                : new FileInfo(startPath).Directory;

            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "WismClient", "Wism.SignalR.Host")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static bool TryBuildStartInfo(string repoRoot, out ProcessStartInfo startInfo)
        {
            var hostDir = Path.Combine(repoRoot, "WismClient", "Wism.SignalR.Host");
            var hostExe = Path.Combine(hostDir, "bin", "Debug", "net8.0", "Wism.SignalR.Host.exe");
            if (File.Exists(hostExe))
            {
                startInfo = CreateHiddenStartInfo(hostExe, null, hostDir);
                return true;
            }

            var hostProject = Path.Combine(hostDir, "Wism.SignalR.Host.csproj");
            if (File.Exists(hostProject))
            {
                startInfo = CreateHiddenStartInfo("dotnet", $"run --project \"{hostProject}\"", repoRoot);
                return true;
            }

            startInfo = null;
            return false;
        }

        private static ProcessStartInfo CreateHiddenStartInfo(string fileName, string arguments, string workingDirectory)
        {
            return new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        private static bool IsTcpPortOpen(string host, int port, int timeoutMs)
        {
            try
            {
                using var client = new TcpClient();
                var task = client.ConnectAsync(host, port);
                return task.Wait(timeoutMs) && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsLoopbackHost(string host)
        {
            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
