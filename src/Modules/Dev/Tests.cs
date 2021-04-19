using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Commands;
using wow2.Modules.Keywords;

namespace wow2.Modules.Dev
{
    public static class Tests
    {
        public static Dictionary<string, Action<ICommandContext>> TestList = new Dictionary<string, Action<ICommandContext>>()
        {
            {
                "keywords", async (context) =>
                {
                    var config = KeywordsModule.GetConfigForGuild(context.Guild);
                    const string keywordName = "testing_keyword";
                    List<KeywordValue> keywordValues;

                    await ExecuteCommandsForTestAsync(context,
                        $"keywords add {keywordName} value1",
                        $"keywords add \"{keywordName}\" \"value2 **Title!**with title\"");
                    await Assert(context, new Dictionary<string, bool>()
                    {
                        {$"{keywordName} exists in dictionary", config.KeywordsDictionary.TryGetValue(keywordName, out keywordValues)},
                        {$"check value1", keywordValues[0].Content == "value1"},
                        {$"check value2", keywordValues[1].Content == "value2 with title" && keywordValues[1].Title == "Title!"}
                    });

                    await ExecuteCommandsForTestAsync(context,
                        "keywords remove testing_keyword");
                }
            }
        };

        private static async Task ExecuteCommandsForTestAsync(ICommandContext context, params string[] commands)
        {
            foreach (string command in commands)
            {
                await context.Channel.SendMessageAsync($"**⏩ INPUT:** {command}");

                await Task.Delay(1000);
                await EventHandlers.ExecuteCommandAsync(context, command);
                await Task.Delay(1000);
            }
        }

        private static async Task Assert(ICommandContext context, string description, bool value)
        {
            string resultText = value ? "✅" : "❌";
            await context.Channel.SendMessageAsync($"**{resultText} ASSERT:** {description}");
        }

        private static async Task Assert(ICommandContext context, Dictionary<string, bool> asserts)
        {
            foreach (var assert in asserts)
            {
                string resultText = assert.Value ? "✅" : "❌";
                await context.Channel.SendMessageAsync($"**{resultText} ASSERT:** {assert.Key}");
            }
        }
    }
}