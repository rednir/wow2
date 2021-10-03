using System;
using System.Linq;

namespace wow2.Bot.Modules.Games.Typing
{
    public class Segment
    {
        public Segment(string content)
        {
            Content = content;
        }

        public string Content { get; }

        public double Wpm => Content.Length / 5 / TimeSpent.TotalMinutes;

        public TimeSpan TimeSpent { get; set; }

        public DateTimeOffset TimeCompleted { get; set; }

        public double Accuracy { get; set; }

        public string CompletedInfo => TimeCompleted == default ? null :
            $"{(Accuracy > 0.9 ? "😄" : (Accuracy > 0.65 ? "😡" : "💥"))} {Math.Round(TimeSpent.TotalSeconds, 1)}sec / {Math.Round(Wpm)}wpm / {Math.Round(Accuracy * 100)}%";
    }
}