using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Verbose;
using wow2.Data;

namespace wow2.Modules.Dev
{
    [Name("Developer")]
    [Group("dev")]
    [RequireOwner(Group = "Permission")]
    [Summary("For developer things.")]
    public class DevModule : ModuleBase<SocketCommandContext>
    {
        private readonly Dictionary<string, string[]> AllCommandLists = new Dictionary<string, string[]>()
        {
            {
                "keywords", new string[]
                {
                    "add testing-keyword1 value",
                    "add testing-keyword1 another value",
                    "rename testing-keyword1 testing-keyword2",
                    "values testing-keyword2",
                    "remove testing-keyword2 value",
                    "remove testing-keyword2 another value"
                }
            },
            {
                "vc", new string[]
                {
                    "join",
                    "add https://www.youtube.com/watch?v=jNQXAC9IVRw",
                    "add 'me at the zoo'",
                    "list",
                    "np",
                    "clear",
                    "add https://www.google.com",
                    "leave"
                }
            },
            {
                "images", new string[]
                {
                    "quote \"Hello world\"",
                    "quote \"Hello world\" einstein"
                }
            }
        };

        [Command("load-guild-data")]
        public async Task LoadGuildDataAsync()
        {
            await DataManager.LoadGuildDataFromFileAsync();
            await GenericMessenger.SendSuccessAsync(Context.Channel, $"`{DataManager.DictionaryOfGuildData.Count}` guilds has their data loaded.");
        }

        [Command("save-guild-data")]
        public async Task SaveGuildDataAsync(bool alsoExit = false)
        {
            await DataManager.SaveGuildDataToFileAsync();
            await GenericMessenger.SendSuccessAsync(Context.Channel, $"`{DataManager.DictionaryOfGuildData.Count}` guilds has their data saved.");
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
            await GenericMessenger.SendSuccessAsync(Context.Channel, $"This is a success message.", "Success");
            await GenericMessenger.SendInfoAsync(Context.Channel, $"This is an info message.", "Info");
            await GenericMessenger.SendWarningAsync(Context.Channel, $"This is a warning message.", "Warning");
            await GenericMessenger.SendErrorAsync(Context.Channel, $"This is an error message.", "Error");
            await GenericMessenger.SendResponseAsync(Context.Channel, $"This is a response message.", "Response");
            await GenericMessenger.SendResponseAsync(Context.Channel, $"This is a response message with fields.", "Fields", CreateSampleFields(50), 2);
        }

        [Command("run-test")]
        [Alias("test")]
        public async Task TestAsync(string group = null, int delay = 2000)
        {
            if (group == null)
            {
                foreach (var commandList in AllCommandLists)
                {
                    await RunListOfCommandsAsync(commandList.Key, commandList.Value, delay);
                }
            }
            else
            {
                if (!AllCommandLists.ContainsKey(group))
                    throw new CommandReturnException(Context, "No such module.");

                await RunListOfCommandsAsync(group, AllCommandLists[group], delay);
            }
        }

        [Command("throw")]
        public Task Throw()
            => throw new Exception("This is a test exception.");

        private async Task RunListOfCommandsAsync(string group, string[] commandList, int delay)
        {
            await GenericMessenger.SendInfoAsync(Context.Channel, $"About to run {commandList.Length} commands with a delay of {delay}ms");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (string command in commandList)
            {
                await Task.Delay(delay);
                string fullCommand = $"{group} {command}";
                await ReplyAsync($"⏩ {fullCommand}");
                await EventHandlers.ExecuteCommandAsync(Context, fullCommand);
            }

            stopwatch.Stop();
            await GenericMessenger.SendSuccessAsync(Context.Channel, $"Finished executing commands in `{stopwatch.Elapsed}`");
        }

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