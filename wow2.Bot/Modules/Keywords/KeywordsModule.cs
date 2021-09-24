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
        private const int MaxNumberOfKeywords = 100;
        private const int MaxNumberOfValues = 40;

        private static readonly Random Random = new();

        private KeywordsModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Keywords;

        /// <summary>Checks if a message contains a keyword, and responds to that message with the value if it does.</summary>
        public static bool CheckMessageForKeyword(SocketCommandContext context)
        {
            var config = DataManager.AllGuildData[context.Guild.Id].Keywords;

            if (!config.IsResponsesOn)
                return false;

            string messageContent = context.Message.Content.ToLower();
            string[] listOfFoundKeywords = GetAllKeywordsInString(messageContent, config.KeywordsDictionary.Keys);

            if (listOfFoundKeywords.Length == 0)
                return false;

            if (Random.Next(0, 100) > config.ResponseChancePercentage)
                return true;

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

            await new SuccessMessage($"It now has `{keywordsDictionary[keyword].Count}` total values.", $"Added a value to `{keyword}`")
                .SendAsync(Context.Channel);
        }

        [Command("remove")]
        [Alias("delete")]
        [Summary("Removes value(s) from a keyword, or if none are specified, removes all values and the keyword.")]
        public async Task RemoveAsync(string keyword, [Name("value")][Remainder] string valueContent = null)
        {
            keyword = keyword.ToLower();

            if (!Config.KeywordsDictionary.ContainsKey(keyword))
                throw new CommandReturnException(Context, $"No such keyword `{keyword}` exists. Did you make a typo?");

            // If the user didn't specify a value, assume removal of entire keyword.
            if (string.IsNullOrEmpty(valueContent))
            {
                async Task delete()
                {
                    Config.DeletedKeywordsDictionary.Add(keyword, Config.KeywordsDictionary[keyword]);
                    Config.KeywordsDictionary.Remove(keyword);

                    await new SuccessMessage($"Sucessfully removed keyword `{keyword}`.")
                        .SendAsync(Context.Channel);
                }

                if (Config.DeletedKeywordsDictionary.ContainsKey(keyword))
                    Config.DeletedKeywordsDictionary.Remove(keyword);

                if (Config.KeywordsDictionary[keyword].Count > 1)
                {
                    // Warn the user about deleting multiple values.
                    await new QuestionMessage(
                        description: $"You are about to delete `{keyword}` and its {Config.KeywordsDictionary[keyword].Count} values.\nAre you okay with that?",
                        title: "Here be dragons...",
                        onConfirm: delete,
                        onDeny: async () =>
                        {
                            await new InfoMessage($"You can do that by typing `{Context.Guild.GetCommandPrefix()} keywords remove {keyword} [VALUE]`", "Just want to remove one value?")
                                .SendAsync(Context.Channel);
                        })
                            .SendAsync(Context.Channel);
                }
                else
                {
                    await delete();
                }
            }
            else
            {
                valueContent = valueContent.Trim('\"');

                if (Config.KeywordsDictionary[keyword]
                    .RemoveAll(x => x.Content.Equals(valueContent, StringComparison.CurrentCultureIgnoreCase)) == 0)
                {
                    throw new CommandReturnException(Context, $"No such value `{valueContent}` exists. Did you make a typo?");
                }

                // Discard keyword if it has no values.
                if (Config.KeywordsDictionary[keyword].Count == 0)
                    Config.KeywordsDictionary.Remove(keyword);

                await new SuccessMessage($"Sucessfully removed `{valueContent}` from `{keyword}`.")
                    .SendAsync(Context.Channel);
            }
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
        }

        [Command("list")]
        [Alias("show", "all")]
        [Summary("Shows a list of all keywords, and a preview of their values. SORT can be date/likes/deletions/values")]
        public async Task ListAsync(KeywordSorts sort = KeywordSorts.Date, int page = 1)
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

            await new ValueListMessage(keyword, values, async () => await GalleryAsync(keyword), page)
                .SendAsync(Context.Channel);
        }

        [Command("gallery")]
        [Summary("Shows all values in a keyword as a message you can scroll through (useful for values with images or videos)")]
        public async Task GalleryAsync(string keyword)
        {
            var keywordsDictionary = Config.KeywordsDictionary;
            keyword = keyword.ToLower();

            if (!keywordsDictionary.TryGetValue(keyword, out List<KeywordValue> values))
                throw new CommandReturnException(Context, "No such keyword.");

            await new ResponseGallery(values)
                .SendAsync(Context.Channel);
        }

        [Command("restore")]
        [Summary("Restores a previously deleted keyword from its name.")]
        public async Task RestoreAsync([Remainder] string keyword)
        {
            if (!Config.DeletedKeywordsDictionary.ContainsKey(keyword))
                throw new CommandReturnException("No keyword with that name was previously deleted.");

            Config.KeywordsDictionary.Add(keyword, Config.DeletedKeywordsDictionary[keyword]);
            Config.DeletedKeywordsDictionary.Remove(keyword);

            await new SuccessMessage($"Restored `{keyword}` and it's {Config.KeywordsDictionary[keyword].Count} values.")
                .SendAsync(Context.Channel);
        }

        [Command("set-chance")]
        [Alias("chance")]
        [Summary("Sets the chance of the bot replying to a keyword with a response.")]
        public async Task SetChanceAsync(int percentage)
        {
            if (percentage <= 0)
                throw new CommandReturnException("Percentage is too low. Consider using the `toggle-responses` command.");
            if (percentage > 100)
                throw new CommandReturnException("That.. isn't how percentages work.");

            Config.ResponseChancePercentage = percentage;
            await new SuccessMessage($"Keyword responses will be responded to {percentage}% of the time.")
                .SendAsync(Context.Channel);
        }

        [Command("toggle-delete-reaction")]
        [Summary("Toggles whether bot responses to keywords should have a wastebasket reaction, allowing a user to delete the message.")]
        public async Task ToggleDeleteReactionAsync()
        {
            await SendToggleQuestionAsync(
                currentState: Config.IsDeleteReactionOn,
                setter: x => Config.IsDeleteReactionOn = x,
                toggledOnMessage: "Future responses to keywords will have a delete reaction.",
                toggledOffMessage: "Future responses to keywords will no longer have a delete reaction.");
        }

        [Command("toggle-like-reaction")]
        [Summary("Toggles whether bot responses to keywords should have a thumbs up reaction.")]
        public async Task ToggleLikeReactionAsync()
        {
            await SendToggleQuestionAsync(
                currentState: Config.IsLikeReactionOn,
                setter: x => Config.IsLikeReactionOn = x,
                toggledOnMessage: "Future responses to keywords will have a like reaction.",
                toggledOffMessage: "Future responses to keywords will no longer have a like reaction.");
        }

        [Command("toggle-responses")]
        [Alias("toggle-response", "toggle")]
        [Summary("Toggles whether the bot will respond to keywords.")]
        public async Task ToggleResponsesAsync()
        {
            await SendToggleQuestionAsync(
                currentState: Config.IsResponsesOn,
                setter: x => Config.IsResponsesOn = x,
                toggledOnMessage: "Keyword responses have been enabled.",
                toggledOffMessage: "Keyword responses have been disabled.");
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

                KeywordSorts.Values => dictionary.OrderByDescending(p => p.Value.Count)
                    .ToDictionary(p => p.Key, p => p.Value),

                _ => new(dictionary),
            };
        }
    }
}
