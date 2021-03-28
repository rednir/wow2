using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using Discord;
using Discord.WebSocket;

namespace wow2.Extentions
{
    public static class Methods
    {
        public static SocketGuild GetGuild(this SocketMessage socketMessage)
            => ((SocketGuildChannel)socketMessage.Channel).Guild;
        public static SocketGuild GetGuild(this IUserMessage socketMessage)
            => ((SocketGuildChannel)socketMessage.Channel).Guild;

        /// <returns>The first url in the string and the new string without the url, or an empty string in place of the url if there is none.</returns>
        public static (string newString, string url) StripUrlIfExists(this string stringContainingUrl)
        {
            Regex regex = new Regex(@"(http|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?");
            MatchCollection matches = regex.Matches(stringContainingUrl);
            if (matches.Count == 0)
            {
                return (stringContainingUrl, "");
            }
            else
            {
                string url = matches[0].Value.ToString();
                return (stringContainingUrl.Substring(stringContainingUrl.IndexOf("http") + url.Length), url);
            }
        }

        /// <returns>The string with newlines placed.</returns>
        public static string Wrap(this string originalString, int maxCharsPerLine)
        {
            string[] splitString = originalString.Split(" ");
            var stringBuilder = new StringBuilder();

            int charsThisLine = 0;
            foreach (string word in splitString)
            {
                charsThisLine += word.Length + 1;
                if (charsThisLine > maxCharsPerLine)
                {
                    stringBuilder.Append("\n" + word + " ");
                    charsThisLine = word.Length;
                }
                else
                {
                    stringBuilder.Append(word + " ");
                }
            }

            // Remove unnecessary space after final word.
            stringBuilder.Remove(stringBuilder.Length - 1, 1);

            return stringBuilder.ToString();
        }

        /// <summary>Check if a string starts with a given value, and also is not part of another word.</summary>
        public static bool StartsWithWord(this string stringToCheck, string word, bool ignoreCase = false) =>
            // The stringToCheck starts with word, and has a space directly after the word.
            (stringToCheck.StartsWith(word, ignoreCase, null) && stringToCheck.IndexOf(" ") == word.Length)
            // Or the stringToCheck is a direct match with the word.
            || stringToCheck.Equals(word, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);

        public static void RenameKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey oldKey, TKey newKey)
        {
            var valuesToMove = dictionary[oldKey];
            dictionary.Remove(oldKey);
            dictionary.Add(newKey, valuesToMove);
        }

        /// <returns>The string with no more than one adjacent whitespace.</returns>
        public static string RemoveUnnecessaryWhiteSpace(this string stringToChange)
        {
            var stringBuilder = new StringBuilder();
            bool lastCharWasWhiteSpace = false;
            foreach (char character in stringToChange)
            {
                if (char.IsWhiteSpace(character) && lastCharWasWhiteSpace) continue;
                lastCharWasWhiteSpace = char.IsWhiteSpace(character);
                stringBuilder.Append(character);
            }
            return stringBuilder.ToString();
        }
    }
}