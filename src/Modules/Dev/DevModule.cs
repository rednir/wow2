using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
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
    [Summary("Boring stuff for developers.")]
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
            var testList = Tests.GetTestList();

            if (string.IsNullOrWhiteSpace(group))
            {
                string result = "";
                foreach (string name in testList.Keys)
                    result += $"`{name}`\n";

                await new GenericMessage(result, "List of tests")
                    .SendAsync(Context.Channel);
                return;
            }

            try
            {
                await testList[group](Context);
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

            var stringBuilder = new StringBuilder($"# List of commands ({Bot.CommandService.Commands.Count()} total)\n\n");
            foreach (var module in commandsGroupedByModule)
            {
                stringBuilder.AppendLine(
                    $"## {module.Key.Name}\n{module.First().Module.Summary}\n");

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

        [Command("shell-execute")]
        [Alias("shell", "execute")]
        [Summary("Executes a command on the host machine.")]
        public async Task ShellExecuteAsync([Remainder] string command)
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string standardOutput = "";
            string standardError = "";
            using (var process = new Process())
            {
                process.StartInfo.FileName = isWindows ? "cmd" : "bash";
                process.StartInfo.Arguments = $"{(isWindows ? "/c" : "-c")} \"{command}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.OutputDataReceived += (_, outline) => standardOutput += outline.Data + "\n";
                process.ErrorDataReceived += (_, outline) => standardError += outline.Data + "\n";

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                await process.WaitForExitAsync();
            }
            await ReplyAsync($"```STDOUT:\n{standardOutput}```\n```STDERR:\n{standardError}```");
        }

        [Command("throw")]
        [Summary("Throws an unhandled exception.")]
        public Task Throw()
            => throw new Exception("This is a test exception.");
    }
}