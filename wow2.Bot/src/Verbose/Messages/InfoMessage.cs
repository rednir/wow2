using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class InfoMessage : Message
    {
        public InfoMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowinfo:{InfoEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = Color.Blue,
            };
        }
    }
}