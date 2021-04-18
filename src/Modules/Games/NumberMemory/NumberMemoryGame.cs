using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using wow2.Verbose.Messages;
using wow2.Extentions;
using wow2.Data;

namespace wow2.Modules.Games.NumberMemory
{
    public class NumberMemoryGame
    {
        /// <returns>True if the message was related to the game.</returns>
        public static async Task<bool> CheckMessageAsync(SocketMessage receivedMessage)
        {
            var config = GetConfigForGuild(receivedMessage.GetGuild());
            return false;
        }

        public static async Task StartGame(SocketCommandContext context)
        {
            var config = GetConfigForGuild(context.Guild);
        }

        private static async Task EndGameAsync(NumberMemoryConfig config)
        {
        }

        private static NumberMemoryConfig GetConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Games.NumberMemory;
    }
}