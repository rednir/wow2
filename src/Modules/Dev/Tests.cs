using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Commands;
using wow2.Modules.Main;
using wow2.Modules.Voice;
using wow2.Modules.Keywords;

namespace wow2.Modules.Dev
{
    public static class Tests
    {
        /// <summary>The time in milliseconds between asserts/commands</summary>
        private const int CommandDelay = 1000;

        public static Dictionary<string, Func<ICommandContext, Task>> TestList = new Dictionary<string, Func<ICommandContext, Task>>()
        {
            {
                "aliases", async (context) =>
                {
                    var config = MainModule.GetConfigForGuild(context.Guild);
                    const string aliasName = "testing_alias";

                    await ExecuteAsync(context,
                        $"alias {aliasName} \"{config.CommandPrefix} help\"");
                    await AssertAsync(context, new Dictionary<string, bool>()
                    {
                        {"key exists in dictionary", config.AliasesDictionary.ContainsKey(aliasName)},
                        {"check definition", config.AliasesDictionary[aliasName] == "help"}
                    });

                    await ExecuteAsync(context,
                        $"alias {aliasName}");
                    await AssertAsync(context,
                        "alias has been removed", !config.AliasesDictionary.ContainsKey(aliasName));
                }
            },
            {
                // TODO: These Task.Delays are a bit of a hacky workaround.
                //       Find some way to reliably wait until the command finishes with timeout.
                "voice", async (context) =>
                {
                    var config = VoiceModule.GetConfigForGuild(context.Guild);

                    await ExecuteAsync(context,
                        "vc clear",
                        "vc skip",
                        "vc join");
                    await DelayAsync(context, 3000);
                    await AssertAsync(context, new Dictionary<string, bool>()
                    {
                        {"song request queue is empty", config.SongRequests.Count == 0},
                        {"nothing is playing", config.CurrentlyPlayingSongRequest == null},
                        {"audio client has connected", !VoiceModule.CheckIfAudioClientDisconnected(config.AudioClient)}
                    });

                    await ExecuteAsync(context,
                        "vc add never gonna give you up",
                        "vc add https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                        "vc add \"me at the zoo\"");
                    await DelayAsync(context, 15000);
                    await ExecuteAsync(context,
                        "vc list");
                    await AssertAsync(context, new Dictionary<string, bool>()
                    {
                        {"currently playing is not null", config.CurrentlyPlayingSongRequest != null},
                        {"song request queue is correct length", config.SongRequests.Count == 2}
                    });

                    var firstCurrentlyPlayingRequest = config.CurrentlyPlayingSongRequest;

                    await ExecuteAsync(context,
                        "vc skip");
                    await AssertAsync(context, new Dictionary<string, bool>()
                    {
                        {"check next request is playing", config.CurrentlyPlayingSongRequest != firstCurrentlyPlayingRequest}
                    });

                    await ExecuteAsync(context,
                        "vc clear");
                    await AssertAsync(context,
                        "song request queue is empty", config.SongRequests.Count == 0);

                    await ExecuteAsync(context,
                        "vc leave");
                    await AssertAsync(context,
                        "audio client has disconnected", VoiceModule.CheckIfAudioClientDisconnected(config.AudioClient));
                }
            },
            {
                "keywords", async (context) =>
                {
                    var config = KeywordsModule.GetConfigForGuild(context.Guild);
                    const string keywordName = "testing_keyword";
                    List<KeywordValue> keywordValues;

                    await ExecuteAsync(context,
                        $"keywords remove {keywordName}");
                    await AssertAsync(context,
                        "keyword doesn't exist", !config.KeywordsDictionary.ContainsKey(keywordName));

                    await ExecuteAsync(context,
                        $"keywords add {keywordName} value1",
                        $"keywords add \"{keywordName}\" \"value2 **Title!**with title\"");
                    await AssertAsync(context, new Dictionary<string, bool>()
                    {
                        {$"keyword exists in dictionary", config.KeywordsDictionary.TryGetValue(keywordName, out keywordValues)},
                        {$"check value1", keywordValues[0].Content == "value1"},
                        {$"check value2", keywordValues[1].Content == "value2 with title" && keywordValues[1].Title == "Title!"}
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
            }
        };

        private static async Task<List<IResult>> ExecuteAsync(ICommandContext context, params string[] commands)
        {
            string commandPrefix = MainModule.GetConfigForGuild(context.Guild).CommandPrefix;
            var results = new List<IResult>();

            foreach (string command in commands)
            {
                await context.Channel.SendMessageAsync($"`{commandPrefix} {command}`");

                await Task.Delay(CommandDelay);
                results.Add(await EventHandlers.ExecuteCommandAsync(context, command));
                await Task.Delay(CommandDelay);
            }
            return results;
        }

        private static async Task DelayAsync(ICommandContext context, int milliseconds)
        {
            await context.Channel.SendMessageAsync($"**⏸️ PAUSE:** {milliseconds}ms");
            await Task.Delay(milliseconds);
        }

        private static async Task AssertAsync(ICommandContext context, string description, bool value)
        {
            if (!value) throw new Exception($"Assert failure ({description})");
            await context.Channel.SendMessageAsync($"**✅ ASSERT:** {description}");
            await Task.Delay(CommandDelay);
        }

        private static async Task AssertAsync(ICommandContext context, Dictionary<string, bool> asserts)
        {
            foreach (var assert in asserts)
                await AssertAsync(context, assert.Key, assert.Value);
        }
    }
}