using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class SuccessMessage : InteractiveMessage
    {
        public SuccessMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowsuccess:{SuccessEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = new Color(0x2ECC71),
            };
        }
    }
}