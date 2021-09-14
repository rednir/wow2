using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;

namespace wow2.Bot.Verbose.Messages
{
    public class TimeSpanSelectorMessage : Message
    {
        public const string ConfirmString = "Confirm";

        public static readonly IReadOnlyDictionary<string, TimeSpan> DateTimeModifierEmotes = new Dictionary<string, TimeSpan>()
        {
            { "+1 week", TimeSpan.FromDays(7) },
            { "-1 week", TimeSpan.FromDays(-7) },
            { "+1 day", TimeSpan.FromDays(1) },
            { "-1 day", TimeSpan.FromDays(-1) },
            { "+1 hour", TimeSpan.FromHours(1) },
            { "-1 hour", TimeSpan.FromHours(-1) },
            { "+10 minutes", TimeSpan.FromMinutes(10) },
            { "-10 minutes", TimeSpan.FromMinutes(-10) },
        };

        public TimeSpanSelectorMessage(Func<TimeSpan, Task> confirmFunc, string description = "Select a time span.")
        {
            Description = description;
            ConfirmFunc = confirmFunc;

            Components = new ComponentBuilder().WithButton(ConfirmString, ConfirmString, ButtonStyle.Primary, row: 2);
            foreach (string text in DateTimeModifierEmotes.Keys)
                Components.WithButton(text, text, ButtonStyle.Secondary);
        }

        public TimeSpan TimeSpan { get; set; } = TimeSpan.Zero;

        private string Description { get; }

        private Func<TimeSpan, Task> ConfirmFunc { get; }

        public static async Task<bool> ActOnButtonAsync(SocketMessageComponent component)
        {
            GuildData guildData = DataManager.AllGuildData[component.Channel.GetGuild().Id];
            TimeSpanSelectorMessage message = guildData.TimeSpanSelectorMessages.Find(m => m.SentMessage.Id == component.Message.Id);

            if (message == null)
                return false;

            if (component.Data.CustomId == ConfirmString)
            {
                guildData.TimeSpanSelectorMessages.Remove(message);
                message.SentMessage?.ModifyAsync(m => m.Components = null);
                await message.ConfirmFunc?.Invoke(message.TimeSpan);
                return true;
            }
            else if (DateTimeModifierEmotes.Any(p => p.Key == component.Data.CustomId))
            {
                message.TimeSpan += DateTimeModifierEmotes[component.Data.CustomId];
                await message.UpdateEmbedAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            await UpdateEmbedAsync();

            IUserMessage message = await base.SendAsync(channel);
            var timeSpanSelectorMessages = DataManager
                .AllGuildData[message.GetGuild().Id].TimeSpanSelectorMessages;

            timeSpanSelectorMessages.Truncate(40);
            timeSpanSelectorMessages.Add(this);

            return message;
        }

        private async Task UpdateEmbedAsync()
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowquestion:{QuestionEmoteId}>")} {Description}\n`{TimeSpan.Days} days, {TimeSpan.Hours} hours, {TimeSpan.Minutes} minutes`",
                Color = new Color(0x9b59b6),
            };

            if (SentMessage != null)
                await SentMessage.ModifyAsync(m => m.Embed = Embed);
        }
    }
}