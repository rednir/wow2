using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Events
{
    public class EventMessage : Message
    {
        public static readonly IEmote AttendEmote = new Emoji("👌");
        public static readonly IEmote EditEmote = new Emoji("✏");

        public EventMessage(Event @event)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = @event.Description,
            };
        }

        public static async Task<bool> ActOnReactionAsync(SocketReaction reaction)
        {
            EventsModuleConfig config = DataManager.AllGuildData[reaction.Channel.GetGuild().Id].Events;
            return false;
            //QuestionMessage message = config.

            /*if (message == null)
                return false;

            if (reaction.Emote.Name == ConfirmEmote.Name)
            {
                await message.OnConfirm.Invoke();
            }
            else if (reaction.Emote.Name == DenyEmote.Name)
            {
                await message.OnDeny.Invoke();
            }
            else
            {
                return false;
            }

            guildData.QuestionMessages.Remove(message);
            await message.SentMessage.RemoveAllReactionsAsync();

            return true;*/
        }

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            IUserMessage message = await base.SendAsync(channel);
            List<EventMessage> eventMessages = DataManager.AllGuildData[message.GetGuild().Id].Events.EventMessages;

            eventMessages.Truncate(30);
            eventMessages.Add(this);

            await message.AddReactionsAsync(
                new IEmote[] { AttendEmote, EditEmote });

            return message;
        }
    }
}