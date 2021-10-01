using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games.Counting
{
    public class CountingGameMessage : GameMessage
    {
        public CountingGameMessage(SocketCommandContext context, float increment)
            : base(context)
        {
            Increment = increment;
            NextNumber = increment;
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"To start off, type the number `{NextNumber}` in this channel.",
                Title = "Counting game has started.",
                Color = Color.LightGrey,
            };
        }

        public float Increment { get; }

        public List<SocketMessage> ListOfMessages { get; } = new();

        public float NextNumber { get; private set; }

        public override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            BotService.Client.MessageReceived += ActOnMessageAsync;
            return base.SendAsync(channel);
        }

        public override async Task StopAsync()
        {
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            var dictionaryOfParticipants = new Dictionary<SocketUser, int>();

            // Get the participants and how many messages they sent
            foreach (var message in ListOfMessages)
            {
                dictionaryOfParticipants.TryAdd(message.Author, 0);
                dictionaryOfParticipants[message.Author]++;
            }

            // Build fields for each participant.
            foreach (var participant in dictionaryOfParticipants)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = participant.Key.Username,
                    Value = $"{participant.Value} messages ({Math.Round((float)participant.Value / dictionaryOfParticipants.Values.Sum() * 100f)}% helpful)",
                    IsInline = true,
                });
            }

            int index = SubmitGame();
            await new PagedMessage(
                fieldBuilders: listOfFieldBuilders,
                title: "📈 Final Stats",
                description: $"*You counted up to* `{NextNumber - Increment}`\n*{getComment()}*\nYou made it to number `{index + 1}` on the leaderboards!")
                    .SendAsync(InitialContext.Channel);

            await base.StopAsync();
            return; 

            string getComment()
            {
                float absNextNumber = Math.Abs((float)NextNumber);
                float absIncrement = Math.Abs((float)Increment);

                if (absNextNumber < 3 * absIncrement)
                    return "Pathetic.";
                else if (absNextNumber < 25 * absIncrement)
                    return "There's plenty room for improvement.";
                else if (absNextNumber < 75 * absIncrement)
                    return "Not bad!";
                else if (absNextNumber >= 75 * absIncrement)
                    return "Amazing!";
                else
                    return string.Empty;
            }
        }

        private async Task ActOnMessageAsync(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot || InitialContext.Channel != socketMessage.Channel)
                return;

            if (!float.TryParse(socketMessage.Content, out float userNumber))
                return;

            if (socketMessage.Author == ListOfMessages.LastOrDefault()?.Author)
            {
                await new WarningMessage("Counting twice in a row is no fun.")
                {
                    ReplyToMessageId = socketMessage.Id,
                }
                .SendAsync(socketMessage.Channel);
                return;
            }

            ListOfMessages.Add(socketMessage);
            if (userNumber == NextNumber)
            {
                NextNumber += Increment;
                await socketMessage.AddReactionAsync(new Emoji("✅"));
                await StopIfNextNumberInvalidAsync();
            }
            else
            {
                await socketMessage.AddReactionAsync(new Emoji("❎"));
                await new InfoMessage($"Counting was ruined by {socketMessage.Author.Mention}. Nice one.\nThe next number should have been `{NextNumber}`")
                    .SendAsync(socketMessage.Channel);
                await StopAsync();
            }
        }

        private async Task StopIfNextNumberInvalidAsync()
        {
            if (NextNumber >= float.MaxValue || NextNumber <= float.MinValue)
            {
                await new WarningMessage("Woah, that's a big number.\nHate to be a killjoy, but even a computer has its limits.")
                    .SendAsync(InitialContext.Channel);
                await StopAsync();
            }
        }
    }
}