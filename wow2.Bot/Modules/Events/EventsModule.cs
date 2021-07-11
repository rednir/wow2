using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Events
{
    [Name("Events")]
    [Group("events")]
    [Alias("event")]
    [Summary("Create and organise events for you and your friends.")]
    public class EventsModule : Module
    {
        private EventsModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Events;

        [Command("new")]
        [Alias("start", "create", "add")]
        [Summary("Create a new event.")]
        public async Task NewAsync([Remainder] string description = "untitled event")
        {
            if (Config.AnnouncementsChannelId == default)
            {
                throw new CommandReturnException(
                    Context,
                    $"You can do this be using `{Context.Guild.GetCommandPrefix()} events set-announcements-channel`",
                    "Set an announcements channel first");
            }

            await new DateTimeSelectorMessage(
                async d =>
                {
                    var @event = new Event()
                    {
                        Description = description,
                        ForDateTime = d,
                        CreatedByMention = Context.User.Mention,
                    };

                    Config.Events.Add(@event);
                    await new EventMessage(@event)
                        .SendAsync((IMessageChannel)BotService.Client.GetChannel(Config.AnnouncementsChannelId));
                },
                "Select a date and time for this event.")
                    .SendAsync(Context.Channel);
        }

        [Command("delete")]
        [Alias("remove", "stop")]
        [Summary("Delete an upcoming event.")]
        public async Task DeleteAsync()
        {
            throw new NotImplementedException();
        }

        [Command("set-announcements-channel")]
        [Alias("set-channel", "announcements-channel", "channel")]
        [Summary("Sets the channel that event notifications will be sent to.")]
        public async Task SetAnnouncementsChannelAsync(SocketTextChannel channel)
        {
            Config.AnnouncementsChannelId = channel.Id;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);

            await new SuccessMessage($"You'll get notified about events in {channel.Mention}")
                .SendAsync(Context.Channel);
        }
    }
}