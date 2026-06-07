namespace WismCompanion.Transport
{
    public enum CompanionConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Faulted
    }

    /// <summary>
    /// A source of WISM telemetry for the companion. Implementations run their own background I/O and
    /// expose decoded messages via a thread-safe queue that the Unity main thread drains each frame.
    /// </summary>
    public interface ICompanionTransport
    {
        CompanionConnectionStatus Status { get; }

        string StatusDetail { get; }

        /// <summary>Human-readable endpoint (e.g. <c>pipe://wism-commands</c> or <c>ws://host/gameHub</c>).</summary>
        string Endpoint { get; }

        bool TryDequeue(out InboundMessage message);

        void Start();

        void Stop();
    }
}
