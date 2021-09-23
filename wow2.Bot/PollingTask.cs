using System;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using wow2.Bot.Verbose;

namespace wow2.Bot
{
    /// <summary>Task created by a <see cref="PollingService" />.</summary>
    public class PollingTask
    {
        public PollingTask(Func<Task> func, int intervalMinutes, int maxConsecutiveFailures = 2)
        {
            Func = func;
            MaxConsecutiveFailures = maxConsecutiveFailures;
            IntervalMinutes = intervalMinutes;
        }

        private readonly Func<Task> Func;

        public string Name => $"{Func.Method.DeclaringType.Name}.{Func.Method.Name}";

        public int IntervalMinutes { get; }

        public int MaxConsecutiveFailures { get; }

        public int ConsecutiveFailures { get; private set; }

        public bool Blocked { get; set; }

        public DateTime LastRun { get; private set; }

        public async Task InvokeAsync()
        {
            if (Blocked)
                return;

            try
            {
                await Func.Invoke();
                ConsecutiveFailures = 0;
                Logger.Log($"Finished running polling task '{Name}'.", LogSeverity.Debug);
            }
            catch (Exception ex)
            {
                ConsecutiveFailures++;
                if (ConsecutiveFailures > MaxConsecutiveFailures)
                {
                    Blocked = true;
                    Logger.LogException(ex, $"Polling task '{Name}' has failed {ConsecutiveFailures} times in a row and will be blocked.");
                }
                else
                {
                    Logger.LogException(ex, $"Exception thrown when running polling service '{Name}'. This is failure number {ConsecutiveFailures}.");
                }
            }
            finally
            {
                LastRun = DateTime.Now;
            }
        }

        public override string ToString() => Name;
    }
}