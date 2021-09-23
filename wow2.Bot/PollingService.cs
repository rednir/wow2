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
        public static Dictionary<string, Timer> PollingServiceTimers { get; } = new();

        public static void CreateService(Func<Task> action, int intervalMinutes, bool runImmediately = false)
        {
            var timer = new Timer(intervalMinutes * 60000)
            {
                AutoReset = true,
            };

            const int maxConsecutiveFailures = 2;
            int consecutiveFailures = 0;
            string name = $"{action.Method.DeclaringType.Name}.{action.Method.Name}";
            timer.Elapsed += async (source, e) => await elapsed();

            timer.Start();
            PollingServiceTimers.Add(name, timer);
            Logger.Log($"Started polling service '{name}', set to run every {intervalMinutes} minutes.", LogSeverity.Debug);

            if (runImmediately)
                _ = elapsed();

            async Task elapsed()
            {
                try
                {
                    await action.Invoke();
                    consecutiveFailures = 0;
                    Logger.Log($"Finished running polling service '{name}'.", LogSeverity.Debug);
                }
                catch (Exception ex)
                {
                    consecutiveFailures++;
                    if (consecutiveFailures > maxConsecutiveFailures)
                    {
                        Logger.LogException(ex, $"Polling service '{name}' has failed {consecutiveFailures} times in a row and will be terminated.");
                        timer.Stop();
                    }
                    else
                    {
                        Logger.LogException(ex, $"Exception thrown when running polling service '{name}'. This is failure number {consecutiveFailures}.");
                    }
                }
            }
        }
    }
}