using System;

namespace wow2.Bot
{
    /// <summary>Declares a method to run on intervals (to poll a server).
    /// The method take BotService as its only parameter and have a return type of Task.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PollTaskAttribute : Attribute
    {
        public PollTaskAttribute(int intervalMinutes, bool executeImmediately = false)
        {
            IntervalMinutes = intervalMinutes;
            ExecuteImmediately = executeImmediately;
        }

        public int IntervalMinutes { get; }
        public bool ExecuteImmediately { get; }
    }
}