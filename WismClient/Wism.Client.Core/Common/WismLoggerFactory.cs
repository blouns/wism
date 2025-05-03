namespace Wism.Client.Common
{
    public class WismLoggerFactory : IWismLoggerFactory
    {
        public IWismLogger CreateLogger()
        {
            return new WismLogger();
        }
    }
}
