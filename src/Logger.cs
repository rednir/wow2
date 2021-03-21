using System;
using System.IO;
using System.Runtime.InteropServices;
using Discord;

namespace wow2
{
    public static class Logger
    {
        public static void LogProgramDetails()
            => Output($"wow2 {Program.Version}\nhttps://github.com/rednir/wow2\n-----------------\nRuntime version: {Environment.Version}\n{RuntimeInformation.OSDescription}\n-----------------\n");

        public static void Log(object message, LogSeverity severity = LogSeverity.Debug)
            => Output($"{DateTime.Now} [{severity.ToString()}] {message}");

        public static void Log(LogMessage logMessage)
            => Output($"{DateTime.Now} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Message}");

        public static void LogException(Exception exception, string message = "Exception was thrown:")
            => Output($"{DateTime.Now} [Exception] {message}\n------ START OF EXCEPTION ------\n\n{exception}\n\n------ END OF EXCEPTION ------");

        private static void Output(string message)
        { 
            try
            {
                File.AppendAllText($"{DataManager.LogsDirPath}/{Program.TimeStarted.ToString("yyyy-MM-dd_HH-mm-ss")}.log", message + "\n");
            }
            catch {}
            finally
            {
                Console.WriteLine(message);
            }
        }
    }
}