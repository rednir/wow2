using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using ExtentionMethods;

namespace wow2.UserCommands
{
    public class Keywords
    {
        /// <summary>Checks if a message contains a keyword, and responds to that message with the value if it does.</summary>
        public static async Task CheckMessageForKeyword(SocketMessage message)
        {
            var keywordsDictionary = DataManager.DictionaryOfGuildData[message.GetGuild().Id].Keywords;
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
    }
}
