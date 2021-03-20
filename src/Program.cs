using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace wow2
{
    public class Program
    {
        private const string discordTokenFilePath = "discord.token";
        private const string discordTokenEnvironmentVariable = "DISCORD_BOT_TOKEN";

        public static DiscordSocketClient Client;

        private static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            Client = new DiscordSocketClient();

            await GetTokenAndLoginAsync(Client);

            await DataManager.InitializeAsync();
            await EventHandlers.InstallCommandsAsync();

            await Client.StartAsync();

            Client.Ready += EventHandlers.ReadyAsync;
            Client.Log += EventHandlers.LogAsync;
            Client.ReactionAdded += EventHandlers.ReactionAddedAsync;
            Client.MessageReceived += EventHandlers.MessageRecievedAsync;

            await Task.Delay(-1);
        }

        private async Task GetTokenAndLoginAsync(DiscordSocketClient client)
        {
            string token = null;
            try
            {
                token = await File.ReadAllTextAsync(discordTokenFilePath);
                await client.LoginAsync(TokenType.Bot, token);
                Console.WriteLine($"Logged in with token found in {discordTokenFilePath}");
            }
            catch
            {
                try
                {
                    token = Environment.GetEnvironmentVariable(discordTokenEnvironmentVariable);
                    await client.LoginAsync(TokenType.Bot, token);
                    Console.WriteLine($"Logged in with token found in environment variable {discordTokenEnvironmentVariable}");
                }
                catch
                {
                    Console.WriteLine($"A valid bot token was not found. You can enter it in manually below.\nIt is recommended to put your bot token in an environment variable called {discordTokenEnvironmentVariable}. Alternatively, you can put your bot token in a file named {discordTokenFilePath}, and place it in the working directory of this executable.\n\n");
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
                    Console.WriteLine($"An exception was thrown: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine();
                }
            }
        }
    }
}
