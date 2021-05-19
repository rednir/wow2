using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using wow2.Data;
using wow2.Extentions;
using wow2.Verbose;
using wow2.Verbose.Messages;

namespace wow2.Modules.Dev
{
    [Name("Developer")]
    [Group("dev")]
    [RequireOwner(Group = "Permission")]
    [Summary("Boring stuff for developers.")]
    public class DevModule : Module
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
            if (alsoExit)
                Environment.Exit(0);
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

        [Command("timer")]
        [Alias("countdown", "count", "time")]
        [Summary("Start a timer that will send a message when elapsed.")]
        public async Task TimerAsync(string time)
        {
            if (time.TryConvertToTimeSpan(out TimeSpan timeSpan))
                throw new CommandReturnException(Context, "Try something like `5m` or `30s`", "Invalid time.");
            if (timeSpan > TimeSpan.FromDays(30) || timeSpan < TimeSpan.FromSeconds(1))
                throw new CommandReturnException(Context, "Be sensible.");

            var timer = new Timer(timeSpan.TotalMilliseconds);
            timer.Elapsed += async (source, e) =>
            {
                timer.Dispose();
                await new SuccessMessage("Time up!")
                {
                    ReplyToMessageId = Context.Message.Id,
                    AllowMentions = true,
                }
                .SendAsync(Context.Channel);
            };

            timer.Start();
            await new InfoMessage("Timer started.")
                .SendAsync(Context.Channel);
        }

        [Command("throw")]
        [Summary("Throws an unhandled exception.")]
        public Task Throw()
            => throw new Exception("This is a test exception.");
    }
}