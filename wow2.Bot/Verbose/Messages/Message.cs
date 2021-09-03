using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    /// <summary>Base class for sending and building embed messages.</summary>
    public abstract class Message
    {
        public const ulong SuccessEmoteId = 858009017950404608;
        public const ulong InfoEmoteId = 858010084868227082;
        public const ulong WarningEmoteId = 858010661091147776;
        public const ulong ErrorEmoteId = 858011314022121482;
        public const ulong QuestionEmoteId = 858340474627686431;

        public ulong ReplyToMessageId { get; set; }

        public bool AllowMentions { get; set; }

        [JsonIgnore]
        public string Text { get; set; }

        [JsonIgnore]
        public IUserMessage SentMessage { get; protected set; }

        [JsonIgnore]
        public Embed Embed => EmbedBuilder.Build();

        [JsonIgnore]
        public ComponentBuilder Components { get; set; }

        [JsonIgnore]
        public MessageReference MessageReference =>
            ReplyToMessageId != 0 ? new MessageReference(ReplyToMessageId) : null;

        [JsonIgnore]
        protected EmbedBuilder EmbedBuilder { get; set; }

        public virtual async Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            SentMessage = await channel.SendMessageAsync(
                text: Text,
                embed: EmbedBuilder?.Build(),
                allowedMentions: AllowMentions ? AllowedMentions.All : AllowedMentions.None,
                messageReference: MessageReference,
                component: Components?.Build());
            return SentMessage;
        }

        public virtual async Task<IUserMessage> SendAsPlainTextAsync(IMessageChannel channel)
        {
            SentMessage = await channel.SendMessageAsync(
                text: (!string.IsNullOrWhiteSpace(EmbedBuilder.Title) ? $"**{EmbedBuilder.Title}**\n\n" : null) + EmbedBuilder.Description,
                allowedMentions: AllowMentions ? AllowedMentions.All : AllowedMentions.None,
                messageReference: MessageReference,
                component: Components?.Build());
            return SentMessage;
        }

        protected static string GetStatusMessageFormattedDescription(string description, string title)
            => (title == null ? null : $"**{title}**\n") + description;
    }
}