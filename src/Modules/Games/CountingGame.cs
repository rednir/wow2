using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using wow2.Verbose;
using wow2.Extentions;
using wow2.Data;

namespace wow2.Modules.Games
{
    public class CountingGame
    {
        public static async Task StartGame(SocketCommandContext context, float increment)
        {
            var config = DataManager.GetGamesConfigForGuild(context.Guild).Counting;

            config.InitalContext = context;
            config.Increment = increment;
            config.NextNumber = increment;
            config.ListOfMessages = new List<SocketMessage>();

            if (await EndGameIfNumberIsInvalidAsync(context.Message, increment))
                return;

            await GenericMessenger.SendInfoAsync(context.Channel, $"Counting has started.\nTo start off, type the number `{config.NextNumber}` in this channel.");
        }

        public static async Task CheckMessageAsync(SocketMessage recievedMessage)
        {
            var config = DataManager.GetGamesConfigForGuild(recievedMessage.GetGuild()).Counting;
            float userNumber;

            if (recievedMessage.Author.IsBot || config.NextNumber == null || config.InitalContext.Channel != recievedMessage.Channel) return;

            try { userNumber = Convert.ToSingle(recievedMessage.Content); }
            catch { return; }

            // If this is the first counting message, there is no need to check if a user counts twice in a row.
            if (config.ListOfMessages.Count != 0)
            {
                if (recievedMessage.Author == config.ListOfMessages.Last().Author)
                {
                    await GenericMessenger.SendWarningAsync(recievedMessage.Channel, "Counting twice in a row is no fun.");
                    return;
                }
            }

            config.ListOfMessages.Add(recievedMessage);
            if (userNumber == config.NextNumber)
            {
                config.NextNumber += config.Increment;
                await recievedMessage.AddReactionAsync(new Emoji("✅"));
                await EndGameIfNumberIsInvalidAsync(recievedMessage, config.NextNumber);
            }
            else
            {
                await recievedMessage.AddReactionAsync(new Emoji("❎"));
                await GenericMessenger.SendInfoAsync(recievedMessage.Channel, $"Counting was ruined by {recievedMessage.Author.Mention}. Nice one.\nThe next number should have been `{config.NextNumber}`");
                await EndGameAsync(recievedMessage);
            }
        }

        private static async Task EndGameAsync(SocketMessage finalMessage)
        {
            var config = DataManager.GetGamesConfigForGuild(finalMessage.GetGuild()).Counting;
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            var dictionaryOfParticipants = new Dictionary<SocketUser, int>();

            // Get the participants and how many messages they sent
            foreach (SocketMessage message in config.ListOfMessages)
            {
                dictionaryOfParticipants.TryAdd(message.Author, 0);
                dictionaryOfParticipants[message.Author]++;
            }

            // Build fields for each participant.
            foreach (KeyValuePair<SocketUser, int> participant in dictionaryOfParticipants)
            {
                var fieldBuilderForParticipant = new EmbedFieldBuilder()
                {
                    Name = participant.Key.Username,
                    Value = $"{participant.Value} messages ({Math.Round((float)participant.Value / (float)dictionaryOfParticipants.Values.Sum() * 100f)}% helpful)",
                    IsInline = true
                };
                listOfFieldBuilders.Add(fieldBuilderForParticipant);
            }

            string commentOnFinalNumber;
            float absNextNumber = Math.Abs((float)config.NextNumber);
            float absIncrement = Math.Abs((float)config.Increment);

            if (absNextNumber < 3 * absIncrement) commentOnFinalNumber = "Pathetic.";
            else if (absNextNumber < 25 * absIncrement) commentOnFinalNumber = "There's plenty room for improvement.";
            else if (config.NextNumber < 75 * absIncrement) commentOnFinalNumber = "Not bad!";
            else if (absNextNumber >= 75 * absIncrement) commentOnFinalNumber = "Amazing!";
            else commentOnFinalNumber = "";

            await GenericMessenger.SendResponseAsync(
                channel: finalMessage.Channel,
                fieldBuilders: listOfFieldBuilders,
                title: "Final Stats",
                description: $"*You counted up to* `{config.NextNumber - config.Increment}`\n*{commentOnFinalNumber}*");

            config.NextNumber = null;
        }

        /// <returns>True if counting was ended, otherwise false</returns>
        private static async Task<bool> EndGameIfNumberIsInvalidAsync(SocketMessage message, float? number)
        {
            if (number >= float.MaxValue || number <= float.MinValue)
            {
                await GenericMessenger.SendWarningAsync(message.Channel, "Woah, that's a big number.\nHate to be a killjoy, but even a computer has its limits.");
                await EndGameAsync(message);
                return true;
            }
            return false;
        }
    }
}