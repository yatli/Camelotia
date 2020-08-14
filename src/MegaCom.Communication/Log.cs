using System;

namespace MegaCom
{
    public static class Log
    {
        public static bool LogToStdout = false;

        public static event Action<string> LogWritten = delegate{ };

        public static void WriteLine(string value)
        {
            if (LogToStdout) System.Console.WriteLine(value);
            LogWritten(value);
        }
    }
}