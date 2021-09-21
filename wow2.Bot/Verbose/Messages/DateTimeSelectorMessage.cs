using System;
using System.Threading.Tasks;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class DateTimeSelectorMessage : SavedMessage
    {
        public DateTimeSelectorMessage(Func<DateTime, Task> confirmFunc, string description = "Select a date and time.")
        {
            Description = description;
            ConfirmFunc = confirmFunc;
        }

        public DateTime DateTime { get; set; } = DateTime.Now;

        private string Description { get; }

        private Func<DateTime, Task> ConfirmFunc { get; }

        protected override ActionButtons[] ActionButtons => new[]
        {
            new ActionButtons()
            {
                Label = "Confirm",
                Style = ButtonStyle.Primary,
                Row = 0,
                Action = async _ =>
                {
                    await StopAsync();
                    await ConfirmFunc?.Invoke(DateTime);
                },
            },
            new ActionButtons()
            {
                Label = "Cancel",
                Style = ButtonStyle.Danger,
                Row = 0,
                Action = async _ => await StopAsync(),
            },
            new ActionButtons()
            {
                Label = "+1 week",
                Style = ButtonStyle.Secondary,
                Row = 1,
                Action = async _ => await AddTimeAsync(TimeSpan.FromDays(7)),
            },
            new ActionButtons()
            {
                Label = "-1 week",
                Style = ButtonStyle.Secondary,
                Row = 1,
                Action = async _ => await AddTimeAsync(TimeSpan.FromDays(-7)),
            },
            new ActionButtons()
            {
                Label = "+1 day",
                Style = ButtonStyle.Secondary,
                Row = 1,
                Action = async _ => await AddTimeAsync(TimeSpan.FromDays(1)),
            },
            new ActionButtons()
            {
                Label = "-1 day",
                Style = ButtonStyle.Secondary,
                Row = 1,
                Action = async _ => await AddTimeAsync(TimeSpan.FromDays(-1)),
            },
            new ActionButtons()
            {
                Label = "+1 hour",
                Style = ButtonStyle.Secondary,
                Row = 2,
                Action = async _ => await AddTimeAsync(TimeSpan.FromHours(1)),
            },
            new ActionButtons()
            {
                Label = "-1 hour",
                Style = ButtonStyle.Secondary,
                Row = 2,
                Action = async _ => await AddTimeAsync(TimeSpan.FromHours(-1)),
            },
            new ActionButtons()
            {
                Label = "+10 minutes",
                Style = ButtonStyle.Secondary,
                Row = 2,
                Action = async _ => await AddTimeAsync(TimeSpan.FromMinutes(10)),
            },
            new ActionButtons()
            {
                Label = "-10 minutes",
                Style = ButtonStyle.Secondary,
                Row = 2,
                Action = async _ => await AddTimeAsync(TimeSpan.FromMinutes(-10)),
            },
        };

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            await UpdateEmbedAsync();
            return await base.SendAsync(channel);
        }

        private async Task AddTimeAsync(TimeSpan timeSpan)
        {
            DateTime += timeSpan;
            await UpdateEmbedAsync();
        }

        private async Task UpdateEmbedAsync()
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowquestion:{QuestionEmoteId}>")} {Description}\n`{DateTime.ToLongDateString()} / {DateTime.ToShortTimeString()}`",
                Color = new Color(0x9b59b6),
            };

            if (SentMessage != null)
                await SentMessage.ModifyAsync(m => m.Embed = Embed);
        }
    }
}