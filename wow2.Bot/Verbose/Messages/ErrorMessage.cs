using System;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class ErrorMessage : SavedMessage
    {
        public ErrorMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Title = $"{new Emoji($"<:wowerror:{ErrorEmoteId}>")} Something bad happened...",
                Description = GetStatusMessageFormattedDescription(description, title),
                Timestamp = DateTime.Now,
                Color = new Color(0xE74C3C),
            };

            Components = new ComponentBuilder()
                .WithButton(
                    label: "Make a bug report",
                    style: ButtonStyle.Link,
                    url: "https://github.com/rednir/wow2/issues/new?assignees=&labels=bug&template=bug_report.md&title=");
        }
    }
}