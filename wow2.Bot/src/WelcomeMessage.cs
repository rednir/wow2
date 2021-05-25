using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot
{
    public class WelcomeMessage : Message
    {
        public WelcomeMessage(string commandPrefix)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Title = "👋 Hi there!",
                Description = $"Thanks for adding me to your server!\nTo get started, type `{commandPrefix} help` to see the wide range of commands available.\n",
                Color = Color.Gold,
            };
        }

        public async Task<IUserMessage> SendToBestChannelAsync(SocketGuild guild)
        {
            foreach (SocketTextChannel channel in guild.TextChannels)
            {
                try
                {
                    return await SendAsync(channel);
                }
                catch (HttpException)
                {
                    // Most likely the bot does not have sufficient privileges.
                }
            }

            // The bot does not have sufficient permissions.
            return null;
        }
    }
}