using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ExtentionMethods;

namespace wow2.Modules
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

            var config = DataManager.GetGamesConfigForGuild(Context.Message.GetGuild());
            int userNumber;

            try { userNumber = Convert.ToInt32(recievedMessage.Content); }
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
                config.CountingNextNumber++;
                await recievedMessage.AddReactionAsync(new Emoji("✅"));
            }
            else
            {
                await recievedMessage.AddReactionAsync(new Emoji("❎"));
                await ReplyAsync(
                    embed: MessageEmbedPresets.Verbose($"Counting was ruined by {recievedMessage.Author.Mention}. Nice one.")
                );
                await EndCounting();
            }
        }

        [Command("counting")]
        public async Task CountingAsync()
        {
            var config = DataManager.GetGamesConfigForGuild(Context.Message.GetGuild());

            config.CountingChannel = Context.Channel;
            config.CountingNextNumber = 1;
            config.CountingListOfMessages = new List<SocketMessage>();

            Program.Client.MessageReceived += MessageRecievedForCountingAsync;

            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"Counting has started.\nTo start off, type the number `{config.CountingNextNumber}` in this channel.", VerboseMessageSeverity.Info)
            );
        }

        private async Task EndCounting()
        {
            var config = DataManager.GetGamesConfigForGuild(Context.Message.GetGuild());
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            var dictionaryOfParticipants = new Dictionary<SocketUser, int>();

            // Get the participants and how many messages they sent
            foreach (SocketMessage message in config.CountingListOfMessages)
            {
                dictionaryOfParticipants.TryAdd(message.Author, 0);
                dictionaryOfParticipants[message.Author]++;
            }

            foreach (KeyValuePair<SocketUser, int> participant in dictionaryOfParticipants)
            {
                var fieldBuilderForParticipant = new EmbedFieldBuilder()
                {
                    Name = participant.Key.Username,
                    Value = $"{participant.Value} messages ({dictionaryOfParticipants.Values.Sum() / participant.Value * 100}% helpful)",
                    IsInline = true
                };
                listOfFieldBuilders.Add(fieldBuilderForParticipant);
            }

            await ReplyAsync(
                embed: MessageEmbedPresets.Fields(listOfFieldBuilders, "Final Stats")
            );
        }
    }
}