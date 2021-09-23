using System;
using System.Threading.Tasks;
using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Timers
{
    public class TimerStartedMessage : SavedMessage
    {
        protected override ActionButtons[] ActionButtons => new[]
        {
            new ActionButtons()
            {
                Label = "Get notified for this timer",
                Style = ButtonStyle.Primary,
                Action = async component =>
                {
                    if (Timer.NotifyUserMentions.Remove(component.User.Mention))
                    {
                        await component.FollowupAsync(
                            embed: new SuccessMessage("You'll no longer be notified about this timer.").Embed,
                            ephemeral: true);
                    }
                    else
                    {
                        Timer.NotifyUserMentions.Add(component.User.Mention);
                        await component.FollowupAsync(
                            embed: new SuccessMessage("Changed your mind? Click the button again.", "You'll be notified when this timer elapses").Embed,
                            ephemeral: true);
                    }
                },
            },
            new ActionButtons()
            {
                Label = "Delete timer",
                Style = ButtonStyle.Danger,
                Action = async component =>
                {
                    Timer.Dispose();
                    await StopAsync();
                    await new SuccessMessage($"Timer was deleted on request of {component.User.Mention}")
                        .SendAsync(component.Channel);
                },
            },
        };

        public TimerStartedMessage(UserTimer timer)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowsuccess:{SuccessEmoteId}>")} {GetStatusMessageFormattedDescription("Timer was started.", null)}",
                Color = new Color(0x2ECC71),
            };

            Timer = timer;
        }

        private UserTimer Timer { get; }
    }
}