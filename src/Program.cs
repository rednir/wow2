using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using wow2.Verbose;
using wow2.Data;

namespace wow2
{
    public class Program
    {
        private const string ReleaseVersion = "v2.0";
        private static bool IsDebugField;

        [System.Diagnostics.Conditional("DEBUG")]
        private static void SetIsDebugField()
            => IsDebugField = true;

        public static readonly DateTime TimeStarted = DateTime.Now;
        public static DiscordSocketClient Client { get; set; }
        public static RestApplication ApplicationInfo { get; set; }

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
            await DataManager.LoadSecretsFromFileAsync();

            Client = new DiscordSocketClient();
            Client.Ready += EventHandlers.ReadyAsync;
            Client.Log += EventHandlers.DiscordLogRecievedAsync;
            Client.ReactionAdded += EventHandlers.ReactionAddedAsync;
            Client.MessageReceived += EventHandlers.MessageRecievedAsync;
            Client.JoinedGuild += EventHandlers.JoinedGuildAsync;
            Client.LeftGuild += EventHandlers.LeftGuildAsync;

            await Client.LoginAsync(TokenType.Bot, DataManager.Secrets.DiscordBotToken);
            await Client.StartAsync();

            ApplicationInfo = await Program.Client.GetApplicationInfoAsync();

            await Task.Delay(-1);
        }
    }
}
