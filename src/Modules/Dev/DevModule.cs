using System;
using System.Diagnostics;
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
            await new InfoMessage($"About to run {commandList.Length} commands with a delay of {delay}ms")
                .SendAsync(Context.Channel);
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
            await new SuccessMessage($"Finished executing commands in `{stopwatch.Elapsed}`")
                .SendAsync(Context.Channel);
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