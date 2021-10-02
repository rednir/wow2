using System;
using System.Collections.Generic;
using System.Linq;
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

        public UserTimer(SocketCommandContext context, TimeSpan timeSpan, ulong sendToChannelId, string messageString, TimeSpan? repeatEvery)
        {
            if (timeSpan <= TimeSpan.Zero)
                throw new ArgumentException("Time span is too small (less than zero)");

            MessageString = messageString;
            RepeatEverySeconds = repeatEvery?.TotalSeconds;
            UserMessageId = context.Message.Id;
            GuildId = context.Guild.Id;
            UserMessageChannelId = context.Channel.Id;
            SendToChannelId = sendToChannelId;
            TargetDateTime = DateTime.Now + timeSpan;
        }

        private Timer Timer { get; set; }

        /// <summary>Gets or sets the timer used when the time span is too long, starting the normal timer when time span is short enough.</summary>
        private Timer RelayTimer { get; set; }

        // This is required as TimeSpans cannot be deserialized from json.
        public double? RepeatEverySeconds { get; set; }

        [JsonIgnore]
        public TimeSpan? RepeatEvery => RepeatEverySeconds == null ? null : TimeSpan.FromSeconds(RepeatEverySeconds.Value);

        public string MessageString { get; set; }

        public string UserMessageUrl => $"https://cdn.discordapp.com/channels/{GuildId}/{UserMessageChannelId}/{UserMessageId}";

        public ulong UserMessageId { get; set; }

        public ulong UserMessageChannelId { get; set; }

        public ulong GuildId { get; set; }

        public ulong SendToChannelId { get; set; }

        public DateTime TargetDateTime { get; set; }

        public List<string> NotifyUserMentions { get; set; } = new();

        public bool IsDeleted => !DataManager.AllGuildData[GuildId].Timers.UserTimers.Contains(this);

        /// <summary>Creates the timer and adds it to the guild's config.</summary>
        public void Start()
        {
            var timers = DataManager.AllGuildData[GuildId].Timers.UserTimers;
            if (!timers.Contains(this))
                timers.Add(this);

            if (TargetDateTime - DateTime.Now >= TimeSpan.FromMilliseconds(int.MaxValue))
            {
                // Run relay timer every 14 days
                RelayTimer = new Timer(1209600000);
                RelayTimer.Elapsed += (source, e) =>
                {
                    if (TargetDateTime - DateTime.Now < TimeSpan.FromMilliseconds(int.MaxValue))
                    {
                        startActual();
                        RelayTimer.Dispose();
                    }
                };
                RelayTimer.Start();
            }
            else
            {
                startActual();
            }

            void startActual()
            {
                Timer = new(DateTime.Now >= TargetDateTime ? 1000 : (TargetDateTime - DateTime.Now).TotalMilliseconds);
                Timer.AutoReset = false;
                Timer.Elapsed += async (source, e) => await OnElapsedAsync();
                Timer.Start();
            }
        }

        /// <summary>Disposes the timer and removes it from the guild's config.</summary>
        public void Stop()
        {
            RelayTimer?.Dispose();
            Timer?.Dispose();
            DataManager.AllGuildData.GetValueOrDefault(GuildId)?.Timers.UserTimers.Remove(this);
        }

        private async Task OnElapsedAsync()
        {
            if (RepeatEvery == null)
            {
                Stop();
            }
            else
            {
                TargetDateTime += RepeatEvery.Value;
                Start();
            }

            try
            {
                var channel = (IMessageChannel)BotService.Client.GetChannel(SendToChannelId);
                await new InfoMessage($"[Click here to see the original message]({UserMessageUrl})", string.IsNullOrWhiteSpace(MessageString) ? $"A timer without a name elapsed!" : MessageString)
                {
                    Text = NotifyUserMentions.Count > 0 ? string.Join(' ', NotifyUserMentions) : null,
                }
                .SendAsync(channel);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Could not send time up message for user timer in guild {GuildId}");
            }
        }
    }
}