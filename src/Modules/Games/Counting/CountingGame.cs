using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Data;
using wow2.Extentions;
using wow2.Verbose.Messages;

namespace wow2.Modules.Games.Counting
{
    public static class CountingGame
    {
        public static async Task StartGame(SocketCommandContext context, float increment)
        {
            var config = GetConfigForGuild(context.Guild);

            config.InitalContext = context;
            config.Increment = increment;
            config.NextNumber = increment;
            config.ListOfMessages = new List<SocketMessage>();
            config.IsGameStarted = true;

            if (await EndGameIfNumberIsInvalidAsync(context.Message, increment))
                return;

            await new InfoMessage($"Counting has started.\nTo start off, type the number `{config.NextNumber}` in this channel.")
                .SendAsync(context.Channel);
        }

        /// <returns>True if the message was related to the game.</returns>
        public static async Task<bool> CheckMessageAsync(SocketMessage receivedMessage)
        {
            var config = GetConfigForGuild(receivedMessage.GetGuild());
            float userNumber;

            if (receivedMessage.Author.IsBot ||
                !config.IsGameStarted ||
                config.InitalContext.Channel != receivedMessage.Channel)
            {
                return false;
            }

            try
            {
                userNumber = Convert.ToSingle(receivedMessage.Content);
            }
            catch
            {
                return false;
            }

            // If this is the first counting message, there is no need to check if a user counts twice in a row.
            if (config.ListOfMessages.Count != 0)
            {
                if (receivedMessage.Author == config.ListOfMessages.Last().Author)
                {
                    await new WarningMessage("Counting twice in a row is no fun.")
                        .SendAsync(receivedMessage.Channel);
                    return true;
                }
            }

            config.ListOfMessages.Add(receivedMessage);
            if (userNumber == config.NextNumber)
            {
                config.NextNumber += config.Increment;
                await receivedMessage.AddReactionAsync(new Emoji("✅"));
                await EndGameIfNumberIsInvalidAsync(receivedMessage, config.NextNumber);
            }
            else
            {
                await receivedMessage.AddReactionAsync(new Emoji("❎"));
                await new InfoMessage($"Counting was ruined by {receivedMessage.Author.Mention}. Nice one.\nThe next number should have been `{config.NextNumber}`")
                    .SendAsync(receivedMessage.Channel);
                await EndGameAsync(config);
            }

            return true;
        }

        private static async Task EndGameAsync(CountingGameConfig config)
        {
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
                    IsInline = true,
                };
                listOfFieldBuilders.Add(fieldBuilderForParticipant);
            }

            string commentOnFinalNumber;
            float absNextNumber = Math.Abs((float)config.NextNumber);
            float absIncrement = Math.Abs((float)config.Increment);

            if (absNextNumber < 3 * absIncrement)
                commentOnFinalNumber = "Pathetic.";
            else if (absNextNumber < 25 * absIncrement)
                commentOnFinalNumber = "There's plenty room for improvement.";
            else if (absNextNumber < 75 * absIncrement)
                commentOnFinalNumber = "Not bad!";
            else if (absNextNumber >= 75 * absIncrement)
                commentOnFinalNumber = "Amazing!";
            else
                commentOnFinalNumber = string.Empty;

            await new PagedMessage(
                fieldBuilders: listOfFieldBuilders,
                title: "📈 Final Stats",
                description: $"*You counted up to* `{config.NextNumber - config.Increment}`\n*{commentOnFinalNumber}*")
                    .SendAsync((ISocketMessageChannel)config.InitalContext.Channel);

            config.IsGameStarted = false;
        }

        /// <returns>True if counting was ended, otherwise false.</returns>
        private static async Task<bool> EndGameIfNumberIsInvalidAsync(SocketMessage message, float? number)
        {
            if (number >= float.MaxValue || number <= float.MinValue)
            {
                await new WarningMessage("Woah, that's a big number.\nHate to be a killjoy, but even a computer has its limits.")
                    .SendAsync(message.Channel);
                await EndGameAsync(GetConfigForGuild(message.GetGuild()));
                return true;
            }

            return false;
        }

        public static CountingGameConfig GetConfigForGuild(IGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Games.Counting;
    }
}