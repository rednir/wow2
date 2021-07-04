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
            Event = @event;
        }

        public Event Event { get; set; }

        public static async Task<bool> ActOnReactionAddedAsync(SocketReaction reaction)
        {
            EventsModuleConfig config = DataManager.AllGuildData[reaction.Channel.GetGuild().Id].Events;
            EventMessage message = config.EventMessages.Find(m => m.SentMessage.Id == reaction.MessageId);

            if (message == null)
                return false;

            if (reaction.Emote.Name == AttendEmote.Name)
            {
                message.Event.AttendeeMentions.Add(reaction.User.Value.Mention);
                await message.UpdateMessageAsync();
            }
            else if (reaction.Emote.Name == EditEmote.Name)
            {
                IUserMessage sentMessage = await new DateTimeSelectorMessage(
                    async d =>
                    {
                        message.Event.ForDateTime = d;
                        await message.UpdateMessageAsync();
                    },
                    "Select the new date and time for this event.")
                        .SendAsync(reaction.Channel);

                await message.SentMessage.RemoveReactionAsync(EditEmote, reaction.User.Value);
            }
            else
            {
                return false;
            }

            return true;
        }

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            await UpdateMessageAsync();

            IUserMessage message = await base.SendAsync(channel);
            List<EventMessage> eventMessages = DataManager.AllGuildData[message.GetGuild().Id].Events.EventMessages;

            eventMessages.Truncate(30);
            eventMessages.Add(this);

            await message.AddReactionsAsync(
                new IEmote[] { AttendEmote, EditEmote });

            return message;
        }

        public async Task UpdateMessageAsync()
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Title = $"📋 {Event.Description}",
                Description = $"{Event.AttendeeMentions.Count} people are attending on `{Event.ForDateTime}`",
                Footer = new EmbedFooterBuilder()
                {
                    Text = $"Want to attend this event? React with {AttendEmote.Name}",
                },
                Color = Color.LightGrey,
            };

            if (SentMessage != null)
                await SentMessage.ModifyAsync(m => m.Embed = Embed);
        }
    }
}