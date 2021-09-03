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
        public const string ConfirmText = "Yeah!";
        public const string DenyText = "Nah.";

        public QuestionMessage(string description, string title = null, Func<Task> onConfirm = null, Func<Task> onDeny = null)
        {
            OnConfirm = onConfirm ?? (() => Task.CompletedTask);
            OnDeny = onDeny ?? (() => Task.CompletedTask);
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowinfo:{QuestionEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = new Color(0x9b59b6),
            };

            Components = new ComponentBuilder()
                .WithButton(ConfirmText, ConfirmText, ButtonStyle.Primary)
                .WithButton(DenyText, DenyText, ButtonStyle.Secondary);
        }

        public Func<Task> OnConfirm { get; }

        public Func<Task> OnDeny { get; }

        public static async Task<bool> ActOnButtonAsync(SocketMessageComponent component)
        {
            GuildData guildData = DataManager.AllGuildData[component.Channel.GetGuild().Id];
            QuestionMessage message = FromMessageId(guildData, component.Message.Id);

            if (message == null)
                return false;

            if (component.Data.CustomId == ConfirmText)
            {
                await message.OnConfirm.Invoke();
            }
            else if (component.Data.CustomId == DenyText)
            {
                await message.OnDeny.Invoke();
            }
            else
            {
                return false;
            }

            guildData.QuestionMessages.Remove(message);
            await message.SentMessage.ModifyAsync(m => m.Components = null);
            return true;
        }

        /// <summary>Finds the <see cref="QuestionMessage"/> with the matching message ID.</summary>
        /// <returns>The <see cref="QuestionMessage"/> respresenting the message ID, or null if a match was not found.</returns>
        public static QuestionMessage FromMessageId(GuildData guildData, ulong messageId) =>
            guildData.QuestionMessages.Find(m => m.SentMessage.Id == messageId);

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            IUserMessage message = await base.SendAsync(channel);
            List<QuestionMessage> confirmMessages = DataManager.AllGuildData[message.GetGuild().Id].QuestionMessages;

            confirmMessages.Truncate(30);
            confirmMessages.Add(this);

            return message;
        }
    }
}