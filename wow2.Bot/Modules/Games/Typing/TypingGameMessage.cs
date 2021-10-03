using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games.Typing
{
    public class TypingGameMessage : GameMessage
    {
        private const int NumberOfWords = 30;
        private const int SegmentSize = 6;

        private static readonly IReadOnlyDictionary<char, string> CharacterDictionary = new Dictionary<char, string>
        {
            { 'a', "𝘢" }, { 'b', "𝘣" }, { 'c', "𝘤" }, { 'd', "𝘥" },
            { 'e', "𝘦" }, { 'f', "𝘧" }, { 'g', "𝘨" }, { 'h', "𝘩" },
            { 'i', "𝘪" }, { 'j', "𝘫" }, { 'k', "𝘬" }, { 'l', "𝘭" },
            { 'm', "𝘮" }, { 'n', "𝘯" }, { 'o', "𝘰" }, { 'p', "𝘱" },
            { 'q', "𝘲" }, { 'r', "𝘳" }, { 's', "𝘴" }, { 't', "𝘵" },
            { 'u', "𝘶" }, { 'v', "𝘷" }, { 'w', "𝘸" }, { 'x', "𝘹" },
            { 'y', "𝘺" }, { 'z', "𝘻" },
        };

        public TypingGameMessage(SocketCommandContext context, GameResourceService resourceService)
            : base(context, null, resourceService)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = "Type the above text as fast as you can. When you see a `⏎`, send the message!",
                Title = "Typing game has started.",
                Color = Color.LightGrey,
            };

            // Add segments to the list.
            var stringBuilder = new StringBuilder();
            for (int i = 0; i < NumberOfWords; i++)
            {
                stringBuilder.Append(resourceService.GetRandomWord());

                if ((i % SegmentSize == 0 && i != 0) || i == NumberOfWords - 1)
                {
                    Segments.Add(stringBuilder.ToString());
                    stringBuilder.Clear();
                }
                else
                {
                    stringBuilder.Append(' ');
                }
            }
        }

        private static string GetAlternativeText(string text)
        {
            var stringBuilder = new StringBuilder();
            foreach (char c in text)
                stringBuilder.Append(CharacterDictionary.GetValueOrDefault(c) ?? c.ToString());

            return stringBuilder.ToString();
        }

        public override Task UpdateMessageAsync()
        {
            Text = string.Empty;
            foreach (string segment in Segments)
                Text += $"`{GetAlternativeText(segment)} ⏎`\n";

            return base.UpdateMessageAsync();
        }

        public override int Points => (int)(Wpm * Accuracy);

        private double Wpm => Segments.Sum(s => s.Count(c => c == ' ') + 1) / (LastSegmentCompletedAt - SentMessage.Timestamp).TotalMinutes;

        private double Accuracy { get; set; } = 1;

        private List<string> Segments { get; } = new();

        private int CurrentIndexInSegments { get; set; }

        private DateTimeOffset LastSegmentCompletedAt { get; set; }

        public override async Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            BotService.Client.MessageReceived += ActOnMessageAsync;
            await UpdateMessageAsync();

            var message = await base.SendAsync(channel);
            LastSegmentCompletedAt = message.Timestamp;
            return message;
        }

        public override async Task StopAsync()
        {
            await new GenericMessage(
                description: $"You got `{Points}` points, with `{Math.Round(Wpm)}` words per minute and an accuracy of `{Math.Round(Accuracy * 100)}%`",
                title: "📈 Final Stats")
                    .SendAsync(InitialContext.Channel);

            BotService.Client.MessageReceived -= ActOnMessageAsync;
            await base.StopAsync();
        }

        private async Task ActOnMessageAsync(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot || InitialContext.Channel != socketMessage.Channel)
                return;

            // Get info about the current segment.
            TimeSpan segmentTime = socketMessage.Timestamp - LastSegmentCompletedAt;
            double wpm = (Segments[CurrentIndexInSegments].Count(c => c == ' ') + 1) / segmentTime.TotalMinutes;

            double levenshteinDistance = Segments[CurrentIndexInSegments].LevenshteinDistanceWith(socketMessage.Content);
            double maxDistance = Math.Max(socketMessage.Content.Length, Segments[CurrentIndexInSegments].Length);
            double accuracy = 1 - (levenshteinDistance / maxDistance);

            // Update info about the completed segment.
            Segments[CurrentIndexInSegments] = $"{(accuracy > 0.92 ? "😄" : "😕")} {Math.Round(segmentTime.TotalSeconds, 1)}sec / {Math.Round(wpm)}wpm / {Math.Round(accuracy * 100)}%";
            Accuracy *= accuracy;
            LastSegmentCompletedAt = socketMessage.Timestamp;

            CurrentIndexInSegments++;
            if (CurrentIndexInSegments >= Segments.Count)
            {
                await StopAsync();
            }
            else
            {
                await UpdateMessageAsync();
            }
        }
    }
}