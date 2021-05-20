using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Data;
using wow2.Extentions;
using wow2.Verbose.Messages;

namespace wow2.Modules.Keywords
{
    public class ResponseMessage : GenericMessage
    {
        public static readonly IEmote DeleteReactionEmote = new Emoji("🗑");
        public static readonly IEmote LikeReactionEmote = new Emoji("👍");

        private const int MaxCountOfRememberedKeywordResponses = 50;

        public ResponseMessage(KeywordValue keywordValue)
             : base(string.Empty)
        {
            AllowMentions = false;

            KeywordValue = keywordValue;
            EmbedBuilder.Description = KeywordValue.Content;
            EmbedBuilder.Title = KeywordValue.Title;
        }

        public KeywordValue KeywordValue { get; }

        public async Task<IUserMessage> RespondToMessageAsync(SocketMessage message)
        {
            IGuild guild = message.GetGuild();
            var config = KeywordsModule.GetConfigForGuild(guild);
            ReplyToMessageId = message.Id;

            // Don't use embed message if the value to send contains a link.
            if (KeywordValue.Content.Contains("http://") || KeywordValue.Content.Contains("https://"))
            {
                await SendAsPlainTextAsync(message.Channel);
            }
            else
            {
                await SendAsync(message.Channel);
            }

            if (config.IsLikeReactionOn)
                await SentMessage.AddReactionAsync(LikeReactionEmote);
            if (config.IsDeleteReactionOn)
                await SentMessage.AddReactionAsync(DeleteReactionEmote);

            // Remember the messages that are actually keyword responses by adding them to a list.
            config.ListOfResponsesId.Add(SentMessage.Id);

            // Remove the oldest message if ListOfResponsesId has reached its max.
            if (config.ListOfResponsesId.Count > MaxCountOfRememberedKeywordResponses)
                config.ListOfResponsesId.RemoveAt(0);

            await DataManager.SaveGuildDataToFileAsync(guild.Id);
            return SentMessage;
        }
    }
}