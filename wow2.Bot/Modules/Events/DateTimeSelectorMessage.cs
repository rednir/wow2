using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Events
{
    public class DateTimeSelectorMessage : Message
    {
        public const string ConfirmChar = "✅";

        public static readonly IReadOnlyDictionary<IEmote, TimeSpan> DateTimeModifierEmotes = new Dictionary<IEmote, TimeSpan>()
        {
            { new Emoji("⏫"), TimeSpan.FromDays(7) },
            { new Emoji("⏬"), TimeSpan.FromDays(-7) },
            { new Emoji("🔼"), TimeSpan.FromDays(1) },
            { new Emoji("🔽"), TimeSpan.FromDays(-1) },
            { new Emoji("⏩"), TimeSpan.FromHours(1) },
            { new Emoji("⏪"), TimeSpan.FromHours(-1) },
            { new Emoji("▶"), TimeSpan.FromMinutes(10) },
            { new Emoji("◀"), TimeSpan.FromMinutes(-10) },
        };

        public DateTimeSelectorMessage(Func<DateTime, Task> confirmFunc, string description = "Select a date and time.")
        {
            Description = description;
            ConfirmFunc = confirmFunc;
            _ = UpdateMessageAsync();
        }

        public DateTime DateTime { get; set; } = DateTime.Now;

        private string Description { get; }

        private Func<DateTime, Task> ConfirmFunc { get; }

        public static async Task<bool> ActOnReactionAsync(SocketReaction reaction)
        {
            EventsModuleConfig config = DataManager.AllGuildData[reaction.Channel.GetGuild().Id].Events;
            DateTimeSelectorMessage message = config.DateTimeSelectorMessages.Find(m => m.SentMessage.Id == reaction.MessageId);

            if (message == null)
                return false;

            if (reaction.Emote.Name == ConfirmChar)
            {
                config.DateTimeSelectorMessages.Remove(message);
                await message.SentMessage.RemoveAllReactionsAsync();
                await message.ConfirmFunc.Invoke(message.DateTime);
                return true;
            }
            else if (DateTimeModifierEmotes.Any(p => p.Key.Name == reaction.Emote.Name))
            {
                await message.SentMessage.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                message.DateTime += DateTimeModifierEmotes[reaction.Emote];
                await message.UpdateMessageAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            IUserMessage message = await base.SendAsync(channel);
            List<DateTimeSelectorMessage> dateTimeSelectorMessages = DataManager
                .AllGuildData[message.GetGuild().Id].Events.DateTimeSelectorMessages;

            dateTimeSelectorMessages.Truncate(12);
            dateTimeSelectorMessages.Add(this);

            await message.AddReactionsAsync(
                DateTimeModifierEmotes.Keys.Append(new Emoji(ConfirmChar)).ToArray());

            return message;
        }

        private async Task UpdateMessageAsync()
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowinfo:{QuestionEmoteId}>")} {Description}\n\nCurrently set to: `{DateTime}`",
                Color = new Color(0x9b59b6),
                Timestamp = DateTime.Now + TimeSpan.FromDays(100),
            };

            await SentMessage?.ModifyAsync(m => m.Embed = Embed);
        }
    }
}