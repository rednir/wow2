using System;
using System.Threading.Tasks;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Verbose;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Timers
{
    [Name("Timers")]
    [Group("timer")]
    [Alias("timers", "clock", "time")]
    [Summary("Create and manage timers and reminders.")]
    public class TimersModule : Module
    {
        public static void InitializeAllTimers()
        {
            lock (DataManager.AllGuildData)
            {
                foreach (var pair in DataManager.AllGuildData)
                {
                    try
                    {
                        var config = DataManager.AllGuildData[pair.Key].Timers;
                        foreach (UserTimer timer in config.UserTimers)
                            timer.Start();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex, $"Exception thrown when trying to initialize user timer for {pair.Value.NameOfGuild} ({pair.Key})");
                    }
                }
            }
        }

        public TimersModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Timers;

        [Command("start")]
        [Alias("new", "create")]
        [Summary("Starts a timer that will send a message when elapsed.")]
        public async Task StartAsync([Remainder] string message = null)
        {
            await new DateTimeSelectorMessage(async dt =>
            {
                if (dt <= DateTime.Now)
                {
                    await new WarningMessage("Try a time in the future.")
                        .SendAsync(Context.Channel);
                    return;
                }

                var timer = new UserTimer(Context, dt, message);
                timer.Start();

                await new TimerStartedMessage()
                    .SendAsync(Context.Channel);
            })
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
                ReplyToMessageId = timer.UserMessageId,
            }
            .SendAsync(Context.Channel);

            timer.Dispose();
        }
    }
}