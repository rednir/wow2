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

        [Command("raw-savedata")]
        public async Task UploadRawGuildDataAsync()
        {
            await Context.Channel.SendFileAsync(
                filePath: $"{DataManager.AppDataDirPath}/GuildData/{Context.Guild.Id}.json"
            );
        }

        [Command("generic-messenger")]
        public async Task GenericMessengerAsync()
        {
            await new SuccessMessage($"This is a success message.", "Success").SendAsync(Context.Channel);
            await new InfoMessage($"This is an info message.", "Info").SendAsync(Context.Channel);
            await new WarningMessage($"This is a warning message.", "Warning").SendAsync(Context.Channel);
            await new ErrorMessage($"This is an error message.", "Error").SendAsync(Context.Channel);
            await new GenericMessage($"This is a response message.", "Response").SendAsync(Context.Channel);
            await new GenericMessage($"This is a response message with fields.", "Fields", CreateSampleFields(50), 2).SendAsync(Context.Channel);
        }

        [Command("set-status")]
        public async Task SetStatus(string message, UserStatus status)
        {
            await Program.Client.SetGameAsync(message);
            await Program.Client.SetStatusAsync(status);
            await new SuccessMessage($"Set status.")
                .SendAsync(Context.Channel);
        }

        [Command("run-test")]
        [Alias("test")]
        public async Task TestAsync(string group = null)
        {
            try
            {
                Tests.TestList[group](Context);
                await new SuccessMessage("Finished test.")
                    .SendAsync(Context.Channel);
            }
            catch (Exception ex)
            {
                await new ErrorMessage(ex.ToString())
                    .SendAsync(Context.Channel);
            }
        }

        [Command("throw")]
        public Task Throw()
            => throw new Exception("This is a test exception.");

        private List<EmbedFieldBuilder> CreateSampleFields(int amount)
        {
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            for (int i = 0; i < amount; i++)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"Field title {i}",
                    Value = $"This is some description text."
                });
            }
            return listOfFieldBuilders;
        }
    }
}