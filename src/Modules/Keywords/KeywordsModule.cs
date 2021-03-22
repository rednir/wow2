using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Rest;
using Discord.Commands;
using wow2.Verbose;
using wow2.Data;
using wow2.Extentions;

namespace wow2.Modules.Keywords
{
    [Name("Keywords")]
    [Group("keywords")]
    [Alias("keyword")]
    [Summary("For automatically responding to keywords in user messages.")]
    public class KeywordsModule : ModuleBase<SocketCommandContext>
    {
        public static IEmote ReactToDeleteEmote { get; } = new Emoji("🗑");
        private const int MaxCountOfRememberedKeywordResponses = 100;

        /// <summary>Checks if a message contains a keyword, and responds to that message with the value if it does.</summary>
        public static async Task<bool> CheckMessageForKeywordAsync(SocketMessage message)
        {
            var config = DataManager.GetKeywordsConfigForGuild(message.GetGuild());
            string content = message.Content.ToLower();
            List<string> listOfFoundKeywords = new List<string>();

            // Replace unnecessary symbols with a whitespace.
            content = new Regex("[;!.\"?\'#,:*-_\t\r ]|[\n]{2}").Replace(content, " ");

            foreach (string keyword in config.KeywordsDictionary.Keys)
            {
                // Search for keyword with word boundaries, making sure that the keyword is not part of another word.
                if (Regex.IsMatch(content, @"\b" + Regex.Escape(keyword.ToLower()) + @"\b"))
                {
                    listOfFoundKeywords.Add(keyword);
                }
            }

            if (listOfFoundKeywords.Count == 0) return false;

            // Prioritize the longest keyword if multiple keywords have been found.
            string foundKeyword = listOfFoundKeywords.OrderByDescending(s => s.Length).First();

            // If the keyword has multiple values, the value will be chosen randomly.
            int chosenValueIndex = new Random().Next(config.KeywordsDictionary[foundKeyword].Count);
            KeywordValue chosenValue = config.KeywordsDictionary[foundKeyword][chosenValueIndex];

            RestUserMessage sentKeywordResponseMessage;
            if (chosenValue.Content.Contains("http://") || chosenValue.Content.Contains("https://"))
            {
                // Don't use embed message if the value to send contains a link.
                sentKeywordResponseMessage = await message.Channel.SendMessageAsync(chosenValue.Content);
            }
            else
            {
                sentKeywordResponseMessage = await message.Channel.SendMessageAsync(embed: MessageEmbedPresets.GenericResponse(chosenValue.Content));
            }

            if (DataManager.GetKeywordsConfigForGuild(message.GetGuild()).KeywordsReactToDelete)
                await sentKeywordResponseMessage.AddReactionAsync(ReactToDeleteEmote);

            // Remember the messages that are actually keyword responses by adding them to a list.
            config.ListOfResponsesId.Add(sentKeywordResponseMessage.Id);
            await DataManager.SaveGuildDataToFileAsync(message.GetGuild().Id);

            // Remove the oldest message if ListOfResponsesId has reached its max.
            if (config.ListOfResponsesId.Count > MaxCountOfRememberedKeywordResponses)
                config.ListOfResponsesId.RemoveAt(0);

            return true;
        }

