using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ExtentionMethods;

namespace wow2.Modules
{
    [Name("Keywords")]
    [Group("keywords")]
    [Alias("keyword")]
    public class KeywordsModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>Checks if a message contains a keyword, and responds to that message with the value if it does.</summary>
        public static async Task CheckMessageForKeywordAsync(SocketMessage message)
        {
            var keywordsDictionary = GetKeywordsDictionaryForGuild(message.GetGuild());
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
        [Summary("Adds value(s) to a keyword, creating a new keyword if it doesn't exist.")]
        public async Task AddAsync(string keyword = null, params string[] values)
        {
            var keywordsDictionary = GetKeywordsDictionaryForGuild(Context.Message.GetGuild());

            if (keyword == null)
            {
                await ReplyAsync(embed: MessageEmbedPresets.Verbose($"In order to add a keyword, you must specify it in the command.", VerboseMessageSeverity.Warning));
                return;
            }
            if (values.Length == 0)
            {
                await ReplyAsync(embed: MessageEmbedPresets.Verbose($"No values for the keyword were specified. Having a keyword without values is useless.", VerboseMessageSeverity.Warning));
                return;
            }

            // Create new dictionary key if necessary
            if (!keywordsDictionary.ContainsKey(keyword)) 
                keywordsDictionary.Add(keyword, new List<string>());

            // Add the keywords
            foreach (string value in values)
            {
                if (!keywordsDictionary[keyword].Contains(value))
                    keywordsDictionary[keyword].Add(value);
            }

            await ReplyAsync(embed: MessageEmbedPresets.Verbose($"Successfully added values to `{keyword}`\nIt now has `{values.Length}` total values.", VerboseMessageSeverity.Info));
            await DataManager.SaveGuildDataToFileAsync(Context.Message.GetGuild().Id);
        }

        [Command("remove")]
        [Alias("delete")]
        [Summary("Removes value(s) from a keyword, or if none are specified, removes all values and the keyword.")]
        public async Task RemoveAsync(string keyword = null, params string[] values)
        {
            var keywordsDictionary = GetKeywordsDictionaryForGuild(Context.Message.GetGuild());

            if (keyword == null)
            {
                await ReplyAsync(embed: MessageEmbedPresets.Verbose($"In order to remove a keyword, you must specify it in the command.", VerboseMessageSeverity.Warning));
                return;
            }

            if (!keywordsDictionary.ContainsKey(keyword))
            {
                await ReplyAsync(embed: MessageEmbedPresets.Verbose($"No such keyword `{keyword}` exists. Did you make a typo?", VerboseMessageSeverity.Warning));
                return;
            }

            if (values.Length == 0)
            {
                // No values have been specified, so assume the user wants to remove all values.
                keywordsDictionary.Remove(keyword);
                await ReplyAsync(embed: MessageEmbedPresets.Verbose($"Sucessfully removed keyword `{keyword}`.", VerboseMessageSeverity.Info));
            }
            else
            {
                // Iterate through and remove the values from the guild's data.
                List<string> unsuccessfulRemoves = new List<string>();
                foreach (string value in values)
                {
                    if (!keywordsDictionary[keyword].Remove(value))
                        unsuccessfulRemoves.Add(value);
                }
                if (keywordsDictionary[keyword].Count == 0)
                {
                    // Discard keyword with no values.
                    keywordsDictionary.Remove(keyword);
                }

                if (unsuccessfulRemoves.Count == 0)
                {
                    await ReplyAsync(embed: MessageEmbedPresets.Verbose($"Sucessfully removed `{values.Length}` values from {keyword}.", VerboseMessageSeverity.Info));
                }
                else
                {
                    // Make a string of all unsuccessful removes.
                    var stringBuilderForUnsuccessfulRemoves = new StringBuilder();
                    foreach (string value in unsuccessfulRemoves)
                    {
                        stringBuilderForUnsuccessfulRemoves.Append($"`{value}`\n");
                    }
                    await ReplyAsync(embed: MessageEmbedPresets.Verbose($"The following values could not be removed, probably because they don't exist.\n{stringBuilderForUnsuccessfulRemoves.ToString()}", VerboseMessageSeverity.Warning));
                }
            }
            await DataManager.SaveGuildDataToFileAsync(Context.Message.GetGuild().Id);
        }

        private static Dictionary<string, List<string>> GetKeywordsDictionaryForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Keywords;
    }
}
