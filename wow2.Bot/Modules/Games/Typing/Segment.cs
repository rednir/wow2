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

        public int TotalWords => Content.Count(c => c == ' ') + 1;

        public double Wpm => TotalWords / TimeSpent.TotalMinutes;

        public TimeSpan TimeSpent { get; set; }

        public DateTimeOffset TimeCompleted { get; set; }

        public double Accuracy { get; set; }

        public string CompletedInfo => TimeCompleted == default ? null :
            $"{(Accuracy > 0.92 ? "😄" : "😕")} {Math.Round(TimeSpent.TotalSeconds, 1)}sec / {Math.Round(Wpm)}wpm / {Math.Round(Accuracy * 100)}%";
    }
}