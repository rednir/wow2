using System;
using System.Threading.Tasks;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public abstract class DateTimeSelectorMessageBase : InteractiveMessage
    {
        protected override ActionButton[] ActionButtons => new[]
        {
            new ActionButton()
            {
                Label = "Confirm",
                Style = ButtonStyle.Primary,
                Row = 0,
                Disabled = ValueIsInvalid,
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
                Row = 2,
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
                Row = 2,
                Action = async _ => await AddTimeAsync(TimeSpan.FromDays(-1)),
            },
            new ActionButton()
            {
                Label = "+6 hour",
                Style = ButtonStyle.Secondary,
                Row = 1,
                Action = async _ => await AddTimeAsync(TimeSpan.FromHours(6)),
            },
            new ActionButton()
            {
                Label = "-6 hour",
                Style = ButtonStyle.Secondary,
                Row = 2,
                Action = async _ => await AddTimeAsync(TimeSpan.FromHours(-6)),
            },
            new ActionButton()
            {
                Label = "+1 hour",
                Style = ButtonStyle.Secondary,
                Row = 1,
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
                Row = 1,
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
            await UpdateMessageAsync();
            return await base.SendAsync(channel);
        }

        public override async Task UpdateMessageAsync()
        {
            if (EmbedBuilder != null)
            {
                EmbedBuilder.Footer = !ValueIsInvalid ? null : new EmbedFooterBuilder()
                {
                    IconUrl = $"https://cdn.discordapp.com/emojis/{WarningEmoteId}.png",
                    Text = "This time is invalid, try something else.",
                };
            }

            await base.UpdateMessageAsync();
        }

        protected abstract bool ValueIsInvalid { get; }

        protected abstract Task AddTimeAsync(TimeSpan timeSpan);

        protected abstract Task OnConfirmAsync();
    }
}