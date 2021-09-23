using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using wow2.Bot.Verbose;

namespace wow2.Bot
{
    /// <summary>Service allowing modules to execute code on intervals.</summary>
    public static class PollingService
    {
        public static List<PollingTask> PollingServiceTimers { get; } = new();

        public static void CreateTask(Func<Task> func, int intervalMinutes, bool runImmediately = false)
        {
            var task = new PollingTask(func, intervalMinutes);
            var timer = new Timer(intervalMinutes * 60000);

            timer.Elapsed += async (source, e) => await task.InvokeAsync();
            timer.Start();
            PollingServiceTimers.Add(task);
            Logger.Log($"Started polling task '{task}', set to run every {intervalMinutes} minutes.", LogSeverity.Debug);

            if (runImmediately)
                _ = task.InvokeAsync();
        }
    }
}