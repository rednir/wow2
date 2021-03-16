using System.Text.RegularExpressions;
using System.Text;
using Discord.WebSocket;

namespace ExtentionMethods
{
    public static class Methods
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
                stringBuilder.Append(word + " ");
                charsThisLine += word.Length + 1;
                if (charsThisLine > maxCharsPerLine)
                {
                    stringBuilder.AppendLine();
                    charsThisLine = 0;
                }
            }

            return stringBuilder.ToString();
        }
    }
}