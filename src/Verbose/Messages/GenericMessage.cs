using Discord;
using wow2.Extentions;

namespace wow2.Verbose.Messages
{
    /// <summary>Basic message, nothing special.</summary>
    public class GenericMessage : Message
    {
        public GenericMessage(string description, string title = "")
        {
            const int maxDescriptionLength = 2048;
            if (description.Length >= maxDescriptionLength)
            {
                // The description will be uploaded as a file.
                DescriptionAsStream = description.ToMemoryStream();
                description = "[DELETED]";
            }

            EmbedBuilder = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = Color.LightGrey,
            };
        }
    }
}