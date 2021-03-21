using System;
using System.IO;
using Discord;

namespace wow2
{
    public static class Logger
    {
        public static void Log(object message, LogSeverity severity = LogSeverity.Debug)
            => Output($"{DateTime.Now} [{severity.ToString()}] {message}");

        public static void Log(LogMessage logMessage)
            => Output($"{DateTime.Now} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Message}");

        public static void LogException(Exception exception, string message = "Exception was thrown:")
            => Output($"{DateTime.Now} [Exception] {message}\n------ START OF EXCEPTION ------\n\n{exception}\n\n------ END OF EXCEPTION ------");

        private static void Output(string message)
        { 
            File.AppendAllText($"{DataManager.LogsDirPath}/{Program.TimeStarted.ToString("yyyy-MM-dd_HH-mm-ss")}.log", message + "\n");
            Console.WriteLine(message);
        }
    }
}