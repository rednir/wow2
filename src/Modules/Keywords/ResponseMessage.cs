using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Data;
using wow2.Extensions;
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

        /// <summary>Gets a list of IDs of users who have previously given a like to the KeywordValue via this message.</summary>
        private List<ulong> UsersLikedIds { get; } = new();

        /// <summary>Checks if a message was a keyword response sent by the bot, and acts on the reaction if so.</summary>
        public static async Task<bool> ActOnReactionAsync(SocketReaction reaction, IUserMessage message)
        {
            var config = DataManager.AllGuildData[message.GetGuild().Id].Keywords;

            ResponseMessage responseMessage = config.ListOfResponseMessages.Find(
                m => m.SentMessage?.Id == message.Id);
            if (responseMessage == null)
                return false;

            if (reaction.Emote.Name == DeleteReactionEmote.Name && config.IsDeleteReactionOn)
            {
                responseMessage.KeywordValue.TimesDeleted++;
                config.ListOfResponseMessages.Remove(responseMessage);
                await responseMessage.SentMessage.DeleteAsync();
            }
            else if (reaction.Emote.Name == LikeReactionEmote.Name && config.IsLikeReactionOn)
            {
                if (!responseMessage.UsersLikedIds.Contains(reaction.UserId))
                {
                    responseMessage.UsersLikedIds.Add(reaction.UserId);
                    responseMessage.KeywordValue.TimesLiked++;
                }
            }
            else
            {
                return false;
            }

            await DataManager.SaveGuildDataToFileAsync(responseMessage.SentMessage.GetGuild().Id);
            return true;
        }

        public async Task<IUserMessage> RespondToMessageAsync(SocketMessage message)
        {
            IGuild guild = message.GetGuild();
            var config = DataManager.AllGuildData[message.GetGuild().Id].Keywords;
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
            config.ListOfResponseMessages.Add(this);

            // Remove the oldest message if ListOfResponsesId has reached its max.
            if (config.ListOfResponseMessages.Count > MaxCountOfRememberedKeywordResponses)
                config.ListOfResponseMessages.RemoveAt(0);

            await DataManager.SaveGuildDataToFileAsync(guild.Id);
            return SentMessage;
        }
    }
}