using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;

namespace wow2.Bot.Verbose.Messages
{
    public class QuestionMessage : Message
    {
        public static readonly IEmote ConfirmEmote = new Emoji("👌");
        public static readonly IEmote DenyEmote = new Emoji("❌");

        public QuestionMessage(string description, string title = null, Func<Task> onConfirm = null, Func<Task> onDeny = null)
        {
            OnConfirm = onConfirm ?? (() => Task.CompletedTask);
            OnDeny = onDeny ?? (() => Task.CompletedTask);
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowinfo:{QuestionEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = new Color(0x9b59b6),
            };
        }

        public Func<Task> OnConfirm { get; }

        public Func<Task> OnDeny { get; }

        public static async Task<bool> ActOnReactionAsync(SocketReaction reaction)
        {
            GuildData guildData = DataManager.AllGuildData[reaction.Channel.GetGuild().Id];
            QuestionMessage message = FromMessageId(guildData, reaction.MessageId);

            if (message == null)
                return false;

            if (reaction.Emote.Name == ConfirmEmote.Name)
            {
                await message.OnConfirm.Invoke();
            }
            else if (reaction.Emote.Name == DenyEmote.Name)
            {
                await message.OnDeny.Invoke();
            }
            else
            {
                return false;
            }

            guildData.ConfirmMessages.Remove(message);
            await message.SentMessage.RemoveAllReactionsAsync();

            return true;
        }

        /// <summary>Finds the <see cref="QuestionMessage"/> with the matching message ID.</summary>
        /// <returns>The <see cref="QuestionMessage"/> respresenting the message ID, or null if a match was not found.</returns>
        public static QuestionMessage FromMessageId(GuildData guildData, ulong messageId) =>
            guildData.ConfirmMessages.Find(m => m.SentMessage.Id == messageId);

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            IUserMessage message = await base.SendAsync(channel);
            List<QuestionMessage> confirmMessages = DataManager.AllGuildData[message.GetGuild().Id].ConfirmMessages;

            confirmMessages.Truncate(30);
            confirmMessages.Add(this);

            await message.AddReactionsAsync(
                new IEmote[] { ConfirmEmote, DenyEmote });

            return message;
        }
    }
}