using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace wow2.Extentions
{
    public static class Methods
    {
        public static SocketGuild GetGuild(this SocketMessage socketMessage)
            => ((SocketGuildChannel)socketMessage.Channel).Guild;
        public static SocketGuild GetGuild(this IUserMessage socketMessage)
            => ((SocketGuildChannel)socketMessage.Channel).Guild;

        // I don't think this even works as intended but it's unused for now anyway.
        /// <returns>The first url in the string and the new string without the url, or an empty string in place of the url if there is none.</returns>
        public static (string newString, string url) StripUrlIfExists(this string stringContainingUrl)
        {
            Regex regex = new(@"(http|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?");
            MatchCollection matches = regex.Matches(stringContainingUrl);
            if (matches.Count == 0)
            {
                return (stringContainingUrl, string.Empty);
            }
            else
            {
                string url = matches[0].Value;
                return (stringContainingUrl[(stringContainingUrl.IndexOf("http") + url.Length)..], url);
            }
        }

        public static string TextBetween(this string stringToSearch, string separator)
        {
            var stringBuilder = new StringBuilder();

            // No separators exist or only one separator exist.
            if (!stringToSearch.Contains(separator) ||
                stringToSearch.IndexOf(separator) == stringToSearch.LastIndexOf(separator))
            {
                return null;
            }

            // The index of the next character after the first instance of the seperator.
            int startIndex = stringToSearch.IndexOf(separator) + separator.Length;

            int charactersOfNextSeparatorFound = 0;
            foreach (char character in stringToSearch[startIndex..])
            {
                stringBuilder.Append(character);
                if (separator.Contains(character))
                {
                    charactersOfNextSeparatorFound++;
                    if (charactersOfNextSeparatorFound == separator.Length)
                    {
                        stringBuilder.Replace(separator, null);
                        break;
                    }
                }
                else
                {
                    charactersOfNextSeparatorFound = 0;
                }
            }
            return stringBuilder.ToString();
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
                    stringBuilder.Append($"\n{word} ");
                    charsThisLine = word.Length;
                }
                else
                {
                    stringBuilder.Append($"{word} ");
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

        /// <summary>Removes a key and adds a new key, transferring its values.</summary>
        /// <exception cref="ArgumentException"></exception>
        public static void RenameKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey oldKey, TKey newKey)
        {
            if (dictionary.ContainsKey(newKey))
                throw new ArgumentException($"The key {newKey} exists in the dictionary.");

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
                if (char.IsWhiteSpace(character) && lastCharWasWhiteSpace)
                    continue;
                lastCharWasWhiteSpace = char.IsWhiteSpace(character);
                stringBuilder.Append(character);
            }
            return stringBuilder.ToString();
        }

        /// <returns>The string without the command prefix.</return>
        public static string MakeCommandInput(this string messageContent, string commandPrefix)
            => messageContent.RemoveUnnecessaryWhiteSpace()[(commandPrefix.Length + 1)..];

        /// <returns>The string with the list of characters removed.</returns>
        public static string ReplaceAll(this string stringToChange, char[] charsToReplace, char? replacementChar)
        {
            var stringBuilder = new StringBuilder();
            foreach (char character in stringToChange)
            {
                stringBuilder.Append(
                    charsToReplace.Contains(character) ? replacementChar : character);
            }
            return stringBuilder.ToString();
        }

        /// <returns>True if the string contains the parameter, and the parameter is adjacent to two whitespaces.</returns>
        public static bool ContainsWord(this string stringToSearch, string word)
        {
            var stringBuilder = new StringBuilder(stringToSearch);
            string stringToSearchWithBoundaries = stringBuilder
                .Insert(0, ' ').Append(' ').ToString();
            return stringToSearchWithBoundaries.Contains($" {word} ");
        }

        /// <returns>A memory stream containing the bytes of the string</returns>
        public static MemoryStream ToMemoryStream(this string inputString)
            => new(Encoding.ASCII.GetBytes(inputString));

        /// <returns>The list of commands as a readable string, with newlines placed between each command.</returns>
        public static string MakeReadableString(this IEnumerable<CommandInfo> commands, string commandPrefix)
        {
            string result = string.Empty;
            int index = 0;
            foreach (var command in commands)
            {
                result += command.MakeFullCommandString(commandPrefix) + "\n";
                if (index >= 5)
                    break;
                index++;
            }
            return result.TrimEnd('\n');
        }

        /// <returns>The list of parameters as a readable string.</returns>
        public static string MakeReadableString(this IEnumerable<ParameterInfo> parameters)
        {
            string parametersInfo = string.Empty;
            foreach (ParameterInfo parameter in parameters)
            {
                string optionalText = parameter.IsOptional ? "optional:" : string.Empty;
                parametersInfo += $" [{optionalText}{parameter.Name.ToUpper()}]";
            }
            return parametersInfo;
        }

        /// <returns>The command info into a readable string.</returns>
        public static string MakeFullCommandString(this CommandInfo command, string commandPrefix)
            => $"`{commandPrefix} {(string.IsNullOrWhiteSpace(command.Module.Group) ? string.Empty : $"{command.Module.Group} ")}{command.Name}{command.Parameters.MakeReadableString()}`";
    }
}