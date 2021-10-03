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

        public TypingGameMessage(SocketCommandContext context, List<TypingLeaderboardEntry> leaderboard, GameResourceService resourceService)
            : base(context, leaderboard.Cast<LeaderboardEntry>().ToArray(), resourceService)
        {
            // Add segments to the list.
            var stringBuilder = new StringBuilder();
            for (int i = 0; i < NumberOfWords; i++)
            {
                stringBuilder.Append(resourceService.GetRandomWord());

                if ((i % SegmentSize == 0 && i != 0) || i == NumberOfWords - 1)
                {
                    Segments.Add(new Segment(stringBuilder.ToString()));
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
            foreach (var segment in Segments)
                Text += segment.CompletedInfo != null ? $"`{segment.CompletedInfo}`\n" : $"`{GetAlternativeText(segment.Content)} ⏎`\n";

            EmbedBuilder = new EmbedBuilder()
            {
                Description = "Type the above text as fast as you can. When you see a `⏎`, send the message!",
                Title = "⌨ Typing game has started.",
                Fields = MiniLeaderboardFields,
                Color = Color.LightGrey,
            };

            return base.UpdateMessageAsync();
        }

        public override int Points => (int)(Wpm * Accuracy);

        private double Wpm => Segments.Sum(s => s.Content.Length) / 5 / (LastSegmentCompletedAt - SentMessage?.Timestamp)?.TotalMinutes ?? 0;

        private double Accuracy => Segments.Count == 0 ? 1 : Segments.Average(x => x.Accuracy);

        private List<Segment> Segments { get; } = new();

        private int CurrentIndexInSegments { get; set; }

        private DateTimeOffset LastSegmentCompletedAt => Segments.LastOrDefault(s => s.TimeCompleted != default)?.TimeCompleted ?? SentMessage?.Timestamp ?? default;

        public override async Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            BotService.Client.MessageReceived += ActOnMessageAsync;
            await UpdateMessageAsync();

            return await base.SendAsync(channel);
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

            var segment = Segments[CurrentIndexInSegments];

            // Set info about the completed segment.
            double levenshteinDistance = Segments[CurrentIndexInSegments].Content.LevenshteinDistanceWith(socketMessage.Content);
            double maxDistance = Math.Max(socketMessage.Content.Length, Segments[CurrentIndexInSegments].Content.Length);
            segment.Accuracy = 1 - (levenshteinDistance / maxDistance);
            segment.TimeSpent = socketMessage.Timestamp - LastSegmentCompletedAt;
            segment.TimeCompleted = socketMessage.Timestamp;

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