using System;
using System.Threading.Tasks;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class DateTimeSelectorMessage : DateTimeSelectorMessageBase
    {
        public DateTimeSelectorMessage(Func<DateTime, Task> confirmFunc, string description = "Select a date and time.")
        {
            Description = description;
            ConfirmFunc = confirmFunc;
        }

        public DateTime DateTime { get; set; } = DateTime.Now;

        private string Description { get; }

        private Func<DateTime, Task> ConfirmFunc { get; }

        protected override async Task AddTimeAsync(TimeSpan timeSpan)
        {
            DateTime += timeSpan;
            await UpdateEmbedAsync();
        }

        protected override async Task OnConfirmAsync()
            => await ConfirmFunc?.Invoke(DateTime);

        protected override async Task UpdateEmbedAsync()
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowquestion:{QuestionEmoteId}>")} {Description}\n`{DateTime.ToLongDateString()} / {DateTime.ToShortTimeString()}`",
                Color = new Color(0x9b59b6),
            };

            await base.UpdateEmbedAsync();
        }
    }
}