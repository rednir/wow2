using System;
using System.Linq;
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
        private const string ReleaseVersion = "1.0";
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

        public static async Task<IGuildUser> GetClientGuildUserAsync(SocketCommandContext context)
            => (IGuildUser)(await context.Channel.GetUserAsync(context.Client.CurrentUser.Id));

        private static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            SetIsDebugField();
            Logger.LogProgramDetails();

            await DataManager.InitializeAsync();
            await EventHandlers.InstallCommandsAsync();

            Client = new DiscordSocketClient();
            await GetTokenAndLoginAsync(Client);
            await Client.StartAsync();

            Client.Ready += EventHandlers.ReadyAsync;
            Client.Log += EventHandlers.DiscordLogRecievedAsync;
            Client.ReactionAdded += EventHandlers.ReactionAddedAsync;
            Client.MessageReceived += EventHandlers.MessageRecievedAsync;

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
                    Logger.Log($"A valid bot token was not found, and must be entered in manually.", LogSeverity.Warning);
                    Console.WriteLine($"\nIt is recommended to put your bot token in an environment variable called {DiscordTokenEnvironmentVariable}. Alternatively, you can put your bot token in a file named {DiscordTokenFilePath}, and place it in the working directory of this executable.\n");
                    await PromptUserForToken(client);
                }
            }
        }

        private async Task PromptUserForToken(DiscordSocketClient client)
        {
            while (client.LoginState == LoginState.LoggedOut)
            {
                Console.Write("TOKEN: ");
                try
                {
                    string token = Console.ReadLine();
                    await client.LoginAsync(TokenType.Bot, token);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message, LogSeverity.Error);
                }
                finally
                {
                    Console.WriteLine();
                }
            }
        }
    }
}
