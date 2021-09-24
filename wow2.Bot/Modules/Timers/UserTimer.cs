using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Verbose;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Timers
{
    public class UserTimer : IDisposable
    {
        public UserTimer()
        {
        }

        public UserTimer(SocketCommandContext context, TimeSpan timeSpan, string message, TimeSpan? repeatEvery)
        {
            if (timeSpan <= TimeSpan.Zero)
                throw new ArgumentException("Time span is too small (less than zero)");

            MessageString = message;
            RepeatEvery = repeatEvery;
            UserMessageId = context.Message.Id;
            GuildId = context.Guild.Id;
            ChannelId = context.Channel.Id;
            TargetDateTime = DateTime.Now + timeSpan;

            var config = DataManager.AllGuildData[context.Guild.Id].Timers;
            config.UserTimers.Add(this);
        }

        private Timer Timer { get; set; }

        public TimeSpan? RepeatEvery { get; }

        public string MessageString { get; set; }

        public ulong UserMessageId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong GuildId { get; set; }

        public DateTime TargetDateTime { get; set; }

        public List<string> NotifyUserMentions { get; set; } = new();

        public void Start()
        {
            if (Timer != null)
                throw new InvalidOperationException("Timer already started");

            Timer = new(DateTime.Now >= TargetDateTime ? 500 : (TargetDateTime - DateTime.Now).TotalMilliseconds);
            Timer.AutoReset = false;
            Timer.Elapsed += async (source, e) => await OnElapsedAsync();
            Timer.Start();
        }

        /// <summary>Disposes of the timer and removes it from the guild's config.</summary>
        public void Dispose()
        {
            Timer.Dispose();
            DataManager.AllGuildData[GuildId].Timers.UserTimers.Remove(this);
            GC.SuppressFinalize(this);
        }

        private async Task OnElapsedAsync()
        {
            Dispose();
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