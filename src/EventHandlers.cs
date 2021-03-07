using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.UserCommands;

namespace wow2
{
    public static class EventHandlers
    {
        public static string CommandIdentifier = "!wow2";

        public static async Task LogAsync(LogMessage logMessage)
        {
            await Task.Run(() => Console.WriteLine($"Log: [{logMessage.Source}: {logMessage.Severity}] {logMessage.Message}"));
        }

        public static async Task MessageRecievedAsync(SocketMessage recievedMessage)
        {
            // TODO: check self id instead
            if (recievedMessage.Author.IsBot) return;

            await DataManager.SaveGuildDataToFile(((SocketGuildChannel)recievedMessage.Channel).Guild.Id, new GuildData());
            //await recievedMessage.Channel.SendMessageAsync($"{DataManager.DictionaryOfGuildData[((SocketGuildChannel)recievedMessage.Channel).Id].Keywords.Count}");

            await Keywords.CheckMessageForKeyword(recievedMessage);

            if (recievedMessage.Content.StartsWith(CommandIdentifier))
            {
                // TODO: proper way of doing commands
            }
        }
    }
}