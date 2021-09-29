using System;
using System.Threading.Tasks;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class TimeSpanSelectorMessage : DateTimeSelectorMessageBase
    {
        public TimeSpanSelectorMessage(Func<TimeSpan, Task> confirmFunc, string description = "Select a time span.", TimeSpan? min = null, TimeSpan? max = null)
        {
            Description = description;
            ConfirmFunc = confirmFunc;
            MinValue = min;
            MaxValue = max;
        }

        public TimeSpan TimeSpan { get; set; } = TimeSpan.Zero;

        protected override bool ValueIsInvalid => TimeSpan > MaxValue || TimeSpan < MinValue;

        private TimeSpan? MinValue { get; set; }

        private TimeSpan? MaxValue { get; set; }

        private string Description { get; }

        private Func<TimeSpan, Task> ConfirmFunc { get; }

        public override async Task UpdateMessageAsync()
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowquestion:{QuestionEmoteId}>")} {Description}\n`{TimeSpan.Days} days, {TimeSpan.Hours} hours, {TimeSpan.Minutes} minutes`",
                Color = new Color(0x9b59b6),
            };

            await base.UpdateMessageAsync();
        }

        protected override async Task AddTimeAsync(TimeSpan timeSpan)
        {
            TimeSpan += timeSpan;
            await UpdateMessageAsync();
        }

        protected override async Task OnConfirmAsync()
            => await ConfirmFunc?.Invoke(TimeSpan);
    }
}