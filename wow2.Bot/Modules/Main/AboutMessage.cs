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
                Title = $"{appInfo.Name} / in {BotService.Client.Guilds.Count} servers",
                Description = string.IsNullOrWhiteSpace(appInfo.Description) ? null : appInfo.Description,
                Color = Color.LightGrey,
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"Hosted by {appInfo.Owner}",
                    IconUrl = appInfo.Owner.GetAvatarUrl(),
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = "Have any concerns? Shoot me a DM, and the bot owner will see!",
                },
            };

            Components = new ComponentBuilder()
                .WithButton("View all commands", style: ButtonStyle.Link, url: "https://github.com/rednir/wow2/blob/master/COMMANDS.md")
                .WithButton("View source code", style: ButtonStyle.Link, url: "https://github.com/rednir/wow2")
                .WithButton("Report an issue", style: ButtonStyle.Link, url: "https://github.com/rednir/wow2/issues/new/choose");
        }
    }
}