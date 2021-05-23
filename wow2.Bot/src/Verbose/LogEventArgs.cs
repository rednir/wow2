using System;

namespace wow2.Bot.Verbose
{
    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
        public delegate void LogEventHandler(object sender, LogEventArgs e);
    }
}