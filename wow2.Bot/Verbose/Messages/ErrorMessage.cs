using System;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class ErrorMessage : SavedMessage
    {
        protected override ActionButton[] ActionButtons => new[]
        {
            new ActionButton()
            {
                Label = "Make a bug report",
                Style = ButtonStyle.Link,
                Url = "https://github.com/rednir/wow2/issues/new?assignees=&labels=bug&template=bug_report.md&title=",
            },
        };

        public ErrorMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Title = $"{new Emoji($"<:wowerror:{ErrorEmoteId}>")} Something bad happened...",
                Description = GetStatusMessageFormattedDescription(description, title),
                Timestamp = DateTime.Now,
                Color = new Color(0xE74C3C),
            };
        }
    }
}