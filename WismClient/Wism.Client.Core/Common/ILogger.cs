namespace Wism.Client.Common
{
    public interface IWismLogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}
