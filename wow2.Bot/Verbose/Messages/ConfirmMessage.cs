using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace wow2.Bot.Verbose.Messages
{
    public class ConfirmMessage : Message
    {
        public ConfirmMessage(string description, string title = null, Action<Task<SocketCommandContext>> onConfirm = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = GetStatusMessageFormattedDescription(description, title),
                Color = Color.Magenta,
            };
        }
    }
}