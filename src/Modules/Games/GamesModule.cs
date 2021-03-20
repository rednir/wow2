using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ExtentionMethods;

namespace wow2.Modules.Games
{
    [Name("Games")]
    [Group("games")]
    [Alias("game")]
    [Summary("For having a bit of fun.")]
    public class GamesModule : ModuleBase<SocketCommandContext>
    {
        public static async Task CheckMessageIsCountingAsync(SocketMessage recievedMessage)
        {
            var config = DataManager.GetGamesConfigForGuild(recievedMessage.GetGuild()).Counting;
            float userNumber;

            if (recievedMessage.Author.IsBot || config.NextNumber == null || config.Channel != recievedMessage.Channel) return;

            try { userNumber = Convert.ToSingle(recievedMessage.Content); }
            catch { return; }

            // If this is the first counting message, there is no need to check if a user counts twice in a row.
            if (config.ListOfMessages.Count != 0)
            {
                if (recievedMessage.Author == config.ListOfMessages.Last().Author)
                {
                    await recievedMessage.Channel.SendMessageAsync(
                        embed: MessageEmbedPresets.Verbose("Counting twice in a row is no fun.", VerboseMessageSeverity.Warning)
                    );
                    return;
                }
            }

            config.ListOfMessages.Add(recievedMessage);
            if (userNumber == config.NextNumber)
            {
                config.NextNumber += config.Increment;
                await recievedMessage.AddReactionAsync(new Emoji("✅"));
                await EndCountingIfNumberIsInvalidAsync(recievedMessage, config.NextNumber);
            }
            else
            {
                await recievedMessage.AddReactionAsync(new Emoji("❎"));
                await recievedMessage.Channel.SendMessageAsync(
                    embed: MessageEmbedPresets.Verbose($"Counting was ruined by {recievedMessage.Author.Mention}. Nice one.\nThe next number should have been `{config.NextNumber}`")
                );
                await EndCountingAsync(recievedMessage);
            }
        }

        [Command("counting")]
        [Alias("count")]
        public async Task CountingAsync(float increment = 1)
        {
            var config = DataManager.GetGamesConfigForGuild(Context.Guild).Counting;

            config.Channel = Context.Channel;
            config.Increment = increment;
            config.NextNumber = increment;
            config.ListOfMessages = new List<SocketMessage>();

            if (await EndCountingIfNumberIsInvalidAsync(Context.Message, increment))
                return;

            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"Counting has started.\nTo start off, type the number `{config.NextNumber}` in this channel.")
            );
        }

        private static async Task EndCountingAsync(SocketMessage finalMessage)
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
            if (config.NextNumber < 3 * config.Increment) commentOnFinalNumber = "Pathetic.";
            else if (config.NextNumber < 25 * config.Increment) commentOnFinalNumber = "There's plenty room for improvement.";
            else if (config.NextNumber < 75 * config.Increment) commentOnFinalNumber = "Not bad!";
            else if (config.NextNumber >= 75 * config.Increment) commentOnFinalNumber = "Amazing!";
            else commentOnFinalNumber = "";

            await finalMessage.Channel.SendMessageAsync(
                embed: MessageEmbedPresets.Fields(listOfFieldBuilders, "Final Stats", $"*You counted up to* `{config.NextNumber - config.Increment}`\n*{commentOnFinalNumber}*")
            );

            config.NextNumber = null;
        }

        /// <returns>True if counting was ended, otherwise false</returns>
        private static async Task<bool> EndCountingIfNumberIsInvalidAsync(SocketMessage message, float? number)
        {
            if (number >= float.MaxValue || number <= float.MinValue)
            {
                await message.Channel.SendMessageAsync(
                    embed: MessageEmbedPresets.Verbose("Woah, that's a big number.\nHate to be a killjoy, but even a computer has its limits.", VerboseMessageSeverity.Warning)
                );
                await EndCountingAsync(message);
                return true;
            }
            return false;
        }
    }
}