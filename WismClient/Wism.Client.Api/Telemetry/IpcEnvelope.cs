namespace Wism.Client.CommandPublisher
{
    public class IpcEnvelope
    {
        public string Type { get; set; }
        public object Payload { get; set; }
    }

}
