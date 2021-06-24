using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Extensions;

namespace wow2.Bot.Verbose.Messages
{
    public class ConfirmMessage : Message
    {
        public static readonly IEmote ConfirmEmote = new Emoji("👌");
        public static readonly IEmote DenyEmote = new Emoji("❌");

        public ConfirmMessage(string description, string title = null, Action<Task<SocketCommandContext>> onConfirm = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = GetStatusMessageFormattedDescription(description, title),
                Color = Color.Magenta,
            };
        }

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            IUserMessage message = await base.SendAsync(channel);
            List<ConfirmMessage> confirmMessages = DataManager.AllGuildData[message.GetGuild().Id].ConfirmMessages;

            confirmMessages.Truncate(30);
            confirmMessages.Add(this);

            await message.AddReactionsAsync(
                new IEmote[] { ConfirmEmote, DenyEmote });

            return message;
        }
    }
}