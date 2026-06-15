using Wism.Client.Terminal.Cli;

namespace Wism.Client.Terminal;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return WismTerminalApp.Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
