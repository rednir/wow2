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
            EmbedBuilder = new EmbedBuilder()
            {
                Description = "Give me a second to add reactions...",
                Color = Color.LightGrey,
            };
            Description = description;
            ConfirmFunc = confirmFunc;
        }

        public DateTime DateTime { get; set; } = DateTime.Now;

        private string Description { get; }

        private Func<DateTime, Task> ConfirmFunc { get; }

        public static async Task<bool> ActOnReactionAsync(SocketReaction reaction)
        {
            GuildData guildData = DataManager.AllGuildData[reaction.Channel.GetGuild().Id];
            DateTimeSelectorMessage message = guildData.DateTimeSelectorMessages.Find(m => m.SentMessage.Id == reaction.MessageId);

            if (message == null)
                return false;

            if (reaction.Emote.Name == ConfirmChar)
            {
                guildData.DateTimeSelectorMessages.Remove(message);
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
                .AllGuildData[message.GetGuild().Id].DateTimeSelectorMessages;

            dateTimeSelectorMessages.Truncate(12);
            dateTimeSelectorMessages.Add(this);

            await message.AddReactionsAsync(
                DateTimeModifierEmotes.Keys.Append(new Emoji(ConfirmChar)).ToArray());

            await UpdateMessageAsync();

            return message;
        }

        private async Task UpdateMessageAsync()
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowquestion:{QuestionEmoteId}>")} {Description}\n`{DateTime}`",
                ImageUrl = "https://cdn.discordapp.com/attachments/680921268081524954/861017801065365524/datetimemessagehelp.png",
                Color = new Color(0x9b59b6),
            };

            await SentMessage?.ModifyAsync(m => m.Embed = Embed);
        }
    }
}