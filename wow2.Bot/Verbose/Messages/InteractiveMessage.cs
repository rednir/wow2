using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Modules;

namespace wow2.Bot.Verbose.Messages
{
    public class InteractiveMessage : Message, IDisposable
    {
        /// <summary>Finds the <see cref="InteractiveMessage"/> with the matching message ID.</summary>
        /// <returns>The <see cref="InteractiveMessage"/> respresenting the message ID, or null if a match was not found.</returns>
        public static InteractiveMessage FromMessageId(GuildData guildData, ulong messageId) =>
                guildData.InteractiveMessages.Find(m => m.SentMessage.Id == messageId);

        public static async Task<bool> ActOnButtonAsync(SocketMessageComponent component)
        {
            var interactiveMessage = FromMessageId(DataManager.AllGuildData[component.Channel.GetGuild().Id], component.Message.Id);
            if (interactiveMessage == null)
                return false;

            foreach (var actionButton in interactiveMessage.GetActionButtons())
            {
                string[] idParts = component.Data.CustomId.Split(":", 2);
                if (idParts[0] == interactiveMessage.GetHashCode().ToString() && idParts[1] == actionButton.Label)
                {
                    try
                    {
                        await actionButton.Action?.Invoke(component);
                        Logger.Log($"Handled button press '{actionButton.Label}' for {component.User} in {component.Channel.GetGuild()?.Name}/{component.Channel.Name}", LogSeverity.Verbose);
                    }
                    catch (Exception ex) when (ex is not CommandReturnException)
                    {
                        Logger.LogException(ex, $"Button press {component.Data.CustomId} by {component.User} threw an exception when invoked.");

                        // TODO: make this message same as the command unhandled exception message.
                        await new ErrorMessage($"```{ex.Message}```", "The interaction couldn't be completed")
                            .SendAsync(component.Channel);
                    }

                    return true;
                }
            }

            return false;
        }

        public List<ActionButton> ExtraActionButtons { get; set; } = new();

        protected virtual ActionButton[] ActionButtons => Array.Empty<ActionButton>();

        /// <summary>Gets all the <see cref="ActionButton" /> objects that will be sent with the message.</summary>
        public ActionButton[] GetActionButtons() => ActionButtons.Concat(ExtraActionButtons).ToArray();

        protected List<InteractiveMessage> InteractiveMessageList => DataManager.AllGuildData[SentMessage.GetGuild().Id].InteractiveMessages;

        protected bool DontSave => !GetActionButtons().Any(b => b.Style != ButtonStyle.Link);

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            Components = GetComponentBuilder();

            IUserMessage message = await base.SendAsync(channel);
            if (!DontSave)
            {
                InteractiveMessageList.Truncate(120);
                InteractiveMessageList.Add(this);
            }

            return message;
        }

        /// <summary>Removes all interactive elements from the sent message and disposes it.</summary>
        public async virtual Task StopAsync()
        {
            Components = GetComponentBuilder(true);
            await base.UpdateMessageAsync();
            Dispose();
        }

        /// <summary>Releases the message from the saved message list.</summary>
        public void Dispose()
        {
            InteractiveMessageList.Remove(this);
            Logger.Log($"Interactive message {SentMessage.Id} was disposed.", LogSeverity.Debug);
            GC.SuppressFinalize(this);
        }

        public async override Task UpdateMessageAsync()
        {
            Components = GetComponentBuilder();
            await base.UpdateMessageAsync();
        }

        private ComponentBuilder GetComponentBuilder(bool forceDisableActions = false)
        {
            var components = new ComponentBuilder();
            foreach (var button in GetActionButtons())
            {
                components.WithButton(
                    label: button.Label,
                    customId: button.Url == null ? $"{GetHashCode()}:{button.Label}" : null,
                    style: button.Style,
                    emote: button.Emote,
                    url: button.Url,
                    disabled: (forceDisableActions && button.Action != null) || button.Disabled,
                    row: button.Row);
            }

            return components;
        }
    }
}