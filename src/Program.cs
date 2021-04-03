using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Verbose;
using wow2.Data;

namespace wow2
{
    public class Program
    {
        private const string ReleaseVersion = "v1.0";
        private const string DiscordTokenFilePath = "discord.token";
        private const string DiscordTokenEnvironmentVariable = "DISCORD_BOT_TOKEN";

        private static bool IsDebugField;

        [System.Diagnostics.Conditional("DEBUG")]
        private static void SetIsDebugField()
            => IsDebugField = true;

        public static readonly DateTime TimeStarted = DateTime.Now;
        public static DiscordSocketClient Client { get; set; }
        public static bool IsDebug
        {
            get { return IsDebugField; }
        }
        public static string Version
        {
            get { return IsDebug ? "DEBUG BUILD" : ReleaseVersion; }
        }
        public static string RuntimeDirectory
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        public static async Task<SocketGuildUser> GetClientGuildUserAsync(ISocketMessageChannel channel)
            => (SocketGuildUser)(await channel.GetUserAsync(Program.Client.CurrentUser.Id));

        private static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            SetIsDebugField();

            await Logger.LogInitialize();
            await DataManager.InitializeAsync();
            await EventHandlers.InstallCommandsAsync();

            Client = new DiscordSocketClient();

            Client.Ready += EventHandlers.ReadyAsync;
            Client.Log += EventHandlers.DiscordLogRecievedAsync;
            Client.ReactionAdded += EventHandlers.ReactionAddedAsync;
            Client.MessageReceived += EventHandlers.MessageRecievedAsync;
            Client.JoinedGuild += EventHandlers.JoinedGuildAsync;

            await GetTokenAndLoginAsync(Client);
            await Client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task GetTokenAndLoginAsync(DiscordSocketClient client)
        {
            string token = null;
            try
            {
                token = (await File.ReadAllLinesAsync(DiscordTokenFilePath)).First();
                await client.LoginAsync(TokenType.Bot, token);
                Logger.Log($"Logged in with token found in {DiscordTokenFilePath}", LogSeverity.Info);
            }
            catch
            {
                try
                {
                    token = Environment.GetEnvironmentVariable(DiscordTokenEnvironmentVariable);
                    await client.LoginAsync(TokenType.Bot, token);
                    Logger.Log($"Logged in with token found in environment variable {DiscordTokenEnvironmentVariable}", LogSeverity.Info);
                }
                catch
                {
                    Logger.Log($"A valid bot token was not found. It is recommended to put your bot token in an environment variable called {DiscordTokenEnvironmentVariable}. Alternatively, you can put your bot token in a file named {DiscordTokenFilePath}, and place it in the working directory of this executable.", LogSeverity.Critical);
                    Console.Read();
                    Environment.Exit(-1);
                }
            }
        }
    }
}
