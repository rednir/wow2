using Discord;

namespace wow2.Verbose.Messages
{
    public class ErrorMessage : Message
    {
        public ErrorMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Title = $"{new Emoji($"<:wowerror:{ErrorEmoteId}>")} Something bad happened...",
                Description = GetStatusMessageFormattedDescription(description, title),
                Color = Color.Red
            };
        }
    }
}