using System;
using System.Threading.Tasks;
using Discord;
using wow2.Bot.Extensions;

namespace wow2.Bot.Verbose.Messages
{
    public class DateTimeSelectorMessage : DateTimeSelectorMessageBase
    {
        public DateTimeSelectorMessage(Func<DateTime, Task> confirmFunc, string description = "Select a date and time.", DateTime? min = null, DateTime? max = null)
        {
            Description = description;
            ConfirmFunc = confirmFunc;
            MinValue = min;
            MaxValue = max;
        }

        public DateTime DateTime { get; set; } = DateTime.Now;

        protected override bool ValueIsInvalid => DateTime > MaxValue || DateTime < MinValue;

        private DateTime? MinValue { get; set; }

        private DateTime? MaxValue { get; set; }

        private string Description { get; }

        private Func<DateTime, Task> ConfirmFunc { get; }

        public override async Task UpdateMessageAsync()
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowquestion:{QuestionEmoteId}>")} {Description}\n{DateTime.ToDiscordTimestamp("F")}",
                Color = new Color(0x9b59b6),
            };

            await base.UpdateMessageAsync();
        }

        protected override async Task AddTimeAsync(TimeSpan timeSpan)
        {
            DateTime += timeSpan;
            await UpdateMessageAsync();
        }

        protected override async Task OnConfirmAsync()
            => await ConfirmFunc?.Invoke(DateTime);
    }
}