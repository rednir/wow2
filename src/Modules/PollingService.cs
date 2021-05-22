using System;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using wow2.Extensions;
using wow2.Verbose;

namespace wow2.Modules
{
    /// <summary>Service allowing modules to execute code on intervals.</summary>
    public static class PollingService
    {
        public static void CreateService(Func<Task> action, int intervalMinutes)
        {
            var timer = new Timer(intervalMinutes * 60000)
            {
                AutoReset = true,
            };

            timer.Elapsed += async (source, e) =>
            {
                try
                {
                    await action.Invoke();
                    Logger.Log($"Finished running polling service '{action.Method.Name}'.", LogSeverity.Debug);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Exception thrown when running polling service '{action.Method.Name}'");
                }
            };

            timer.Start();
            Logger.Log($"Started polling service '{action.Method.Name}', set to run every {intervalMinutes} minutes.", LogSeverity.Debug);
        }
    }
}