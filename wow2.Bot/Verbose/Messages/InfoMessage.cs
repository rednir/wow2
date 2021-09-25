using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class InfoMessage : SavedMessage
    {
        public InfoMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowinfo:{InfoEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = new Color(0x3498DB),
            };
        }
    }
}