using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace wow2
{
    public class Program
    {
        public static DiscordSocketClient Client;

        private static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            string token = File.ReadAllText("discord.token");

            await DataManager.InitializeAsync();
            await EventHandlers.InstallCommandsAsync();

            Client = new DiscordSocketClient();

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

            Client.Ready += EventHandlers.ReadyAsync;
            Client.Log += EventHandlers.LogAsync;
            Client.ReactionAdded += EventHandlers.ReactionAddedAsync;
            Client.MessageReceived += EventHandlers.MessageRecievedAsync;

            await Task.Delay(-1);
        }
    }
}