        /// <summary>Checks if a message was a keyword response sent by the bot, deleting the message if so.</summary>
        /// <returns>True if the message was deleted, otherwise false.</returns>
        public static async Task<bool> DeleteMessageIfKeywordResponse(IUserMessage messageToCheck)
        {
            var config = DataManager.GetKeywordsConfigForGuild(messageToCheck.GetGuild());

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

        [Command("add")]
        [Summary("Adds value(s) to a keyword, creating a new keyword if it doesn't exist.")]
        public async Task AddAsync(string keyword, [Name("value")] params string[] valueSplit)
        {
            var keywordsDictionary = DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsDictionary;

            if (valueSplit.Length == 0)
                throw new CommandReturnException("No value to add to the keyword was specified.", Context);

            string value = string.Join(" ", valueSplit);

            // Create new dictionary key if necessary.
            if (!keywordsDictionary.ContainsKey(keyword))
                keywordsDictionary.Add(keyword, new List<KeywordValue>());

            if (keywordsDictionary[keyword].FindIndex(x => x.Content == value) != -1)
                throw new CommandReturnException("The value already exists in the keyword.", Context);

            keywordsDictionary[keyword].Add(new KeywordValue() { Content = value });

            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"Successfully added values to `{keyword}`\nIt now has `{keywordsDictionary[keyword].Count}` total values.")
            );
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("remove")]
        [Alias("delete")]
        [Summary("Removes value(s) from a keyword, or if none are specified, removes all values and the keyword.")]
        public async Task RemoveAsync(string keyword, [Name("value")] params string[] valueSplit)
        {
            var keywordsDictionary = DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsDictionary;

            if (!keywordsDictionary.ContainsKey(keyword))
                throw new CommandReturnException($"No such keyword `{keyword}` exists. Did you make a typo?", Context);

            if (valueSplit.Length == 0)
            {
                // No values have been specified, so assume the user wants to remove the keyword.
                keywordsDictionary.Remove(keyword);
                await ReplyAsync(
                    embed: MessageEmbedPresets.Verbose($"Sucessfully removed keyword `{keyword}`.")
                );
            }
            else
            {
                string value = string.Join(" ", valueSplit);

                if (keywordsDictionary[keyword].RemoveAll(x => x.Content == value) == 0)
                    throw new CommandReturnException($"No such value `{value}` exists. Did you make a typo?", Context);

                // Discard keyword if it has no values.
                if (keywordsDictionary[keyword].Count == 0)
                    keywordsDictionary.Remove(keyword);

                await ReplyAsync(
                    embed: MessageEmbedPresets.Verbose($"Sucessfully removed `{value}` from `{keyword}`.")
                );
            }
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("list")]
        [Alias("show", "all")]
        [Summary("Shows a list of all keywords, and a preview of their values. Previews will not be shown if the list of keywords is too large.")]
        public async Task ListAsync()
        {
            const int maxFields = 16;

            var keywordsDictionary = DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsDictionary;
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();

            if (keywordsDictionary.Count > maxFields)
            {
                await ListMinimalAsync();
                return;
            }

            foreach (var keywordPair in keywordsDictionary)
            {
                string nameToShow = (keywordPair.Value.Count > 1) ? ($"{keywordPair.Key}:\t({keywordPair.Value.Count} values)") : ($"{keywordPair.Key}:");
                string valueToShow = (keywordPair.Value[0].Content.Length > 50) ? ($"`{keywordPair.Value[0].Content.Substring(0, 47)}...`") : ($"`{keywordPair.Value[0].Content}`");

                var fieldBuilderForKeyword = new EmbedFieldBuilder()
                {
                    Name = nameToShow,
                    Value = valueToShow
                };
                listOfFieldBuilders.Add(fieldBuilderForKeyword);
            }

            await ReplyAsync(
                embed: MessageEmbedPresets.Fields(listOfFieldBuilders, "Keywords", $"*There are {keywordsDictionary.Count} keywords in total, as listed below.*")
            );
        }

        [Command("toggle-react-to-delete")]
        [Summary("Toggles whether my responses to keywords should have a reaction, allowing a user to delete the message.")]
        public async Task ToggleReactToDeleteAsync()
        {
            DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsReactToDelete = !DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsReactToDelete;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"React to delete is now `{(DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsReactToDelete ? "on" : "off")}` for keyword responses.")
            );
        }

        /// <summary>Alternative to list command, where only keywords are shown</summary>
        private async Task ListMinimalAsync()
        {
            var keywordsDictionary = DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsDictionary;
            var descriptionBuilder = new StringBuilder($"*There are {keywordsDictionary.Count} keywords in total, as listed below.*\n\n");

            foreach (var keywordPair in keywordsDictionary)
            {
                descriptionBuilder.Append($"{(keywordPair.Value.Count > 1 ? $"`{keywordPair.Key}` ({keywordPair.Value.Count} values)" : $"`{keywordPair.Key}`")}\n");
            }

            await ReplyAsync(
                embed: MessageEmbedPresets.GenericResponse(descriptionBuilder.ToString(), "Keywords")
            );
        }
    }
}
