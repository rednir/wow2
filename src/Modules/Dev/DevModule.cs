using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Verbose.Messages;
using wow2.Data;

namespace wow2.Modules.Dev
{
    [Name("Developer")]
    [Group("dev")]
    [RequireOwner(Group = "Permission")]
    [Summary("For developer things.")]
    public class DevModule : ModuleBase<SocketCommandContext>
    {
        [Command("load-guild-data")]
        public async Task LoadGuildDataAsync()
        {
            await DataManager.LoadGuildDataFromFileAsync();
            await new SuccessMessage($"`{DataManager.DictionaryOfGuildData.Count}` guilds has their data loaded.")
                .SendAsync(Context.Channel);
        }

        [Command("save-guild-data")]
        public async Task SaveGuildDataAsync(bool alsoExit = false)
        {
            await DataManager.SaveGuildDataToFileAsync();
            await new SuccessMessage($"`{DataManager.DictionaryOfGuildData.Count}` guilds has their data saved.")
                .SendAsync(Context.Channel);
            if (alsoExit) Environment.Exit(0);
        }

        [Command("set-status")]
        public async Task SetStatus(string message, UserStatus status)
        {
            await Program.Client.SetGameAsync(message);
            await Program.Client.SetStatusAsync(status);
            await new SuccessMessage("Set status.")
                .SendAsync(Context.Channel);
        }

        [Command("run-test")]
        [Alias("test")]
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
                await new ErrorMessage($"```{ex}```")
                    .SendAsync(Context.Channel);
            }
        }

        [Command("throw")]
        public Task Throw()
            => throw new Exception("This is a test exception.");
    }
}