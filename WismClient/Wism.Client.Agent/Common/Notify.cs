﻿using System;
using System.Threading;

namespace Wism.Client.Agent;

public static class Notify
{
    public const int SleepDuration = 2000;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public static void Alert(string message, params object[] args)
    {
        Console.Beep(1000, 500);
        Console.WriteLine(message, args);
        Thread.Sleep(SleepDuration);
    }

    public static void Information(string message, params object[] args)
    {
        Console.WriteLine(message, args);
    }

    public static void Display(string message, params object[] args)
    {
        Console.WriteLine(message, args);
        Thread.Sleep(SleepDuration);
    }

    public static void DisplayAndWait(string message, params object[] args)
    {
        Console.WriteLine(message, args);
        Console.ReadKey(true);
    }
}