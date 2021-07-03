using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
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
        public async Task NewAsync()
        {
            throw new NotImplementedException();
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