using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Data;
using wow2.Extensions;
using wow2.Verbose.Messages;

namespace wow2.Modules.Games.NumberMemory
{
    public static class NumberMemoryGame
    {
        public static NumberMemoryConfig GetConfig(IGuild guild)
            => DataManager.AllGuildData[guild.Id].Games.NumberMemory;

        /// <summary>Checks whether a user message is part of the number memory game, and acts on it if so.</summary>
        /// <returns>True if the message was related to the game.</returns>
        public static async Task<bool> CheckMessageAsync(SocketMessage receivedMessage)
        {
            var config = GetConfig(receivedMessage.GetGuild());
            return false;
        }

        public static async Task StartGame(SocketCommandContext context)
        {
            throw new NotImplementedException();

            var config = GetConfig(context.Guild);

            config.IsGameStarted = true;
            config.HighestNumberOfDigits = 0;

            await new InfoMessage(
                description: "When the number disappears, try retype the number. Don't start typing before the message gets deleted!",
                title: $"Number memory has started for {context.User.Mention}")
                    .SendAsync(context.Channel);
        }

        private static async Task EndGameAsync(NumberMemoryConfig config)
        {
            config.IsGameStarted = false;
        }
    }
}