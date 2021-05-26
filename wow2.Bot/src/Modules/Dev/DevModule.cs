using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Dev
{
    [Name("Developer")]
    [Group("dev")]
    [RequireOwner(Group = "Permission")]
    [Summary("Boring stuff for developers.")]
    public class DevModule : Module
    {
        protected DevModule(BotService botService)
            : base(botService)
        {
        }

        [Command("load-guild-data")]
        [Alias("load")]
        [Summary("Loads guild data from file to memory, discarding any unsaved changes.")]
        public async Task LoadGuildDataAsync()
        {
            await BotService.Data.LoadGuildDataFromFileAsync();
            await new SuccessMessage($"`{BotService.Data.AllGuildData.Count}` guilds has their data loaded.")
                .SendAsync(Context.Channel);
        }

        [Command("save-guild-data")]
        [Alias("save")]
        [Summary("Save guild data from memory to file, optionally stopping the bot.")]
        public async Task SaveGuildDataAsync()
        {
            await BotService.Data.SaveGuildDataToFileAsync();
            await new SuccessMessage($"`{BotService.Data.AllGuildData.Count}` guilds has their data saved.")
                .SendAsync(Context.Channel);
        }

        [Command("set-status")]
        [Summary("Sets the 'playing' text and the status of the bot user.")]
        public async Task SetStatus(string message, UserStatus status)
        {
            await BotService.Client.SetGameAsync(message);
            await BotService.Client.SetStatusAsync(status);
            await new SuccessMessage("Set status.")
                .SendAsync(Context.Channel);
        }

        [Command("run-test")]
        [Alias("test")]
        [Summary("Runs a list of commands.")]
        public async Task TestAsync(params string[] groups)
        {
            if (groups.Length == 0)
            {
                string result = string.Empty;
                foreach (string name in Tests.TestList.Keys)
                    result += $"`{name}`\n";

                await new GenericMessage(result, "List of tests")
                    .SendAsync(Context.Channel);
                return;
            }

            foreach (string group in groups)
            {
                try
                {
                    await Tests.TestList[group](Context);
                    await new SuccessMessage("Finished test.")
                        .SendAsync(Context.Channel);
                }
                catch (Exception ex)
                {
                    await new ErrorMessage($"```{ex}```", "Test failed due to exception.")
                        .SendAsync(Context.Channel);
                }
            }
        }

        [Command("commands-list")]
        [Alias("commands")]
        [Summary("Creates a COMMANDS.md file with a list of all commands.")]
        public async Task CommandsListAsync()
        {
            string md = BotService.MakeCommandsMarkdown();
            await Context.Channel.SendFileAsync(md.ToMemoryStream(), "COMMANDS.md");
        }

        [Command("get-logs")]
        [Alias("logs", "log")]
        [Summary("Sends the log file for this session.")]
        public async Task GetLogsAsync()
        {
            throw new NotImplementedException();
            /*string logs = await Logger.GetLogsForSessionAsync();
            await Context.Channel.SendFileAsync(logs.ToMemoryStream(), "wow2.log");*/
        }

        [Command("panic")]
        [Summary("Uninstalls all user commands and changes the bot's Discord status.")]
        public async Task PanicAsync()
        {
            await BotService.Client.SetGameAsync("under maintenance");
            await BotService.Client.SetStatusAsync(UserStatus.DoNotDisturb);
            foreach (ModuleInfo module in BotService.CommandService.Modules)
            {
                // TODO: Get attribute from this class.
                if (module.Name != "Developer")
                    await BotService.CommandService.RemoveModuleAsync(module);
            }

            await new SuccessMessage("Done.")
                .SendAsync(Context.Channel);
        }

        [Command("unpanic")]
        [Summary("Installs all commands and reconnects the bot, reloading save data from file.")]
        public async Task UnpanicAsync()
        {
            await BotService.Client.StopAsync();
            await BotService.InstallCommandsAsync();
            await BotService.InitializeAndStartClientAsync();

            await new SuccessMessage("Reconnected.")
                .SendAsync(Context.Channel);
        }

        [Command("stop-program")]
        [Summary("Stops the program.")]
        public Task StopProgramAsync()
        {
            Environment.Exit(0);
            return Task.CompletedTask;
        }

        [Command("throw")]
        [Summary("Throws an unhandled exception.")]
        public Task Throw()
            => throw new Exception("This is a test exception.");
    }
}