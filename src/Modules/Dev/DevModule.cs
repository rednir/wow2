using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Verbose.Messages;
using wow2.Verbose;
using wow2.Data;
using wow2.Extentions;

namespace wow2.Modules.Dev
{
    [Name("Developer")]
    [Group("dev")]
    [RequireOwner(Group = "Permission")]
    [Summary("For developer things.")]
    public class DevModule : ModuleBase<SocketCommandContext>
    {
        [Command("load-guild-data")]
        [Alias("load")]
        [Summary("Loads guild data from file to memory, discarding any unsaved changes.")]
        public async Task LoadGuildDataAsync()
        {
            await DataManager.LoadGuildDataFromFileAsync();
            await new SuccessMessage($"`{DataManager.DictionaryOfGuildData.Count}` guilds has their data loaded.")
                .SendAsync(Context.Channel);
        }

        [Command("save-guild-data")]
        [Alias("save")]
        [Summary("Save guild data from memory to file, optionally stopping the bot.")]
        public async Task SaveGuildDataAsync(bool alsoExit = false)
        {
            await DataManager.SaveGuildDataToFileAsync();
            await new SuccessMessage($"`{DataManager.DictionaryOfGuildData.Count}` guilds has their data saved.")
                .SendAsync(Context.Channel);
            if (alsoExit) Environment.Exit(0);
        }

        [Command("set-status")]
        [Summary("Sets the 'playing' text and the status of the bot user.")]
        public async Task SetStatus(string message, UserStatus status)
        {
            await Bot.Client.SetGameAsync(message);
            await Bot.Client.SetStatusAsync(status);
            await new SuccessMessage("Set status.")
                .SendAsync(Context.Channel);
        }

        [Command("run-test")]
        [Alias("test")]
        [Summary("Runs a list of commands.")]
        public async Task TestAsync(string group = null)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                string result = "";
                foreach (string testName in Tests.TestList.Keys)
                    result += $"`{testName}`\n";

                await new InfoMessage(result, "List of tests")
                    .SendAsync(Context.Channel);
                return;
            }

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

        [Command("commands-list")]
        [Alias("commands")]
        [Summary("Creates a COMMANDS.md file with a list of all commands.")]
        public async Task CommandsListAsync()
        {
            var commandsGroupedByModule = Bot.CommandService.Commands
                .GroupBy(c => c.Module);

            var stringBuilder = new StringBuilder("# List of commands\n\n");
            foreach (var module in commandsGroupedByModule)
            {
                stringBuilder.AppendLine($"## {module.Key.Name}");
                foreach (var command in module)
                {
                    string summary = command.Summary == null ? null : $"\n     - {command.Summary}";
                    stringBuilder.AppendLine(
                        $" - {command.MakeFullCommandString("!wow")}{summary}\n");
                }
            }

            await Context.Channel.SendFileAsync(
                stringBuilder.ToString().ToMemoryStream(), "COMMANDS.md");
        }

        [Command("get-logs")]
        [Alias("logs", "log")]
        [Summary("Sends the log file for this session.")]
        public async Task GetLogsAsync()
        {
            string logs = await Logger.GetLogsForSessionAsync();
            await Context.Channel.SendFileAsync(logs.ToMemoryStream(), "wow2.log");
        }

        [Command("throw")]
        [Summary("Throws an unhandled exception.")]
        public Task Throw()
            => throw new Exception("This is a test exception.");
    }
}