using System;
using System.Threading.Tasks;
using System.Timers;
using Discord.Commands;
using wow2.Data;
using wow2.Extentions;
using wow2.Verbose.Messages;

namespace wow2.Modules.Timers
{
    [Name("Timers")]
    [Group("timer")]
    [Alias("timers", "clock", "time")]
    [Summary("Create and manage timers and reminders.")]
    public class TimersModule : Module
    {
        public TimersModuleConfig Config => DataManager.DictionaryOfGuildData[Context.Guild.Id].Timers;

        [Command("start")]
        [Alias("new", "create")]
        [Summary("Starts a timer that will send a message when elapsed.")]
        public async Task StartAsync(string time)
        {
            if (time.TryConvertToTimeSpan(out TimeSpan timeSpan))
                throw new CommandReturnException(Context, "Try something like `5m` or `30s`", "Invalid time.");
            if (timeSpan > TimeSpan.FromDays(90) || timeSpan < TimeSpan.FromSeconds(1))
                throw new CommandReturnException(Context, "Be sensible.");

            var timer = new UserTimer(timeSpan.TotalMilliseconds, Context);
            Config.UserTimers.Add(timer);

            await new SuccessMessage("Started a new timer.")
                .SendAsync(Context.Channel);
        }

        [Command("stop")]
        [Alias("cancel")]
        [Summary("Stops a timer. The ID is its place in the list as a number.")]
        public async Task Stop(int id)
        {
            int index = id - 1;

            if (index < 0 || index >= Config.UserTimers.Count)
                throw new CommandReturnException(Context, "That timer doesn't exist.");

            Config.UserTimers[index].Dispose();
            Config.UserTimers.RemoveAt(index);

            await new SuccessMessage("Removed timer.")
                .SendAsync(Context.Channel);
        }
    }
}