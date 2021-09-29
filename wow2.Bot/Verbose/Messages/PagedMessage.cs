using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace wow2.Bot.Verbose.Messages
{
    /// <summary>Class for sending and building embeds with pages of fields.</summary>
    public class PagedMessage : InteractiveMessage
    {
        public PagedMessage()
        {
        }

        public PagedMessage(List<EmbedFieldBuilder> fieldBuilders, string description = "", string title = "", int? page = null, int maxFieldsPerPage = 8)
        {
            AllFieldBuilders = fieldBuilders;
            Page = page;
            MaxFieldsPerPage = maxFieldsPerPage;
            EmbedBuilder = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = Color.LightGrey,
            };

            UpdateProperties();
        }

        public List<EmbedFieldBuilder> AllFieldBuilders { get; protected set; }

        public int? Page { get; protected set; }

        public int MaxFieldsPerPage { get; }

        public virtual int TotalNumberOfPages => (int)Math.Ceiling((float)AllFieldBuilders.Count / MaxFieldsPerPage);

        public string StoppedByUsername { get; set; }

        protected override ActionButton[] ActionButtons => Page == null ? Array.Empty<ActionButton>() : new[]
        {
            new ActionButton()
            {
                Label = "«",
                Style = ButtonStyle.Primary,
                Disabled = Page == 1,
                Action = async _ => await ChangePageToAsync(1),
            },
            new ActionButton()
            {
                Label = "Left",
                Style = ButtonStyle.Primary,
                Disabled = Page == 1,
                Action = async _ => await ChangePageByAsync(-1),
            },
            new ActionButton()
            {
                Label = "Right",
                Style = ButtonStyle.Primary,
                Disabled = Page == TotalNumberOfPages,
                Action = async _ => await ChangePageByAsync(1),
            },
            new ActionButton()
            {
                Label = "»",
                Style = ButtonStyle.Primary,
                Disabled = Page == TotalNumberOfPages,
                Action = async _ => await ChangePageToAsync(TotalNumberOfPages),
            },
            new ActionButton()
            {
                Label = StoppedByUsername == null ? "Stop" : $"Stopped by {StoppedByUsername}",
                Style = ButtonStyle.Danger,
                Action = async component =>
                {
                    StoppedByUsername = component.User.Username;
                    await StopAsync();
                },
            },
        };

        /// <summary>Modify the page and therefore the embed of this message.</summary>
        public Task ChangePageByAsync(int increment)
            => ChangePageToAsync((int)Page + increment);

        /// <summary>Modify the page and therefore the embed of this message.</summary>
        public async Task ChangePageToAsync(int page)
        {
            Page = page;
            UpdateProperties();
            await UpdateMessageAsync();
            Logger.Log($"PagedMessage {SentMessage.Id} was changed to page {Page}.", LogSeverity.Debug);
        }

        protected virtual void UpdateProperties()
        {
            // Check if the fields don't fit on one page.
            if (AllFieldBuilders.Count > MaxFieldsPerPage)
            {
                // Pages are not used if page number is negative.
                if (Page == null)
                {
                    EmbedBuilder.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = $"https://cdn.discordapp.com/emojis/{WarningEmoteId}.png",
                        Text = $"{AllFieldBuilders.Count - MaxFieldsPerPage} items were excluded",
                    };

                    EmbedBuilder.Fields = AllFieldBuilders.GetRange(0, AllFieldBuilders.Count > 25 ? 25 : AllFieldBuilders.Count);
                }
                else
                {
                    // Ensure page number is within bounds.
                    Page = Math.Min(TotalNumberOfPages, Math.Max(Page ?? 0, 1));

                    EmbedBuilder.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = $"https://cdn.discordapp.com/emojis/{InfoEmoteId}.png",
                        Text = $"Page {Page}/{TotalNumberOfPages}",
                    };

                    // Page 0 and 1 should have the same starting index (never negative).
                    int startIndex = MaxFieldsPerPage * Math.Max((int)Page - 1, 0);

                    // Get the fields that are within the page and set the embed's fields to that.
                    bool isFinalPage = Page == TotalNumberOfPages;
                    EmbedBuilder.Fields = AllFieldBuilders.GetRange(startIndex,
                        isFinalPage ? (AllFieldBuilders.Count - startIndex) : MaxFieldsPerPage);
                }
            }
            else
            {
                Page = null;
                EmbedBuilder.Fields = AllFieldBuilders;
            }
        }
    }
}