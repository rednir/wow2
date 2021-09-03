using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Keywords
{
    public class ResponseMessage : GenericMessage
    {
        public static readonly IEmote DeleteReactionEmote = new Emoji("🗑");
        public static readonly IEmote LikeReactionEmote = new Emoji("👍");

        private const int MaxCountOfRememberedKeywordResponses = 100;

        public ResponseMessage()
            : base(string.Empty)
        {
        }

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

        /// <summary>Checks if a message was a keyword response sent by the bot, and acts on the button press if so.</summary>
        public static async Task<bool> ActOnButtonAsync(SocketMessageComponent component)
        {
            // todo: do i still want to use reactions here?
            var config = DataManager.AllGuildData[component.Channel.GetGuild().Id].Keywords;
            return false;

            // ResponseMessage responseMessage = config.ListOfResponseMessages.Find(
            //     m => m.SentMessage?.Id == component.Message.Id);
            // if (responseMessage == null)
            //     return false;

            // if (component.Data.CustomId == DeleteReactionEmote.Name && config.IsDeleteReactionOn)
            // {
            //     responseMessage.KeywordValue.TimesDeleted++;
            //     config.ListOfResponseMessages.Remove(responseMessage);
            //     await responseMessage.SentMessage.DeleteAsync();
            // }
            // else if (component.Data.CustomId == LikeReactionEmote.Name && config.IsLikeReactionOn)
            // {
            //     if (!responseMessage.UsersLikedIds.Contains(component.User.Id))
            //     {
            //         responseMessage.UsersLikedIds.Add(component.User.Id);
            //         responseMessage.KeywordValue.TimesLiked++;
            //     }
            // }
            // else
            // {
            //     return false;
            // }

            // await DataManager.SaveGuildDataToFileAsync(responseMessage.SentMessage.GetGuild().Id);
            // return true;
        }

        /// <summary>Checks if a message was a keyword response sent by the bot, and acts on the removed reaction if so.</summary>
        public static bool ActOnReactionRemoved(SocketReaction reaction)
        {
            var config = DataManager.AllGuildData[reaction.Channel.GetGuild().Id].Keywords;

            ResponseMessage responseMessage = config.ListOfResponseMessages.Find(
                m => m.SentMessage?.Id == reaction.MessageId);
            if (responseMessage == null)
                return false;

            if (reaction.Emote.Name == LikeReactionEmote.Name
                && config.IsLikeReactionOn
                && responseMessage.UsersLikedIds.Remove(reaction.UserId))
            {
                responseMessage.KeywordValue.TimesLiked--;
                return true;
            }

            return false;
        }

        public async Task<IUserMessage> RespondToMessageAsync(SocketMessage message)
        {
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
            config.ListOfResponseMessages.Truncate(MaxCountOfRememberedKeywordResponses);

            return SentMessage;
        }
    }
}