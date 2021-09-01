using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace wow2.Bot.Extensions
{
    public static class SystemExtensions
    {
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

        /// <summary>Copies a string and places newline characters after words.</summary>
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

        /// <summary>Checks if a string is longer than the maximum length allowed and truncates it if so.</summary>
        /// <returns>The string with no more characters than the maximum length.</returns>
        public static string Truncate(this string originalString, int maxLength, bool addEllipses = false)
            => originalString.Length > maxLength ? originalString.Substring(0, maxLength - 4) + (addEllipses ? "..." : null) : originalString;

        /// <summary>Checks if a list is longer than the maximum length allowed and truncates it if so, removing elements from the start.</summary>
        /// <returns>The number of items removed.</returns>
        public static int Truncate<T>(this IList<T> list, int maxLength)
        {
            int elementsToRemove = Math.Max(list.Count - maxLength, 0);
            for (int i = 0; i < elementsToRemove; i++)
                list.RemoveAt(0);

            return elementsToRemove;
        }

        /// <summary>Adds a <typeparamref name="separator"/> throughout an int.</summary>
        /// <returns>The integer as a string with <typeparamref name="separator"/> placed throughout it.</returns>
        public static string Humanize(this long input, char separator = ',')
        {
            StringBuilder stringBuilder = new(input.ToString());

            bool isNegative = false;
            if (stringBuilder[0] == '-')
            {
                isNegative = true;
                stringBuilder.Remove(0, 1);
            }

            if (stringBuilder.Length <= 4)
                return stringBuilder.ToString();

            for (int i = stringBuilder.Length - 3; i > 0; i -= 3)
                stringBuilder.Insert(i, separator);

            return (isNegative ? '-' : null) + stringBuilder.ToString();
        }

        /// <summary>Checks if a string starts with a given value, and also is not part of another word.</summary>
        public static bool StartsWithWord(this string stringToCheck, string word, bool ignoreCase = false) =>

            // The stringToCheck starts with word, and has a space directly after the word.
            (stringToCheck.StartsWith(word, ignoreCase, null) && stringToCheck.IndexOf(" ") == word.Length)

            // Or the stringToCheck is a direct match with the word.
            || stringToCheck.Equals(word, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);

        /// <summary>Removes a key and adds a new key, transferring its values.</summary>
        public static void RenameKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey oldKey, TKey newKey)
        {
            if (dictionary.ContainsKey(newKey))
                throw new ArgumentException($"The key {newKey} exists in the dictionary.");

            var valuesToMove = dictionary[oldKey];
            dictionary.Remove(oldKey);
            dictionary.Add(newKey, valuesToMove);
        }

        /// <summary>Creates a new string where there is no more than one adjacent whitespace.</summary>
        /// <returns>The modified string.</returns>
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

        /// <summary>Changes command input to one without the command prefix, also discarding unnecessary whitespaces.</summary>
        /// <returns>The string without the command prefix.</return>
        public static string MakeCommandInput(this string messageContent, string commandPrefix)
        {
            if (!messageContent.StartsWith(commandPrefix))
                return messageContent;

            return messageContent.RemoveUnnecessaryWhiteSpace()[(commandPrefix.Length + 1)..];
        }

        /// <summary>Replaces all instances of some characters with a new character.</summary>
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

        /// <summary>Checks whether a given string contains a term, and that term is adjacent to two whitespaces.</summary>
        /// <returns>True if the string contains the term.</returns>
        public static bool ContainsWord(this string stringToSearch, string word)
        {
            var stringBuilder = new StringBuilder(stringToSearch);
            string stringToSearchWithBoundaries = stringBuilder
                .Insert(0, ' ').Append(' ').ToString();
            return stringToSearchWithBoundaries.Contains($" {word} ");
        }

        /// <summary>Converts a string to a memory stream.</summary>
        /// <returns>A new instance of MemoryStream containing the bytes of the string.</returns>
        public static MemoryStream ToMemoryStream(this string inputString)
            => new(Encoding.ASCII.GetBytes(inputString));

        /// <summary>Parses the string into a TimeSpan where the last character is the units.</summary>
        /// <returns>Whether the conversion was successful.</returns>
        public static bool TryConvertToTimeSpan(this string inputString, out TimeSpan timeSpan)
        {
            string units = Regex.Replace(inputString, "[^a-zA-z]", string.Empty);
            float number;
            try
            {
                number = Convert.ToSingle(
                    Regex.Replace(inputString, "[^.0-9]", string.Empty));
            }
            catch (FormatException)
            {
                timeSpan = TimeSpan.Zero;
                return false;
            }

            timeSpan = units switch
            {
                "ms" => TimeSpan.FromMilliseconds(number),
                "s" => TimeSpan.FromSeconds(number),
                "m" => TimeSpan.FromMinutes(number),
                "h" => TimeSpan.FromHours(number),
                "d" => TimeSpan.FromDays(number),
                _ => TimeSpan.Zero,
            };

            return timeSpan == TimeSpan.Zero;
        }
    }
}