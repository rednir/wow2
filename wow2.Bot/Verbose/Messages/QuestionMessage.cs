using System;
using System.Threading.Tasks;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    public class QuestionMessage : InteractiveMessage
    {
        public QuestionMessage(string description, string title = null, Func<Task> onConfirm = null, Func<Task> onDeny = null)
        {
            OnConfirm = onConfirm ?? (() => Task.CompletedTask);
            OnDeny = onDeny ?? (() => Task.CompletedTask);
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowinfo:{QuestionEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = new Color(0x9b59b6),
            };
        }

        protected override ActionButton[] ActionButtons => new[]
        {
            new ActionButton()
            {
                Label = "Yeah!",
                Style = ButtonStyle.Primary,
                Action = async _ =>
                {
                    await StopAsync();
                    await OnConfirm();
                },
            },
            new ActionButton()
            {
                Label = "Nah...",
                Style = ButtonStyle.Secondary,
                Action = async _ =>
                {
                    await StopAsync();
                    await OnDeny();
                },
            },
        };

        public Func<Task> OnConfirm { get; }

        public Func<Task> OnDeny { get; }
    }
}