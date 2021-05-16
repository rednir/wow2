using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace wow2.Verbose.Messages
{
    /// <summary>Class for sending and building embeds with pages of fields.</summary>
    public class PagedMessage : Message, IDisposable
    {
        public static readonly IEmote PageLeftEmote = new Emoji("⏪");
        public static readonly IEmote StopEmote = new Emoji("🛑");
        public static readonly IEmote PageRightEmote = new Emoji("⏩");

        public static List<PagedMessage> ListOfPagedMessages { get; protected set; } = new();

        public List<EmbedFieldBuilder> AllFieldBuilders { get; protected set; }
        public int Page { get; protected set; }

        public PagedMessage(List<EmbedFieldBuilder> fieldBuilders, string description = "", string title = "", int page = -1)
        {
            AllFieldBuilders = fieldBuilders;
            Page = page;
            EmbedBuilder = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = Color.LightGrey,
            };
            SetEmbedFields();
        }

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            // TODO: Performance might still be slow. Maybe have a list for every guild?
            if (ListOfPagedMessages.Count > 40)
                ListOfPagedMessages.RemoveAt(40);

            IUserMessage message = await base.SendAsync(channel);

            // A negative page number means the message does not use pages.
            if (Page >= 0)
            {
                ListOfPagedMessages.Add(this);
                await message.AddReactionsAsync(
                    new IEmote[] { PageLeftEmote, PageRightEmote, StopEmote });
            }
            return message;
        }

        /// <summary>If the message has pages and the emote is recognised, modifies the page of the message.</summary>
        public static async Task ActOnReactionAsync(SocketReaction reaction)
        {
            PagedMessage message = ListOfPagedMessages.Find(m => m.SentMessage?.Id == reaction.MessageId);
            if (message == null)
                return;

            if (reaction.Emote.Name == PageLeftEmote.Name)
            {
                await message.ChangePageByAsync(-1);
                await message.SentMessage.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            }
            else if (reaction.Emote.Name == PageRightEmote.Name)
            {
                await message.ChangePageByAsync(1);
                await message.SentMessage.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            }
            else if (reaction.Emote.Name == StopEmote.Name)
            {
                await message.StopAsync();
            }
        }

        /// <summary>Modify the page and therefore the embed of this message.</summary>
        public async Task ChangePageByAsync(int increment)
        {
            Page += increment;
            SetEmbedFields();
            await SentMessage.ModifyAsync(
                message => message.Embed = EmbedBuilder.Build());
            Logger.Log($"PagedMessage {SentMessage.Id} was changed to page {Page}.", LogSeverity.Debug);
        }

        /// <summary>Removes reactions, and stops ability to scroll through pages for this message.</summary>
        public async Task StopAsync()
        {
            Dispose();
            await SentMessage.RemoveAllReactionsAsync();
        }

        public void Dispose()
        {
            ListOfPagedMessages.Remove(this);
            GC.SuppressFinalize(this);
            Logger.Log($"PagedMessage {SentMessage.Id} was disposed.", LogSeverity.Debug);
        }

        private void SetEmbedFields()
        {
            const int maxFieldsPerPage = 8;

            // Check if the fields don't fit on one page.
            if (AllFieldBuilders.Count > maxFieldsPerPage)
            {
                int totalNumberOfPages = (int)Math.Ceiling((float)AllFieldBuilders.Count / maxFieldsPerPage);

                // Pages are not used if page number is negative.
                if (Page < 0)
                {
                    EmbedBuilder.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = $"https://cdn.discordapp.com/emojis/{WarningEmoteId}.png",
                        Text = $"{AllFieldBuilders.Count - maxFieldsPerPage} items were excluded",
                    };
                }
                else
                {
                    // Ensure page number is within bounds.
                    Page = Math.Min(totalNumberOfPages, Math.Max(1, Page));

                    EmbedBuilder.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = $"https://cdn.discordapp.com/emojis/{InfoEmoteId}.png",
                        Text = $"Page {Page}/{totalNumberOfPages}",
                    };
                }

                // Page 0 and 1 should have the same starting index (never negative).
                int startIndex = maxFieldsPerPage * Math.Max(Page - 1, 0);

                // Get the fields that are within the page and set the embed's fields to that.
                bool isFinalPage = Page == totalNumberOfPages;
                EmbedBuilder.Fields = AllFieldBuilders.GetRange(startIndex,
                    isFinalPage ? (AllFieldBuilders.Count - startIndex) : maxFieldsPerPage);
            }
            else
            {
                Page = -1;
                EmbedBuilder.Fields = AllFieldBuilders;
            }
        }
    }
}