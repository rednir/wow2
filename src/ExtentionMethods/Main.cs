using System;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

namespace ExtentionMethods
{
    public static class Main
    {
        // Add overloads if necessary.
        public static SocketGuild GetGuild(this SocketMessage socketMessage)
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
                Console.WriteLine(url);
                Console.WriteLine(stringContainingUrl.IndexOf("http") + (url.Length));
                Console.WriteLine((url.Length));
                return (stringContainingUrl.Substring(stringContainingUrl.IndexOf("http") + url.Length), url);
            }
        }
    }
}