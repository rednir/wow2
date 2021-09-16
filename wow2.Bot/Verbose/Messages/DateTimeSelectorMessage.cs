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
    public class DateTimeSelectorMessage : Message
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

        public DateTimeSelectorMessage(Func<DateTime, Task> confirmFunc, string description = "Select a date and time.")
        {
            Description = description;
            ConfirmFunc = confirmFunc;

            Components = new ComponentBuilder().WithButton(ConfirmString, ConfirmString, ButtonStyle.Primary, row: 2);
            foreach (string text in DateTimeModifierEmotes.Keys)
                Components.WithButton(text, text, ButtonStyle.Secondary);
        }

        public DateTime DateTime { get; set; } = DateTime.Now;

        private string Description { get; }

        private Func<DateTime, Task> ConfirmFunc { get; }

        public static async Task<bool> ActOnButtonAsync(SocketMessageComponent component)
        {
            GuildData guildData = DataManager.AllGuildData[component.Channel.GetGuild().Id];
            DateTimeSelectorMessage message = guildData.DateTimeSelectorMessages.Find(m => m.SentMessage.Id == component.Message.Id);

            if (message == null)
                return false;

            if (component.Data.CustomId == ConfirmString)
            {
                guildData.DateTimeSelectorMessages.Remove(message);
                message.SentMessage?.ModifyAsync(m => m.Components = null);
                await message.ConfirmFunc?.Invoke(message.DateTime);
                return true;
            }
            else if (DateTimeModifierEmotes.Any(p => p.Key == component.Data.CustomId))
            {
                message.DateTime += DateTimeModifierEmotes[component.Data.CustomId];
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
            List<DateTimeSelectorMessage> dateTimeSelectorMessages = DataManager
                .AllGuildData[message.GetGuild().Id].DateTimeSelectorMessages;

            dateTimeSelectorMessages.Truncate(40);
            dateTimeSelectorMessages.Add(this);

            return message;
        }

        private async Task UpdateEmbedAsync()
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowquestion:{QuestionEmoteId}>")} {Description}\n`{DateTime.ToLongDateString()} / {DateTime.ToShortTimeString()}`",
                Color = new Color(0x9b59b6),
            };

            if (SentMessage != null)
                await SentMessage.ModifyAsync(m => m.Embed = Embed);
        }
    }
}