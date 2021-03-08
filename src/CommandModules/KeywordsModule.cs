using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ExtentionMethods;

namespace wow2.CommandModules
{
    [Group("keywords")]
    [Alias("keyword")]
    public class KeywordsModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>Checks if a message contains a keyword, and responds to that message with the value if it does.</summary>
        public static async Task CheckMessageForKeywordAsync(SocketMessage message)
        {
            var keywordsDictionary = GetKeywordsDictionary(message.GetGuild());
            string content = message.Content.ToLower();

            // Replace unnecessary symbols with a whitespace.
            content = new Regex("[;!.\"?\'#,:*-_\t\r ]|[\n]{2}").Replace(content, " ");

            foreach (string keyword in keywordsDictionary.Keys)
            {
                // Search for keyword with word boundaries, making sure that the keyword is not part of another word.
                if (Regex.IsMatch(content, @"\b" + Regex.Escape(keyword.ToLower()) + @"\b"))
                {
                    // If the keyword has multiple values, the value will be chosen randomly.
                    int chosenValueIndex = new Random().Next(keywordsDictionary[keyword].Count);
                    await message.Channel.SendMessageAsync(keywordsDictionary[keyword][chosenValueIndex]);
                }
            }
        }
        
        [Command("add")]
        [Summary("Adds a value to a keyword, creating a new keyword if it doesn't exist.")]
        public async Task AddAsync(string keyword = null, params string[] values)
        {
            var keywordsDictionary = GetKeywordsDictionary(Context.Message.GetGuild());

            if (!keywordsDictionary.ContainsKey(keyword))
                keywordsDictionary.Add(keyword, new List<string>());

            foreach (string value in values)
            {
                keywordsDictionary[keyword].Add(value);
            }

            await ReplyAsync($"Added {keyword} with {values.Length} values");
            await DataManager.SaveGuildDataToFileAsync(Context.Message.GetGuild().Id);

            foreach(var a in GetKeywordsDictionary(Context.Message.GetGuild()))
            {
                Console.WriteLine(a);
            }
        }

        private static Dictionary<string, List<string>> GetKeywordsDictionary(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Keywords;
    }
}
