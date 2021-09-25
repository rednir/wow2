using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class WarningMessage : SavedMessage
    {
        public WarningMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowwarning:{WarningEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = new Color(0xF39C12),
            };
        }
    }
}