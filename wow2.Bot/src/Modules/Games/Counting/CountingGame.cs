using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games.Counting
{
    public static class CountingGame
    {
        public static async Task StartGame(SocketCommandContext context, BotService botService, float increment)
        {
            var config = botService.Data.AllGuildData[context.Guild.Id].Games.Counting;

            config.InitalContext = context;
            config.Increment = increment;
            config.NextNumber = increment;
            config.ListOfMessages = new List<SocketMessage>();
            config.IsGameStarted = true;

            if (await EndGameIfNumberIsInvalidAsync(context, botService, increment))
                return;

            await new InfoMessage($"Counting has started.\nTo start off, type the number `{config.NextNumber}` in this channel.")
                .SendAsync(context.Channel);
        }

        /// <summary>Checks whether a user message is part of the counting game, and acts on it if so.</summary>
        /// <returns>True if the message was related to the game.</returns>
        public static async Task<bool> CheckMessageAsync(SocketCommandContext context, BotService botService)
        {
            var config = botService.Data.AllGuildData[context.Guild.Id].Games.Counting;
            float userNumber;

            if (context.User.IsBot ||
                !config.IsGameStarted ||
                config.InitalContext.Channel != context.Channel)
            {
                return false;
            }

            try
            {
                userNumber = Convert.ToSingle(context.Message.Content);
            }
            catch
            {
                return false;
            }

            // If this is the first counting message, there is no need to check if a user counts twice in a row.
            if (config.ListOfMessages.Count != 0)
            {
                if (context.User == config.ListOfMessages.Last().Author)
                {
                    await new WarningMessage("Counting twice in a row is no fun.")
                        .SendAsync(context.Channel);
                    return true;
                }
            }

            config.ListOfMessages.Add(context.Message);
            if (userNumber == config.NextNumber)
            {
                config.NextNumber += config.Increment;
                await context.Message.AddReactionAsync(new Emoji("✅"));
                await EndGameIfNumberIsInvalidAsync(context, botService, config.NextNumber);
            }
            else
            {
                await context.Message.AddReactionAsync(new Emoji("❎"));
                await new InfoMessage($"Counting was ruined by {context.Message.Author.Mention}. Nice one.\nThe next number should have been `{config.NextNumber}`")
                    .SendAsync(context.Channel);
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
        private static async Task<bool> EndGameIfNumberIsInvalidAsync(SocketCommandContext context, BotService botService, float? number)
        {
            if (number >= float.MaxValue || number <= float.MinValue)
            {
                await new WarningMessage("Woah, that's a big number.\nHate to be a killjoy, but even a computer has its limits.")
                    .SendAsync(context.Channel);
                await EndGameAsync(botService.Data.AllGuildData[context.Guild.Id].Games.Counting);
                return true;
            }

            return false;
        }
    }
}