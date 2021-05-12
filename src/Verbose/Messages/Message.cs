using System.IO;
using System.Threading.Tasks;
using Discord;

namespace wow2.Verbose.Messages
{
    /// <summary>Base class for sending and building embed messages.</summary>
    public abstract class Message
    {
        public const ulong SuccessEmoteId = 823595458978512997;
        public const ulong InfoEmoteId = 804732580423008297;
        public const ulong WarningEmoteId = 804732632751407174;
        public const ulong ErrorEmoteId = 804732656721199144;

        public ulong ReplyToMessageId { get; set; }
        public IUserMessage SentMessage { get; protected set; }
        public Embed Embed
        {
            get { return EmbedBuilder.Build(); }
        }

        protected EmbedBuilder EmbedBuilder;
        protected MemoryStream DescriptionAsStream;

        public virtual async Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            var reference = ReplyToMessageId != 0 ? new MessageReference(ReplyToMessageId) : null;

            if (DescriptionAsStream != null)
            {
                SentMessage = await channel.SendFileAsync(
                    stream: DescriptionAsStream,
                    filename: $"{Embed?.Title}_desc.txt",
                    embed: new WarningMessage("A message was too long, so it was uploaded as a file.").Embed,
                    messageReference: reference);
            }
            else
            {
                SentMessage = await channel.SendMessageAsync(
                    embed: EmbedBuilder.Build(),
                    allowedMentions: AllowedMentions.None,
                    messageReference: reference);
            }
            return SentMessage;
        }

        protected static string GetStatusMessageFormattedDescription(string description, string title)
            => $"{(title == null ? null : $"**{title}**\n")}{description}";
    }
}