using System;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class ErrorMessage : Message
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
        }
    }
}