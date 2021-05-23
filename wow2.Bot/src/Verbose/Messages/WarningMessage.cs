using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class WarningMessage : Message
    {
        public WarningMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowwarning:{WarningEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = Color.LightOrange,
            };
        }
    }
}