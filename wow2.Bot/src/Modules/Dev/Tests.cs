using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Modules.Keywords;
using wow2.Bot.Modules.Main;
using wow2.Bot.Modules.Voice;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Dev
{
    public static class Tests
    {
        public static readonly Dictionary<string, Func<SocketCommandContext, Task>> TestList = new();

        /// <summary>The time in milliseconds between asserts/commands.</summary>
        private const int CommandDelay = 1000;

        static Tests()
        {
            var testMethods = typeof(Tests).GetMethods().Where(
                m => m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0);

            foreach (MethodInfo method in testMethods)
            {
                var func = (Func<SocketCommandContext, Task>)Delegate.CreateDelegate(
                    typeof(Func<SocketCommandContext, Task>), null, method);
                var attribute = (TestAttribute)method.GetCustomAttribute(typeof(TestAttribute));
                TestList.Add(attribute.Name, func);
            }
        }

        [Test("messages")]
        public static async Task MessagesTest(SocketCommandContext context)
        {
            await new SuccessMessage("This is a success message.", "Success").SendAsync(context.Channel);
            await new InfoMessage("This is an info message.", "Info").SendAsync(context.Channel);
            await new WarningMessage("This is a warning message.", "Warning").SendAsync(context.Channel);
            await new ErrorMessage("This is an error message.", "Error").SendAsync(context.Channel);
            await new GenericMessage("This is a generic message.", "Response").SendAsync(context.Channel);

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            for (int i = 0; i < 46; i++)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"Field title {i}",
                    Value = "This is some description text.",
                });
            }

            await new PagedMessage(listOfFieldBuilders, "This is a response message with fields.", "Fields", 2)
                .SendAsync(context.Channel);
        }

        [Test("aliases")]
        public static async Task AliasesTest(SocketCommandContext context)
        {
            var config = BotService.Data.AllGuildData[context.Guild.Id].Main;
            const string aliasName = "testing_alias";

            await ExecuteAsync(context,
                $"alias {aliasName} \"{config.CommandPrefix} help\"");
            await AssertAsync(context, new()
            {
                { "key exists in dictionary", config.AliasesDictionary.ContainsKey(aliasName) },
                { "check definition", config.AliasesDictionary[aliasName] == "help" },
            });

            await ExecuteAsync(context,
                $"alias {aliasName}");
            await AssertAsync(context,
                "alias has been removed", !config.AliasesDictionary.ContainsKey(aliasName));
        }

        [Test("voice")]
        public static async Task VoiceTest(SocketCommandContext context)
        {
            // TODO: These Task.Delays are a bit of a hacky workaround.
            // Find some way to reliably wait until the command finishes with timeout.
            var config = BotService.Data.AllGuildData[context.Guild.Id].Voice;

            await ExecuteAsync(context,
                "vc clear",
                "vc skip",
                "vc join");
            await DelayAsync(context, 3000);
            await AssertAsync(context, new()
            {
                { "song request queue is empty", config.CurrentSongRequestQueue.Count == 0 },
                { "nothing is playing", config.CurrentlyPlayingSongRequest == null },
                { "audio client has connected", !VoiceModule.CheckIfAudioClientDisconnected(config.AudioClient) },
            });

            await ExecuteAsync(context,
                "vc add never gonna give you up",
                "vc add https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                "vc add \"me at the zoo\"");
            await DelayAsync(context, 10000);
            await ExecuteAsync(context,
                "vc list");
            await AssertAsync(context, new()
            {
                { "currently playing is not null", config.CurrentlyPlayingSongRequest != null },
                { "song request queue is correct length", config.CurrentSongRequestQueue.Count == 2 },
            });

            var firstCurrentlyPlayingRequest = config.CurrentlyPlayingSongRequest;

            await ExecuteAsync(context,
                "vc skip");
            await AssertAsync(context, new()
            {
                { "check next request is playing", config.CurrentlyPlayingSongRequest != firstCurrentlyPlayingRequest },
            });

            await ExecuteAsync(context,
                "vc clear");
            await AssertAsync(context,
                "song request queue is empty", config.CurrentSongRequestQueue.Count == 0);

            await ExecuteAsync(context,
                "vc leave");
            await AssertAsync(context,
                "audio client has disconnected", VoiceModule.CheckIfAudioClientDisconnected(config.AudioClient));
        }

        [Test("voice-queue")]
        public static async Task VoiceQueueTest(SocketCommandContext context)
        {
            var config = BotService.Data.AllGuildData[context.Guild.Id].Voice;
            const string queueName = "testing-queue";

            await ExecuteAsync(context,
                $"vc pop-queue {queueName}",
                "vc clear");
            await AssertAsync(context, new()
            {
                { "testing queue does not exist", !config.SavedSongRequestQueues.ContainsKey(queueName) },
                { "song request queue is empty", config.CurrentSongRequestQueue.Count == 0 },
            });

            await ExecuteAsync(context,
                "vc add https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                "vc add https://www.youtube.com/watch?v=dQw4w9WgXcQ");
            await DelayAsync(context, 10000);
            await AssertAsync(context,
                "song request queue is correct length", config.CurrentSongRequestQueue.Count == 2);

            await ExecuteAsync(context,
                $"vc save {queueName}");
            await AssertAsync(context, new()
            {
                { "testing queue exists in saved queues", config.SavedSongRequestQueues.ContainsKey(queueName) },
                { "correct number of songs in testing queue", config.SavedSongRequestQueues[queueName].Count == 2 },
            });

            await ExecuteAsync(context,
                "vc clear");
            await AssertAsync(context, new()
            {
                { "song request queue is empty", config.CurrentSongRequestQueue.Count == 0 },
                { "testing queue was unmodified", config.SavedSongRequestQueues[queueName].Count == 2 },
            });

            await ExecuteAsync(context,
                $"vc pop-queue {queueName}");
            await AssertAsync(context, new()
            {
                { "testing queue does not exist", !config.SavedSongRequestQueues.ContainsKey(queueName) },
                { "song request queue is correct length", config.CurrentSongRequestQueue.Count == 2 },
            });

            await ExecuteAsync(context,
                "vc clear");
        }

        [Test("keywords")]
        public static async Task KeywordsTest(SocketCommandContext context)
        {
            var config = BotService.Data.AllGuildData[context.Guild.Id].Keywords;
            const string keywordName = "testing_keyword";

            await ExecuteAsync(context,
                $"keywords remove {keywordName}");
            await AssertAsync(context,
                "keyword doesn't exist", !config.KeywordsDictionary.ContainsKey(keywordName));

            await ExecuteAsync(context,
                $"keywords add {keywordName} value1",
                $"keywords add \"{keywordName}\" \"value2 **Title!**with title\"");
            await AssertAsync(context, new()
            {
                { "keyword exists in dictionary", config.KeywordsDictionary.TryGetValue(keywordName, out List<KeywordValue> keywordValues) },
                { "check value1", keywordValues[0].Content == "value1" },
                { "check value2", keywordValues[1].Content == "value2 with title" && keywordValues[1].Title == "Title!" },
            });

            await ExecuteAsync(context,
                $"keywords remove {keywordName} value1");
            await AssertAsync(context,
                "value was removed", keywordValues.Count == 1);

            await ExecuteAsync(context,
                "keywords remove testing_keyword");
            await AssertAsync(context,
                "keyword was removed", !config.KeywordsDictionary.ContainsKey(keywordName));
        }

        [Test("quotes")]
        public static async Task QuotesTest(SocketCommandContext context)
        {
            // TODO: also test specified author
            string repeatedText = string.Concat(
                Enumerable.Repeat("This is a quote with a lot of text.", 10));

            await ExecuteAsync(context,
                $"text quote \"{repeatedText}\"");
        }

        private static async Task<List<IResult>> ExecuteAsync(SocketCommandContext context, params string[] commands)
        {
            var results = new List<IResult>();

            foreach (string command in commands)
            {
                await context.Channel.SendMessageAsync($"`{context.Guild} {command}`");

                await Task.Delay(CommandDelay);
                results.Add(await BotService.ExecuteCommandAsync(context, command));
                await Task.Delay(CommandDelay);
            }

            return results;
        }

        private static async Task DelayAsync(SocketCommandContext context, int milliseconds)
        {
            await context.Channel.SendMessageAsync($"**⏸️ PAUSE:** {milliseconds}ms");
            await Task.Delay(milliseconds);
        }

        private static async Task AssertAsync(SocketCommandContext context, string description, bool value)
        {
            if (!value)
                throw new Exception($"Assert failure ({description})");
            await context.Channel.SendMessageAsync($"**✅ ASSERT:** {description}");
            await Task.Delay(CommandDelay);
        }

        private static async Task AssertAsync(SocketCommandContext context, Dictionary<string, bool> asserts)
        {
            foreach (var assert in asserts)
                await AssertAsync(context, assert.Key, assert.Value);
        }
    }
}