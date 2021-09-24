using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Verbose;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Timers
{
    public class UserTimer
    {
        public UserTimer()
        {
        }

        public UserTimer(SocketCommandContext context, TimeSpan timeSpan, string message, TimeSpan? repeatEvery)
        {
            if (timeSpan <= TimeSpan.Zero)
                throw new ArgumentException("Time span is too small (less than zero)");

            MessageString = message;
            RepeatEverySeconds = repeatEvery?.TotalSeconds;
            UserMessageId = context.Message.Id;
            GuildId = context.Guild.Id;
            ChannelId = context.Channel.Id;
            TargetDateTime = DateTime.Now + timeSpan;
        }

        private Timer Timer { get; set; }

        // This is required as TimeSpans cannot be deserialized from json.
        public double? RepeatEverySeconds { get; set; }

        [JsonIgnore]
        public TimeSpan? RepeatEvery => RepeatEverySeconds == null ? null : TimeSpan.FromSeconds(RepeatEverySeconds.Value);

        public string MessageString { get; set; }

        public string UserMessageUrl => $"https://cdn.discordapp.com/channels/{GuildId}/{ChannelId}/{UserMessageId}";

        public ulong UserMessageId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong GuildId { get; set; }

        public DateTime TargetDateTime { get; set; }

        public List<string> NotifyUserMentions { get; set; } = new();

        public bool IsDeleted => !DataManager.AllGuildData[GuildId].Timers.UserTimers.Contains(this);

        /// <summary>Creates the timer and adds it to the guild's config.</summary>
        public void Start()
        {
            Timer = new(DateTime.Now >= TargetDateTime ? 500 : (TargetDateTime - DateTime.Now).TotalMilliseconds);
            Timer.AutoReset = false;
            Timer.Elapsed += async (source, e) => await OnElapsedAsync();
            Timer.Start();

            var timers = DataManager.AllGuildData[GuildId].Timers.UserTimers;
            if (!timers.Contains(this))
                timers.Add(this);
        }

        /// <summary>Disposes the timer and removes it from the guild's config.</summary>
        public void Stop()
        {
            Timer.Dispose();
            DataManager.AllGuildData[GuildId].Timers.UserTimers.Remove(this);
        }

        private async Task OnElapsedAsync()
        {
            if (RepeatEvery == null)
            {
                Stop();
            }
            else
            {
                TargetDateTime = DateTime.Now + RepeatEvery.Value;
                Start();
            }

            try
            {
                var channel = (IMessageChannel)BotService.Client.GetChannel(ChannelId);
                await new InfoMessage(MessageString, "Time up!")
                {
                    ReplyToMessageId = UserMessageId,
                    AllowMentions = true,
                }
                .SendAsync(channel);

                if (NotifyUserMentions.Count > 0)
                    await channel.SendMessageAsync(string.Join(' ', NotifyUserMentions));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Could not send time up message for user timer in guild {GuildId}");
            }
        }
    }
}