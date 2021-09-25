using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;

namespace wow2.Bot.Verbose.Messages
{
    public class SavedMessage : Message, IDisposable
    {
        /// <summary>Finds the <see cref="PagedMessage"/> with the matching message ID.</summary>
        /// <returns>The <see cref="PagedMessage"/> respresenting the message ID, or null if a match was not found.</returns>
        public static SavedMessage FromMessageId(GuildData guildData, ulong messageId) =>
                guildData.SavedMessages.Find(m => m.SentMessage.Id == messageId);

        public static async Task<bool> ActOnButtonAsync(SocketMessageComponent component)
        {
            var savedMessage = FromMessageId(DataManager.AllGuildData[component.Channel.GetGuild().Id], component.Message.Id);
            if (savedMessage == null)
                return false;

            foreach (var actionButton in savedMessage.ActionButtons)
            {
                string[] idParts = component.Data.CustomId.Split(":", 2);
                if (idParts[0] == savedMessage.GetHashCode().ToString() && idParts[1] == actionButton.Label)
                {
                    try
                    {
                        await actionButton.Action.Invoke(component);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex, $"Button {component.Data.CustomId} threw an exception when invoked.");

                        // TODO: make this message same as the command unhandled exception message.
                        await new ErrorMessage($"```{ex.Message}```", "The interaction couldn't be completed")
                            .SendAsync(component.Channel);
                    }

                    return true;
                }
            }

            return false;
        }

        protected List<SavedMessage> SavedMessageList => DataManager.AllGuildData[SentMessage.GetGuild().Id].SavedMessages;

        protected virtual ActionButton[] ActionButtons => Array.Empty<ActionButton>();

        protected bool DontSave => ActionButtons.Length == 0;

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            Components = GetComponentBuilder(ActionButtons);

            IUserMessage message = await base.SendAsync(channel);
            if (!DontSave)
            {
                SavedMessageList.Truncate(120);
                SavedMessageList.Add(this);
            }

            return message;
        }

        /// <summary>Removes all interactive elements from the sent message and disposes it.</summary>
        public async virtual Task StopAsync()
        {
            Components = GetComponentBuilder(ActionButtons, true);

            if (SentMessage.Components.Count > 0)
                await SentMessage.ModifyAsync(m => m.Components = Components.Build());

            Dispose();
        }

        /// <summary>Releases the message from the saved message list.</summary>
        public void Dispose()
        {
            SavedMessageList.Remove(this);
            Logger.Log($"SavedMessage {SentMessage.Id} was disposed.", LogSeverity.Debug);
            GC.SuppressFinalize(this);
        }

        private ComponentBuilder GetComponentBuilder(IEnumerable<ActionButton> actionButtons, bool forceDisable = false)
        {
            var components = new ComponentBuilder();
            foreach (var button in actionButtons)
                components.WithButton(button.Label, button.Url == null ? $"{GetHashCode()}:{button.Label}" : null, button.Style, button.Emote, button.Url, forceDisable || button.Disabled, button.Row);
            return components;
        }
    }
}