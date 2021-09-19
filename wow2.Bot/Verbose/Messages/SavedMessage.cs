using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using wow2.Bot.Data;
using wow2.Bot.Extensions;

namespace wow2.Bot.Verbose.Messages
{
    public class SavedMessage : Message, IDisposable
    {
        protected List<SavedMessage> SavedMessageList => DataManager.AllGuildData[SentMessage.GetGuild().Id].SavedMessages;

        protected virtual bool DontSave => false;

        /// <summary>Finds the <see cref="PagedMessage"/> with the matching message ID.</summary>
        /// <returns>The <see cref="PagedMessage"/> respresenting the message ID, or null if a match was not found.</returns>
        public static SavedMessage FromMessageId(GuildData guildData, ulong messageId) =>
                guildData.SavedMessages.Find(m => m.SentMessage.Id == messageId);

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            IUserMessage message = await base.SendAsync(channel);
            if (!DontSave)
            {
                SavedMessageList.Truncate(70);
                SavedMessageList.Add(this);
            }

            return message;
        }

        /// <summary>Removes all interactive elements from the sent message and disposes it.</summary>
        public async virtual Task StopAsync()
        {
            if (SentMessage.Components.Count > 0)
                await SentMessage.ModifyAsync(m => m.Components = null);

            Dispose();
        }

        /// <summary>Releases the message from the saved message list.</summary>
        public void Dispose()
        {
            SavedMessageList.Remove(this);
            Logger.Log($"SavedMessage {SentMessage.Id} was disposed.", LogSeverity.Debug);
            GC.SuppressFinalize(this);
        }
    }
}