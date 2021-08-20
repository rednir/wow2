using System;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Modules.Keywords;
using wow2.Bot.Verbose;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Dev
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
            await new SuccessMessage($"`{DataManager.AllGuildData.Count}` guilds has their data loaded.")
                .SendAsync(Context.Channel);
        }

        [Command("save-guild-data")]
        [Alias("save")]
        [Summary("Save guild data from memory to file, optionally stopping the bot.")]
        public async Task SaveGuildDataAsync()
        {
            await DataManager.SaveGuildDataToFileAsync();
            await new SuccessMessage($"`{DataManager.AllGuildData.Count}` guilds has their data saved.")
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
            string logs = await Logger.GetLogsForSessionAsync();
            await Context.Channel.SendFileAsync(logs.ToMemoryStream(), "wow2.log");
        }

        [Command("panic")]
        [Summary("Disables the bot for non-owner and changes the bot's Discord status.")]
        public async Task PanicAsync()
        {
            await BotService.Client.SetGameAsync("*under maintenance*");
            await BotService.Client.SetStatusAsync(UserStatus.DoNotDisturb);
            BotService.IsDisabled = true;

            await new SuccessMessage("Done.")
                .SendAsync(Context.Channel);
        }

        [Command("unpanic")]
        [Summary("Enables the bot for all.")]
        public async Task UnpanicAsync()
        {
            await BotService.Client.SetGameAsync("!wow help");
            await BotService.Client.SetStatusAsync(UserStatus.Online);
            BotService.IsDisabled = false;

            await new SuccessMessage("Done.")
                .SendAsync(Context.Channel);
        }

        [Command("poll-start")]
        [Summary("Enables the bot for all.")]
        public async Task PollStartAsync(string name)
        {
            Timer timer = PollingService.PollingServiceTimers[name];
            timer.Start();

            await new SuccessMessage($"Started `{name}` with interval {timer.Interval / 60000}min.")
                .SendAsync(Context.Channel);
        }

        [Command("poll-stop")]
        [Summary("Stops a polling service.")]
        public async Task PollStopAsync(string name)
        {
            PollingService.PollingServiceTimers[name].Stop();

            await new SuccessMessage("Stopped.")
                .SendAsync(Context.Channel);
        }

        [Command("stop-program")]
        [Summary("Stops the program.")]
        public async Task StopProgramAsync()
        {
            await SaveGuildDataAsync();

            await BotService.Client.SetGameAsync("RESTARTING...");
            await BotService.Client.SetStatusAsync(UserStatus.DoNotDisturb);

            foreach (GuildData guildData in DataManager.AllGuildData.Values)
            {
                foreach (PagedMessage message in guildData.PagedMessages)
                {
                    try
                    {
                        await message.SentMessage.RemoveAllReactionsAsync();
                    }
                    catch
                    {
                    }
                }

                foreach (ResponseMessage message in guildData.Keywords.ListOfResponseMessages)
                {
                    try
                    {
                        await message.SentMessage.RemoveAllReactionsAsync();
                    }
                    catch
                    {
                    }
                }
            }

            await new SuccessMessage("Done.")
                .SendAsync(Context.Channel);

            Environment.Exit(0);
        }

        [Command("throw")]
        [Summary("Throws an unhandled exception.")]
        public Task Throw()
            => throw new Exception("This is a test exception.");
    }
}