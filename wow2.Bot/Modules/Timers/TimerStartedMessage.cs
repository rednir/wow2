using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
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
                    bool userWillBeNotified = await NotifyUserButton.Invoke(component.User);
                    if (userWillBeNotified)
                    {
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
                    await DeleteTimerButton.Invoke();
                    await StopAsync();
                    await new SuccessMessage($"Timer was deleted on request of {component.User.Mention}")
                        .SendAsync(component.Channel);
                },
            },
        };

        public TimerStartedMessage(Func<SocketUser, Task<bool>> notifyUserButton = null, Func<Task> deleteTimerButton = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowsuccess:{SuccessEmoteId}>")} {GetStatusMessageFormattedDescription("Timer was started.", null)}",
                Color = new Color(0x2ECC71),
            };

            NotifyUserButton = notifyUserButton;
            DeleteTimerButton = deleteTimerButton;
        }

        private Func<SocketUser, Task<bool>> NotifyUserButton { get; }

        private Func<Task> DeleteTimerButton { get; }
    }
}