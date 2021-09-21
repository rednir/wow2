using System;
using System.Threading.Tasks;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules
{
    public abstract class Module : ModuleBase<SocketCommandContext>
    {
        public IDataManager DataManager { get; set; }

        public string CommandPrefix => DataManager.AllGuildData[Context.Guild.Id].Main.CommandPrefix;

        public async Task SendToggleQuestionAsync(
            bool currentState,
            Action<bool> setter,
            string toggledOnMessage = "Toggled setting.",
            string toggledOffMessage = "Toggled setting.")
        {
            await new QuestionMessage(
                description: $"Are you sure you want to turn this setting {(!currentState ? "on" : "off")}?",
                title: null,
                onConfirm: async () =>
                {
                    setter.Invoke(!currentState);
                    if (!currentState && toggledOnMessage != null)
                    {
                        await new SuccessMessage(toggledOnMessage)
                            .SendAsync(Context.Channel);
                    }
                    else if (currentState && toggledOffMessage != null)
                    {
                        await new SuccessMessage(toggledOffMessage)
                            .SendAsync(Context.Channel);
                    }
                },
                onDeny: async () =>
                {
                    await new SuccessMessage("Nothing was changed.")
                        .SendAsync(Context.Channel);
                })
                .SendAsync(Context.Channel);
        }
    }
}