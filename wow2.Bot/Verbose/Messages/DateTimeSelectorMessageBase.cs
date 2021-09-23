using System;
using System.Threading.Tasks;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public abstract class DateTimeSelectorMessageBase : SavedMessage
    {
        protected override ActionButton[] ActionButtons => new[]
        {
            new ActionButton()
            {
                Label = "Confirm",
                Style = ButtonStyle.Primary,
                Row = 0,
                Action = async _ =>
                {
                    await StopAsync();
                    await OnConfirmAsync();
                },
            },
            new ActionButton()
            {
                Label = "Cancel",
                Style = ButtonStyle.Danger,
                Row = 0,
                Action = async _ => await StopAsync(),
            },
            new ActionButton()
            {
                Label = "+1 week",
                Style = ButtonStyle.Secondary,
                Row = 1,
                Action = async _ => await AddTimeAsync(TimeSpan.FromDays(7)),
            },
            new ActionButton()
            {
                Label = "-1 week",
                Style = ButtonStyle.Secondary,
                Row = 1,
                Action = async _ => await AddTimeAsync(TimeSpan.FromDays(-7)),
            },
            new ActionButton()
            {
                Label = "+1 day",
                Style = ButtonStyle.Secondary,
                Row = 1,
                Action = async _ => await AddTimeAsync(TimeSpan.FromDays(1)),
            },
            new ActionButton()
            {
                Label = "-1 day",
                Style = ButtonStyle.Secondary,
                Row = 1,
                Action = async _ => await AddTimeAsync(TimeSpan.FromDays(-1)),
            },
            new ActionButton()
            {
                Label = "+1 hour",
                Style = ButtonStyle.Secondary,
                Row = 2,
                Action = async _ => await AddTimeAsync(TimeSpan.FromHours(1)),
            },
            new ActionButton()
            {
                Label = "-1 hour",
                Style = ButtonStyle.Secondary,
                Row = 2,
                Action = async _ => await AddTimeAsync(TimeSpan.FromHours(-1)),
            },
            new ActionButton()
            {
                Label = "+10 minutes",
                Style = ButtonStyle.Secondary,
                Row = 2,
                Action = async _ => await AddTimeAsync(TimeSpan.FromMinutes(10)),
            },
            new ActionButton()
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

        protected abstract Task AddTimeAsync(TimeSpan timeSpan);

        protected abstract Task OnConfirmAsync();

        protected virtual async Task UpdateEmbedAsync()
        {
            if (SentMessage != null)
                await SentMessage.ModifyAsync(m => m.Embed = Embed);
        }
    }
}