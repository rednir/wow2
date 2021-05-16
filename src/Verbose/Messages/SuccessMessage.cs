using Discord;

namespace wow2.Verbose.Messages
{
    public class SuccessMessage : Message
    {
        public SuccessMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowsuccess:{SuccessEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = Color.Green,
            };
        }
    }
}