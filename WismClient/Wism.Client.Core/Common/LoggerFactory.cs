namespace Wism.Client.Common
{
    public class WismLoggerFactory : ILoggerFactory
    {
        public ILogger CreateLogger()
        {
            return new WismLogger();
        }
    }
}