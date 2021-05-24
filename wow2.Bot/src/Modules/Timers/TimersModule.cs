using System;
using System.Threading.Tasks;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Timers
{
    [Name("Timers")]
    [Group("timer")]
    [Alias("timers", "clock", "time")]
    [Summary("Create and manage timers and reminders.")]
    public class TimersModule : Module
    {
        public TimersModule(BotService botService)
            : base(botService)
        {
        }

        public TimersModuleConfig Config => BotService.Data.AllGuildData[Context.Guild.Id].Timers;

        [Command("start")]
        [Alias("new", "create")]
        [Summary("Starts a timer that will send a message when elapsed.")]
        public async Task StartAsync(string time, [Remainder] string message = null)
        {
            if (time.TryConvertToTimeSpan(out TimeSpan timeSpan))
                throw new CommandReturnException(Context, "Try something like `5m` or `30s`", "Invalid time.");
            if (timeSpan > TimeSpan.FromDays(90) || timeSpan < TimeSpan.FromSeconds(1))
                throw new CommandReturnException(Context, "Be sensible.");

            _ = new UserTimer(Context, Config.UserTimers, timeSpan.TotalMilliseconds, message);
            await new SuccessMessage($"There are now `{Config.UserTimers.Count}` active timer(s)", "Started a new timer.")
                .SendAsync(Context.Channel);
        }

        [Command("stop")]
        [Alias("cancel", "remove", "delete")]
        [Summary("Stops the most recently created timer.")]
        public async Task StopAsync()
        {
            if (Config.UserTimers.Count == 0)
                throw new CommandReturnException(Context, "There are no active timers to remove.");

            int index = Config.UserTimers.Count - 1;
            UserTimer timer = Config.UserTimers[index];

            await new SuccessMessage("Removed this timer.")
            {
                ReplyToMessageId = timer.Context.Message.Id,
            }
            .SendAsync(Context.Channel);

            timer.Remove();
        }
    }
}