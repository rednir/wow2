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
using ExtentionMethods;

namespace wow2.Modules.Keywords
{
    [Name("Keywords")]
    [Group("keywords")]
    [Alias("keyword")]
    [Summary("For automatically responding to keywords in user messages.")]
    public class KeywordsModule : ModuleBase<SocketCommandContext>
    {
        private static List<ulong> ListOfResponsesId = new List<ulong>();

        /// <summary>Checks if a message contains a keyword, and responds to that message with the value if it does.</summary>
        public static async Task<bool> CheckMessageForKeywordAsync(SocketMessage message)
        {
            var keywordsDictionary = DataManager.GetKeywordsConfigForGuild(message.GetGuild()).KeywordsDictionary;
            string content = message.Content.ToLower();
            List<string> listOfFoundKeywords = new List<string>();

            // Replace unnecessary symbols with a whitespace.
            // TODO: for some reason this removes numbers too
            content = new Regex("[;!.\"?\'#,:*-_\t\r ]|[\n]{2}").Replace(content, " ");

            foreach (string keyword in keywordsDictionary.Keys)
            {
                // Search for keyword with word boundaries, making sure that the keyword is not part of another word.
                if (Regex.IsMatch(content, @"\b" + Regex.Escape(keyword.ToLower()) + @"\b"))
                {
                    listOfFoundKeywords.Add(keyword);
                }
            }

            // Return if no keywords were found in the message.
            if (listOfFoundKeywords.Count == 0) return false;

            // Prioritize the longest keyword if multiple keywords have been found.
            string foundKeyword = listOfFoundKeywords.OrderByDescending(s => s.Length).First();

            // If the keyword has multiple values, the value will be chosen randomly.
            int chosenValueIndex = new Random().Next(keywordsDictionary[foundKeyword].Count);
            KeywordValue chosenValue = keywordsDictionary[foundKeyword][chosenValueIndex];

            // Get URL in message and seperate it (commented this out because even if url was not image, it would get seperated). 
            //var strippedUrlAndString = keywordsDictionary[foundKeyword][chosenValueIndex].Content.StripUrlIfExists();

            // Check if first word is URL and assume it is an image (this is also bad)
            //string valueImageUrl = chosenValue.Content.StartsWith("http") ? chosenValue.Content.Split(" ").First() : "";

            RestUserMessage sentKeywordResponseMessage;
            // Don't use embed message if the value to send contains a link.
            if (chosenValue.Content.Contains("http://") || chosenValue.Content.Contains("https://"))
            {
                sentKeywordResponseMessage = await message.Channel.SendMessageAsync(chosenValue.Content);
            }
            else
            {
                sentKeywordResponseMessage = await message.Channel.SendMessageAsync(embed: MessageEmbedPresets.GenericResponse(chosenValue.Content));
            }

            // Remember the messages that are actually keyword responses by adding them to a list.
            ListOfResponsesId.Add(sentKeywordResponseMessage.Id);

            if (DataManager.GetKeywordsConfigForGuild(message.GetGuild()).KeywordsReactToDelete)
                await sentKeywordResponseMessage.AddReactionAsync(new Emoji("🗑"));

            return true;
        }

        /// <summary>Checks if a message was a keyword response sent by the bot, deleting the message if so.</summary>
        /// <returns>True if the message was deleted, otherwise false.</returns>
        public static async Task<bool> DeleteMessageIfKeywordResponse(IUserMessage messageToCheck)
        {
            foreach (ulong id in ListOfResponsesId)
            {
                if (id == messageToCheck.Id)
                {
                    await messageToCheck.DeleteAsync();
                    return true;
                }
            }
            return false;
        }

        [Command("add")]
        [Summary("Adds value(s) to a keyword, creating a new keyword if it doesn't exist.")]
        public async Task AddAsync(string keyword = null, params string[] valueSplit)
        {
            var keywordsDictionary = DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsDictionary;

            if (keyword == null)
                throw new CommandReturnException("You forgot to type the keyword you want to add.", Context);

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
        public async Task RemoveAsync(string keyword = null, params string[] valueSplit)
        {
            var keywordsDictionary = DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsDictionary;

            if (keyword == null)
                throw new CommandReturnException("In order to remove a keyword, you must specify it in the command.", Context);

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
        [Summary("Shows a list of all keywords.")]
        public async Task ListAsync()
        {
            var keywordsDictionary = DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsDictionary;
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();

            // TODO: dont show value previews if too many keywords.
            // TODO: upload file instead if embed max reached

            foreach (var keywordPair in keywordsDictionary)
            {
                string nameToShow = (keywordPair.Value.Count > 1) ? ($"{keywordPair.Key} ({keywordPair.Value.Count} values):") : ($"{keywordPair.Key}:");
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
        [Summary("Toggles whether bot responses to keywords should have a reaction, allowing a user to delete the message.")]
        public async Task ToggleReactToDeleteAsync()
        {
            DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsReactToDelete = !DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsReactToDelete;
            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"React to delete is now `{(DataManager.GetKeywordsConfigForGuild(Context.Guild).KeywordsReactToDelete ? "on" : "off")}` for keyword responses.")
            );
        }
    }
}
