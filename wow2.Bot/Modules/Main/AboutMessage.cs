using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Main
{
    public class AboutMessage : Message
    {
        public AboutMessage(string commandPrefix = "!wow")
        {
            var appInfo = BotService.ApplicationInfo;
            EmbedBuilder = new EmbedBuilder()
            {
                Title = $"{appInfo.Name}  •  in {BotService.Client.Guilds.Count} servers",
                Description = (string.IsNullOrWhiteSpace(appInfo.Description) ? null : appInfo.Description) + "\n[Link to github](https://github.com/rednir/wow2)",
                Color = Color.LightGrey,
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"Hosted by {appInfo.Owner}",
                    IconUrl = appInfo.Owner.GetAvatarUrl(),
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = $" - To view a list of commands, type `{commandPrefix} help`",
                },
            };
        }
    }
}