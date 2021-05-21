using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using wow2.Modules.Main;
using wow2.Verbose;
using wow2.Verbose.Messages;
using wow2.Extentions;

namespace wow2
{
    public class WelcomeMessage : Message
    {
        private readonly SocketGuild Guild;

        public WelcomeMessage(SocketGuild guild)
        {
            Guild = guild;
            EmbedBuilder = new EmbedBuilder()
            {
                Title = "👋 Hi there!",
                Description = $"Thanks for adding me to your server!\nTo get started, type `{guild.GetCommandPrefix()} help` to see the wide range of commands available.\n",
                Color = Color.Gold,
            };
        }

        public async Task<IUserMessage> SendToBestChannelAsync()
        {
            foreach (SocketTextChannel channel in Guild.TextChannels)
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

            Logger.Log($"Couldn't send welcome message to {Guild.Name} ({Guild.Id})", LogSeverity.Warning);
            return null;
        }
    }
}