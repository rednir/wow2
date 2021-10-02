using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
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
        public async Task StartAsync([Name("MESSAGE")][Remainder] string messageString = null)
        {
            await new DateTimeSelectorMessage(
                onConfirm: async dt =>
                {
                    TimeSpan timeSpan = dt - DateTime.Now;

                    await new QuestionMessage(
                        description: "Want the timer to repeat?",
                        onConfirm: async () =>
                        {
                            await new TimeSpanSelectorMessage(
                                onConfirm: async ts => await startTimer(timeSpan, ts),
                                description: "The timer will repeat every...",
                                min: TimeSpan.FromMinutes(30))
                                    .SendAsync(Context.Channel);
                        },
                        onDeny: async () => await startTimer(timeSpan, null))
                            .SendAsync(Context.Channel);
                },
                min: DateTime.Now)
                    .SendAsync(Context.Channel);

            async Task startTimer(TimeSpan timeSpan, TimeSpan? repeatEvery)
            {
                var timer = new UserTimer(
                    context: Context,
                    timeSpan: timeSpan,
                    sendToChannelId: Config.AnnouncementsChannelId != default ? Config.AnnouncementsChannelId : Context.Channel.Id,
                    messageString: messageString,
                    repeatEvery: repeatEvery);

                timer.Start();
                await new TimerStartedMessage(timer)
                    .SendAsync(Context.Channel);
            }
        }

        // TODO: would be nice to get rid of this.
        [Command("start-legacy")]
        [Summary("Starts a timer for a specific time span that will send a message when elapsed.")]
        public async Task StartLegacyAsync(string time, [Remainder] string message = null)
        {
            if (time.TryConvertToTimeSpan(out TimeSpan timeSpan))
                throw new CommandReturnException(Context, "Try something like `5m` or `30s`", "Invalid time.");

            var timer = new UserTimer(Context, timeSpan, Config.AnnouncementsChannelId != default ? Config.AnnouncementsChannelId : Context.Channel.Id, message, null);
            timer.Start();

            await new TimerStartedMessage(timer)
                .SendAsync(Context.Channel);
        }

        [Command("list")]
        [Summary("Lists all active timers")]
        public async Task ListAsync(int page = 1)
        {
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();

            int num = 1;
            foreach (UserTimer timer in Config.UserTimers)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{num}) " + (timer.MessageString ?? "<unnamed timer>"),
                    Value = $"[See message]({timer.UserMessageUrl}) • Scheduled for {timer.TargetDateTime.ToDiscordTimestamp("F")}{(timer.RepeatEvery == null ? null : $" • Repeats every {timer.RepeatEvery.Value}")}",
                });
                num++;
            }

            await new PagedMessage(
                fieldBuilders: listOfFieldBuilders,
                description: $"*There are {Config.UserTimers.Count} timers in total, as listed below.*",
                title: "⏰ Timers",
                page: page)
                    .SendAsync(Context.Channel);
        }

        [Command("stop")]
        [Alias("cancel", "remove", "delete")]
        [Summary("Stops the timer from its place in the list.")]
        public async Task StopAsync(int id)
        {
            if (id > Config.UserTimers.Count || id < 1)
                throw new CommandReturnException(Context, "No such icon with that ID.");

            Config.UserTimers[id - 1].Stop();
            await new SuccessMessage("Stopped timer.")
                .SendAsync(Context.Channel);
        }

        [Command("set-announcements-channel")]
        [Alias("announcements-channel", "set-announce-channel", "set-channel")]
        [Summary("Sets the channel where all timer notifications will be sent.")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetAnnoucementsChannelAsync(SocketTextChannel channel = null)
        {
            if (channel == null)
            {
                Config.AnnouncementsChannelId = default;
                await new SuccessMessage("Timer notifications will be sent in the same channel they were requested.")
                    .SendAsync(Context.Channel);
            }
            else
            {
                Config.AnnouncementsChannelId = channel.Id;
                await new SuccessMessage($"All timer notifications will be sent in {channel.Mention}")
                    .SendAsync(Context.Channel);
            }
        }
    }
}