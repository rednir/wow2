using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose;

namespace wow2.Bot.Modules
{
    /// <summary>Service allowing modules to execute code on intervals.</summary>
    public static class PollingService
    {
        public static Dictionary<string, Timer> PollingServiceTimers { get; } = new();
        
        public static void CreateService(Func<Task> action, int intervalMinutes)
        {
            var timer = new Timer(intervalMinutes * 60000)
            {
                AutoReset = true,
            };

            int consecutiveFailures = 0;
            const int maxConsecutiveFailures = 2;
            timer.Elapsed += async (source, e) =>
            {
                try
                {
                    await action.Invoke();
                    consecutiveFailures = 0;
                    Logger.Log($"Finished running polling service '{action.Method.Name}'.", LogSeverity.Debug);
                }
                catch (Exception ex)
                {
                    consecutiveFailures++;
                    if (consecutiveFailures > maxConsecutiveFailures)
                    {
                        Logger.LogException(ex, $"Polling service '{action.Method.Name}' has failed {consecutiveFailures} times in a row and will be terminated.");
                        timer.Stop();
                    }
                    else
                    {
                        Logger.LogException(ex, $"Exception thrown when running polling service '{action.Method.Name}'. This is failure number {consecutiveFailures}.");
                    }
                }
            };

            timer.Start();
            PollingServiceTimers.Add(action.Method.Name, timer);
            Logger.Log($"Started polling service '{action.Method.Name}', set to run every {intervalMinutes} minutes.", LogSeverity.Debug);
        }
    }
}