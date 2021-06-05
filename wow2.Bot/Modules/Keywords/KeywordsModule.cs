using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Keywords
{
    [Name("Keywords")]
    [Group("keywords")]
    [Alias("keyword", "k")]
    [Summary("Automatically respond to keywords in user messages.")]
    public class KeywordsModule : Module
    {
        private const int MaxNumberOfKeywords = 50;
        private const int MaxNumberOfValues = 20;

        private KeywordsModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Keywords;

        /// <summary>Checks if a message contains a keyword, and responds to that message with the value if it does.</summary>
        public static bool CheckMessageForKeyword(SocketCommandContext context)
        {
            var config = DataManager.AllGuildData[context.Guild.Id].Keywords;
            string messageContent = context.Message.Content.ToLower();
            string[] listOfFoundKeywords = GetAllKeywordsInString(messageContent, config.KeywordsDictionary.Keys);

            if (listOfFoundKeywords.Length == 0)
                return false;

            // Prioritize the longest keyword if multiple keywords have been found.
            string foundKeyword = listOfFoundKeywords.OrderByDescending(s => s.Length).First();

            // If the keyword has multiple values, the value will be chosen randomly.
            int chosenValueIndex = new Random().Next(config.KeywordsDictionary[foundKeyword].Count);
            KeywordValue keywordValue = config.KeywordsDictionary[foundKeyword][chosenValueIndex];

            // Don't await this to avoid blocking gateway task.
            _ = new ResponseMessage(keywordValue)
                .RespondToMessageAsync(context.Message);

            return true;
        }

        [Command("add")]
        [Summary("Adds value(s) to a keyword, creating a new keyword if it doesn't exist.")]
        public async Task AddAsync(string keyword, [Name("value")][Remainder] string valueContent)
        {
            var keywordsDictionary = Config.KeywordsDictionary;
            const int maxValueLength = 1024;
            valueContent = valueContent.Trim('\"');
            keyword = keyword.ToLower();

            if (valueContent.Length >= maxValueLength)
                throw new CommandReturnException(Context, $"The max length for a single value is {maxValueLength} characters.");

            // Check whether the user has specified a title.
            string valueTitle = valueContent.TextBetween("**");
            if (valueTitle != null)
                valueContent = valueContent.Replace($"**{valueTitle}**", null);

            if (!keywordsDictionary.ContainsKey(keyword))
            {
                if (keywordsDictionary.Count >= MaxNumberOfKeywords)
                    throw new CommandReturnException(Context, "You've got too many keywords. Try remove some first.", "Keywords limit reached");

                // Create new dictionary key if necessary.
                keywordsDictionary.Add(keyword, new List<KeywordValue>());
            }

            if (keywordsDictionary[keyword].FindIndex(x => x.Content == valueContent) != -1)
                throw new CommandReturnException(Context, "The value already exists in this keyword.");
            if (keywordsDictionary[keyword].Count >= MaxNumberOfValues)
                throw new CommandReturnException(Context, "You've got too many values in this keyword. Try remove some first.", "Value limit reached");

            keywordsDictionary[keyword].Add(new KeywordValue()
            {
                Content = valueContent,
                Title = valueTitle,
                AddedByUserId = Context.User.Id,
                DateTimeAddedBinary = DateTime.Now.ToBinary(),
            });

            await new SuccessMessage($"Added a value to `{keyword}`\nIt now has `{keywordsDictionary[keyword].Count}` total values.")
                .SendAsync(Context.Channel);
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("remove")]
        [Alias("delete")]
        [Summary("Removes value(s) from a keyword, or if none are specified, removes all values and the keyword.")]
        public async Task RemoveAsync(string keyword, [Name("value")][Remainder] string valueContent = null)
        {
            var keywordsDictionary = Config.KeywordsDictionary;
            keyword = keyword.ToLower();

            if (!keywordsDictionary.ContainsKey(keyword))
                throw new CommandReturnException(Context, $"No such keyword `{keyword}` exists. Did you make a typo?");

            if (string.IsNullOrEmpty(valueContent))
            {
                // No values have been specified, so assume the user wants to remove the keyword.
                keywordsDictionary.Remove(keyword);
                await new SuccessMessage($"Sucessfully removed keyword `{keyword}`.")
                    .SendAsync(Context.Channel);
            }
            else
            {
                valueContent = valueContent.Trim('\"');

                if (keywordsDictionary[keyword].RemoveAll(x => x.Content.Equals(valueContent, StringComparison.CurrentCultureIgnoreCase)) == 0)
                    throw new CommandReturnException(Context, $"No such value `{valueContent}` exists. Did you make a typo?");

                // Discard keyword if it has no values.
                if (keywordsDictionary[keyword].Count == 0)
                    keywordsDictionary.Remove(keyword);

                await new SuccessMessage($"Sucessfully removed `{valueContent}` from `{keyword}`.")
                    .SendAsync(Context.Channel);
            }

            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("rename")]
        [Alias("edit", "change")]
        [Summary("Renames a keyword, leaving its values unchanged.")]
        public async Task RenameAsync(string oldKeyword, [Remainder] string newKeyword)
        {
            var keywordsDictionary = Config.KeywordsDictionary;

            if (!keywordsDictionary.ContainsKey(oldKeyword))
                throw new CommandReturnException(Context, "Can't rename a keyword that doesn't exist. Did you make a typo?", "No such keyword");
            if (keywordsDictionary.ContainsKey(newKeyword))
                throw new CommandReturnException(Context, $"The keyword `{newKeyword}` already exists.");

            keywordsDictionary.RenameKey(oldKeyword, newKeyword);

            await new SuccessMessage($"Renamed `{oldKeyword}` to `{newKeyword}`.")
                .SendAsync(Context.Channel);
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("list")]
        [Alias("show", "all")]
        [Summary("Shows a list of all keywords, and a preview of their values. SORTBY can be date/likes/deletions")]
        public async Task ListAsync(int page = 1, KeywordSorts sort = KeywordSorts.Date)
        {
            var keywordsDictionary = Config.KeywordsDictionary;
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();

            if (keywordsDictionary.Count == 0)
                throw new CommandReturnException(Context, "No keywords have been added yet, so there's nothing to show.");

            var orderedDictionary = SortKeywordsDictionary(keywordsDictionary, sort);

            foreach (var keywordPair in orderedDictionary)
            {
                string nameToShow = (keywordPair.Value.Count > 1) ? $"{keywordPair.Key}:\t({keywordPair.Value.Count} values)" : $"{keywordPair.Key}:";
                string valueToShow = $"`{keywordPair.Value[0].Content.Truncate(50, true)}`";

                var fieldBuilderForKeyword = new EmbedFieldBuilder()
                {
                    Name = nameToShow,
                    Value = $"{keywordPair.Value.Sum(v => v.TimesLiked)} times liked, {keywordPair.Value.Sum(v => v.TimesDeleted)} times deleted.\n" + valueToShow,
                };
                listOfFieldBuilders.Add(fieldBuilderForKeyword);
            }

            await new PagedMessage(
                fieldBuilders: listOfFieldBuilders,
                description: $"*There are {orderedDictionary.Count} keywords in total, as listed below.*",
                title: "📒 Keywords",
                page: page)
                    .SendAsync(Context.Channel);
        }
        
        [Command("values")]
        [Alias("listvalues", "values", "list-value", "listvalue", "value", "list")]
        [Summary("Shows a list of values for a keyword.")]
        public async Task ListKeywordValuesAsync([Name("keyword")] string keyword, int page = 1)
        {
            var keywordsDictionary = Config.KeywordsDictionary;
            keyword = keyword.ToLower();

            if (!keywordsDictionary.TryGetValue(keyword, out List<KeywordValue> values))
                throw new CommandReturnException(Context, "If you want to list all keywords available, don't specify a keyword in the command.", "No such keyword");

            var fieldBuildersForValueList = new List<EmbedFieldBuilder>();
            foreach (KeywordValue value in values)
            {
                var user = value.AddedByUserId == 0 ?
                    "[UNKNOWN USER]" : BotService.Client.GetUser(value.AddedByUserId).Username;
                var date = value.DateTimeAddedBinary == 0 ?
                    "[UNKNOWN DATE]" : DateTime.FromBinary(value.DateTimeAddedBinary).ToShortDateString();

                fieldBuildersForValueList.Add(
                    new EmbedFieldBuilder()
                    {
                        Name = $"Added by {user} at {date}",
                        Value = $"{value.TimesLiked} times liked, {value.TimesDeleted} times deleted.\n```{value.Content}```",
                    });
            }

            await new PagedMessage(
                fieldBuilders: fieldBuildersForValueList,
                description: $"*There are {values.Count} values in total, as listed below.*",
                title: $"📒 Values for '{keyword}'",
                page: page)
                    .SendAsync(Context.Channel);
        }

        [Command("toggle-delete-reaction")]
        [Summary("Toggles whether bot responses to keywords should have a wastebasket reaction, allowing a user to delete the message.")]
        public async Task ToggleDeleteReactionAsync()
        {
            Config.IsDeleteReactionOn = !Config.IsDeleteReactionOn;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Delete reaction is now `{(Config.IsDeleteReactionOn ? "on" : "off")}` for keyword responses.")
                .SendAsync(Context.Channel);
        }

        [Command("toggle-like-reaction")]
        [Summary("Toggles whether bot responses to keywords should have a thumbs up reaction.")]
        public async Task ToggleLikeReactionAsync()
        {
            Config.IsLikeReactionOn = !Config.IsLikeReactionOn;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Like reaction is now `{(Config.IsLikeReactionOn ? "on" : "off")}` for keyword responses.")
                .SendAsync(Context.Channel);
        }

        private static string[] GetAllKeywordsInString(string content, IEnumerable<string> keywords)
        {
            var foundKeywords = new List<string>();
            foreach (string keyword in keywords)
            {
                // No need to check with symbols removed.
                if (!content.Contains(keyword))
                    continue;

                // Replace unnecessary symbols with a whitespace.
                var symbolsToReplace = "!.?;.'#\"_-\\".Where(c => !keyword.Contains(c)).ToArray();
                string contentWithoutSymbols = content.ReplaceAll(symbolsToReplace, ' ');

                // Search for keyword with word boundaries, making sure that the keyword is not part of another word.
                if (contentWithoutSymbols.ContainsWord(keyword))
                    foundKeywords.Add(keyword);
            }

            return foundKeywords.ToArray();
        }

        private static Dictionary<string, List<KeywordValue>> SortKeywordsDictionary(Dictionary<string, List<KeywordValue>> dictionary, KeywordSorts sort)
        {
            return sort switch
            {
                KeywordSorts.Likes => dictionary.OrderByDescending(p => p.Value.Sum(v => v.TimesLiked))
                    .ToDictionary(p => p.Key, p => p.Value),

                KeywordSorts.Deletions => dictionary.OrderByDescending(p => p.Value.Sum(v => v.TimesDeleted))
                    .ToDictionary(p => p.Key, p => p.Value),

                _ => new(dictionary),
            };
        }
    }
}
