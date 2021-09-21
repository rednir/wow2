using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;

namespace wow2.Bot.Verbose.Messages
{
    public class QuestionMessage : SavedMessage
    {
        public const string ConfirmText = "Yeah!";
        public const string DenyText = "Nah.";

        public QuestionMessage(string description, string title = null, Func<Task> onConfirm = null, Func<Task> onDeny = null)
        {
            OnConfirm = onConfirm ?? (() => Task.CompletedTask);
            OnDeny = onDeny ?? (() => Task.CompletedTask);
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowinfo:{QuestionEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = new Color(0x9b59b6),
            };

            Components = new ComponentBuilder()
                .WithButton(ConfirmText, ConfirmText, ButtonStyle.Primary)
                .WithButton(DenyText, DenyText, ButtonStyle.Secondary);
        }

        public Func<Task> OnConfirm { get; }

        public Func<Task> OnDeny { get; }

        public static async Task<bool> ActOnButtonAsync(SocketMessageComponent component)
        {
            GuildData guildData = DataManager.AllGuildData[component.Channel.GetGuild().Id];
            if (FromMessageId(guildData, component.Message.Id) is not QuestionMessage message)
                return false;

            try
            {
                if (component.Data.CustomId == ConfirmText)
                {
                    await message.OnConfirm.Invoke();
                }
                else if (component.Data.CustomId == DenyText)
                {
                    await message.OnDeny.Invoke();
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Exception thrown when trying to invoke question message.");

                // TODO: make this message same as the command unhandled exception message.
                await new ErrorMessage($"```{ex.Message}```", "The interaction couldn't be completed")
                    .SendAsync(component.Channel);
            }

            await message.StopAsync();
            return true;
        }
    }
}