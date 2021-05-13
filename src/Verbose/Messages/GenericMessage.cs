using System.Threading.Tasks;
using System.IO;
using Discord;
using wow2.Extentions;

namespace wow2.Verbose.Messages
{
    /// <summary>Basic message, nothing special.</summary>
    public class GenericMessage : Message
    {
        protected MemoryStream DescriptionAsStream;

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

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            if (DescriptionAsStream != null)
            {
                return await channel.SendFileAsync(
                    stream: DescriptionAsStream,
                    filename: $"{Embed?.Title}_desc.txt",
                    embed: new WarningMessage("A message was too long, so it was uploaded as a file.").Embed,
                    messageReference: MessageReference);
            }
            else
            {
                return await base.SendAsync(channel);
            }
        }
    }
}