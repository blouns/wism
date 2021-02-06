using System;
using System.Threading;

namespace Wism.Client.Agent
{
    public static class Notify
    {
        public const int SleepDuration = 2000;

        public static void Alert(string message, params object[] args)
        {
            Console.Beep(1000, 500);
            Console.WriteLine(String.Format(message, args));
            Thread.Sleep(SleepDuration);
        }

        public static void Information(string message, params object[] args)
        {
            Console.WriteLine(String.Format(message, args));
        }

        public static void Display(string message, params object[] args)
        {
            Console.WriteLine(String.Format(message, args));
            Thread.Sleep(SleepDuration);
        }

        public static void DisplayAndWait(string message, params object[] args)
        {
            Console.WriteLine(String.Format(message, args));
            Console.ReadKey();
        }
    }
}
