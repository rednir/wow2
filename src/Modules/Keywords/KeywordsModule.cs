using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Data;
using wow2.Extentions;
using wow2.Verbose.Messages;

namespace wow2.Modules.Keywords
{
    [Name("Keywords")]
    [Group("keywords")]
    [Alias("keyword", "k")]
    [Summary("For automatically responding to keywords in user messages.")]
    public class KeywordsModule : Module
    {
        public static readonly IEmote DeleteReactionEmote = new Emoji("🗑");
        public static readonly IEmote LikeReactionEmote = new Emoji("👍");
        private const int MaxCountOfRememberedKeywordResponses = 50;
        private const int MaxNumberOfKeywords = 50;
        private const int MaxNumberOfValues = 20;

        public static KeywordsModuleConfig GetConfigForGuild(IGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Keywords;

        /// <summary>Checks if a message was a keyword response sent by the bot, deleting the message if so.</summary>
        /// <returns>True if the message was deleted, otherwise false.</returns>
        public static async Task<bool> DeleteMessageIfKeywordResponse(IUserMessage messageToCheck)
        {
            var config = GetConfigForGuild(messageToCheck.GetGuild());

            foreach (ulong id in config.ListOfResponsesId)
            {
                if (id == messageToCheck.Id)
                {
                    await messageToCheck.DeleteAsync();
                    config.ListOfResponsesId.Remove(id);
                    await DataManager.SaveGuildDataToFileAsync(messageToCheck.GetGuild().Id);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Checks if a message contains a keyword, and responds to that message with the value if it does.</summary>
        public static bool CheckMessageForKeyword(SocketMessage message)
        {
            var config = GetConfigForGuild(message.GetGuild());
            string messageContent = message.Content.ToLower();
            List<string> listOfFoundKeywords = new();

            foreach (string keyword in config.KeywordsDictionary.Keys)
            {
                // No need to check with symbols removed.
                if (!messageContent.Contains(keyword))
                    continue;

                // Replace unnecessary symbols with a whitespace.
                var symbolsToReplace = "!.?;.'#\"_-\\".Where(c => !keyword.Contains(c)).ToArray();
                string messageContentWithoutSymbols = messageContent.ReplaceAll(symbolsToReplace, ' ');

                // Search for keyword with word boundaries, making sure that the keyword is not part of another word.
                if (messageContentWithoutSymbols.ContainsWord(keyword))
                    listOfFoundKeywords.Add(keyword);
            }

            if (listOfFoundKeywords.Count == 0)
                return false;

            // Prioritize the longest keyword if multiple keywords have been found.
            string foundKeyword = listOfFoundKeywords.OrderByDescending(s => s.Length).First();

            // Don't await this, as it can block the gateway task.
            _ = SendKeywordResponse(foundKeyword, message);

            return true;
        }

        [Command("add")]
        [Summary("Adds value(s) to a keyword, creating a new keyword if it doesn't exist.")]
        public async Task AddAsync(string keyword, [Name("value")][Remainder] string valueContent)
        {
            var keywordsDictionary = GetConfigForGuild(Context.Guild).KeywordsDictionary;
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
            var keywordsDictionary = GetConfigForGuild(Context.Guild).KeywordsDictionary;
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
            var keywordsDictionary = GetConfigForGuild(Context.Guild).KeywordsDictionary;

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
        [Summary("Shows a list of all keywords, and a preview of their values.")]
        public async Task ListAsync(int page = 1)
        {
            var keywordsDictionary = GetConfigForGuild(Context.Guild).KeywordsDictionary;
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();

            if (keywordsDictionary.Count == 0)
                throw new CommandReturnException(Context, "No keywords have been added yet, so there's nothing to show.");

            foreach (var keywordPair in keywordsDictionary)
            {
                string nameToShow = (keywordPair.Value.Count > 1) ? $"{keywordPair.Key}:\t({keywordPair.Value.Count} values)" : $"{keywordPair.Key}:";
                string valueToShow = $"`{keywordPair.Value[0].Content.Truncate(50, true)}`";

                var fieldBuilderForKeyword = new EmbedFieldBuilder()
                {
                    Name = nameToShow,
                    Value = valueToShow,
                };
                listOfFieldBuilders.Add(fieldBuilderForKeyword);
            }

            await new PagedMessage(
                fieldBuilders: listOfFieldBuilders,
                description: $"*There are {keywordsDictionary.Count} keywords in total, as listed below.*",
                title: "📒 Keywords",
                page: page)
                    .SendAsync(Context.Channel);
        }

        [Command("values")]
        [Alias("listvalues", "values", "list-value", "listvalue", "value", "list")]
        [Summary("Shows a list of values for a keyword.")]
        public async Task ListKeywordValuesAsync([Name("keyword")] string keyword, int page = 1)
        {
            var keywordsDictionary = GetConfigForGuild(Context.Guild).KeywordsDictionary;
            keyword = keyword.ToLower();

            if (!keywordsDictionary.TryGetValue(keyword, out List<KeywordValue> values))
                throw new CommandReturnException(Context, "If you want to list all keywords available, don't specify a keyword in the command.", "No such keyword");

            var fieldBuildersForValueList = new List<EmbedFieldBuilder>();
            foreach (KeywordValue value in values)
            {
                var user = value.AddedByUserId == 0 ?
                    "[UNKNOWN USER]" : Bot.Client.GetUser(value.AddedByUserId).Username;
                var date = value.DateTimeAddedBinary == 0 ?
                    "[UNKNOWN DATE]" : DateTime.FromBinary(value.DateTimeAddedBinary).ToShortDateString();

                fieldBuildersForValueList.Add(
                    new EmbedFieldBuilder()
                    {
                        Name = $"Added by {user} at {date}",
                        Value = $"```{value.Content}```",
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
            var config = GetConfigForGuild(Context.Guild);

            config.IsDeleteReactionOn = !config.IsDeleteReactionOn;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Delete reaction is now `{(config.IsDeleteReactionOn ? "on" : "off")}` for keyword responses.")
                .SendAsync(Context.Channel);
        }

        [Command("toggle-like-reaction")]
        [Summary("Toggles whether bot responses to keywords should have a thumbs up reaction.")]
        public async Task ToggleLikeReactionAsync()
        {
            var config = GetConfigForGuild(Context.Guild);

            config.IsLikeReactionOn = !GetConfigForGuild(Context.Guild).IsLikeReactionOn;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Like reaction is now `{(config.IsLikeReactionOn ? "on" : "off")}` for keyword responses.")
                .SendAsync(Context.Channel);
        }

        private static async Task SendKeywordResponse(string foundKeyword, SocketMessage message)
        {
            var config = GetConfigForGuild(message.GetGuild());

            // If the keyword has multiple values, the value will be chosen randomly.
            int chosenValueIndex = new Random().Next(config.KeywordsDictionary[foundKeyword].Count);
            KeywordValue chosenValue = config.KeywordsDictionary[foundKeyword][chosenValueIndex];

            IUserMessage sentKeywordResponseMessage;
            if (chosenValue.Content.Contains("http://") || chosenValue.Content.Contains("https://"))
            {
                // Don't use embed message if the value to send contains a link.
                sentKeywordResponseMessage = await message.Channel.SendMessageAsync(
                    text: chosenValue.Content,
                    messageReference: new MessageReference(message.Id));
            }
            else
            {
                sentKeywordResponseMessage = await new GenericMessage(
                    description: chosenValue.Content,
                    title: chosenValue.Title)
                {
                    ReplyToMessageId = message.Id,
                }
                .SendAsync(message.Channel);
            }

            if (config.IsLikeReactionOn)
                await sentKeywordResponseMessage.AddReactionAsync(LikeReactionEmote);
            if (config.IsDeleteReactionOn)
                await sentKeywordResponseMessage.AddReactionAsync(DeleteReactionEmote);

            // Remember the messages that are actually keyword responses by adding them to a list.
            config.ListOfResponsesId.Add(sentKeywordResponseMessage.Id);

            // Remove the oldest message if ListOfResponsesId has reached its max.
            if (config.ListOfResponsesId.Count > MaxCountOfRememberedKeywordResponses)
                config.ListOfResponsesId.RemoveAt(0);

            await DataManager.SaveGuildDataToFileAsync(message.GetGuild().Id);
        }
    }
}
