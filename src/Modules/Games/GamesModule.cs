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
    [Summary("Some games idk")]
    public class GamesModule : ModuleBase<SocketCommandContext>
    {
        public async Task MessageRecievedForCountingAsync(SocketMessage recievedMessage)
        {
            if (recievedMessage.Author.IsBot) return;

            var config = DataManager.GetGamesConfigForGuild(Context.Guild);
            float userNumber;

            try { userNumber = Convert.ToSingle(recievedMessage.Content); }
            catch { return; }

            // If this is the first counting message, there is no need to check if a user counts twice in a row.
            if (config.CountingListOfMessages.Count != 0)
            {
                if (recievedMessage.Author == config.CountingListOfMessages.Last().Author)
                {
                    await ReplyAsync(
                        embed: MessageEmbedPresets.Verbose("Counting twice in a row is no fun.", VerboseMessageSeverity.Warning)
                    );
                    return;
                }
            }

            config.CountingListOfMessages.Add(recievedMessage);
            if (userNumber == config.CountingNextNumber)
            {
                config.CountingNextNumber += config.CountingIncrement;
                await recievedMessage.AddReactionAsync(new Emoji("✅"));
            }
            else
            {
                await recievedMessage.AddReactionAsync(new Emoji("❎"));
                await ReplyAsync(
                    embed: MessageEmbedPresets.Verbose($"Counting was ruined by {recievedMessage.Author.Mention}. Nice one.\nThe next number should have been `{config.CountingNextNumber}`")
                );
                await EndCounting();
            }
        }

        [Command("counting")]
        [Alias("count")]
        public async Task CountingAsync(float increment = 1)
        {
            var config = DataManager.GetGamesConfigForGuild(Context.Guild);

            config.CountingChannel = Context.Channel;
            config.CountingIncrement = increment;
            config.CountingNextNumber = increment;
            config.CountingListOfMessages = new List<SocketMessage>();

            Program.Client.MessageReceived += MessageRecievedForCountingAsync;

            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"Counting has started.\nTo start off, type the number `{config.CountingNextNumber}` in this channel.")
            );
        }

        private async Task EndCounting()
        {
            Program.Client.MessageReceived -= MessageRecievedForCountingAsync;

            var config = DataManager.GetGamesConfigForGuild(Context.Guild);
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            var dictionaryOfParticipants = new Dictionary<SocketUser, int>();

            // Get the participants and how many messages they sent
            foreach (SocketMessage message in config.CountingListOfMessages)
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
            if (config.CountingNextNumber < 3 * config.CountingIncrement) commentOnFinalNumber = "Pathetic.";
            else if (config.CountingNextNumber < 25 * config.CountingIncrement) commentOnFinalNumber = "There's plenty room for improvement.";
            else if (config.CountingNextNumber < 75 * config.CountingIncrement) commentOnFinalNumber = "Not bad!";
            else commentOnFinalNumber = "Amazing!";

            await ReplyAsync(
                embed: MessageEmbedPresets.Fields(listOfFieldBuilders, "Final Stats", $"*You counted up to {config.CountingNextNumber - config.CountingIncrement}. {commentOnFinalNumber}*")
            );
        }
    }
}