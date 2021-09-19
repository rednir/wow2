using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;

namespace wow2.Bot.Verbose.Messages
{
    /// <summary>Class for sending and building embeds with pages of fields.</summary>
    public class PagedMessage : SavedMessage
    {
        public const string StopText = "Stop";
        public static readonly IEmote PageLeftEmote = new Emoji("⏪");
        public static readonly IEmote PageRightEmote = new Emoji("⏩");

        public PagedMessage()
        {
        }

        public PagedMessage(List<EmbedFieldBuilder> fieldBuilders, string description = "", string title = "", int? page = null)
        {
            AllFieldBuilders = fieldBuilders;
            Page = page;
            EmbedBuilder = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = Color.LightGrey,
            };

            UpdateEmbed();
        }

        public List<EmbedFieldBuilder> AllFieldBuilders { get; protected set; }

        public int? Page { get; protected set; }

        protected override bool DontSave => Page == null;

        /// <summary>If the message has pages, modifies the page of the message.</summary>
        public static async Task<bool> ActOnButtonAsync(SocketMessageComponent component)
        {
            if (FromMessageId(DataManager.AllGuildData[component.Channel.GetGuild().Id], component.Message.Id) is not PagedMessage message)
                return false;

            if (component.Data.CustomId == PageLeftEmote.Name)
            {
                await message.ChangePageByAsync(-1);
            }
            else if (component.Data.CustomId == PageRightEmote.Name)
            {
                await message.ChangePageByAsync(1);
            }
            else if (component.Data.CustomId == StopText)
            {
                await message.StopAsync();
            }
            else
            {
                return false;
            }

            return true;
        }

        public async override Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            if (Page != null)
            {
                Components = new ComponentBuilder()
                    .WithButton("Left", PageLeftEmote.Name, ButtonStyle.Primary, emote: PageLeftEmote)
                    .WithButton("Right", PageRightEmote.Name, ButtonStyle.Primary, emote: PageRightEmote)
                    .WithButton("Stop", StopText, ButtonStyle.Danger);
            }

            return await base.SendAsync(channel);
        }

        /// <summary>Modify the page and therefore the embed of this message.</summary>
        public async Task ChangePageByAsync(int increment)
        {
            Page += increment;
            UpdateEmbed();
            await SentMessage.ModifyAsync(
                message =>
                {
                    message.Content = Text;
                    message.Embed = EmbedBuilder?.Build();
                });
            Logger.Log($"PagedMessage {SentMessage.Id} was changed to page {Page}.", LogSeverity.Debug);
        }

        protected virtual void UpdateEmbed()
        {
            const int maxFieldsPerPage = 8;

            // Check if the fields don't fit on one page.
            if (AllFieldBuilders.Count > maxFieldsPerPage)
            {
                int totalNumberOfPages = (int)Math.Ceiling((float)AllFieldBuilders.Count / maxFieldsPerPage);

                // Pages are not used if page number is negative.
                if (Page == null)
                {
                    EmbedBuilder.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = $"https://cdn.discordapp.com/emojis/{WarningEmoteId}.png",
                        Text = $"{AllFieldBuilders.Count - maxFieldsPerPage} items were excluded",
                    };

                    EmbedBuilder.Fields = AllFieldBuilders.GetRange(0, AllFieldBuilders.Count > 25 ? 25 : AllFieldBuilders.Count);
                }
                else
                {
                    // Ensure page number is within bounds.
                    Page = Math.Min(totalNumberOfPages, Math.Max(Page ?? 0, 1));

                    EmbedBuilder.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = $"https://cdn.discordapp.com/emojis/{InfoEmoteId}.png",
                        Text = $"Page {Page}/{totalNumberOfPages}",
                    };

                    // Page 0 and 1 should have the same starting index (never negative).
                    int startIndex = maxFieldsPerPage * Math.Max((int)Page - 1, 0);

                    // Get the fields that are within the page and set the embed's fields to that.
                    bool isFinalPage = Page == totalNumberOfPages;
                    EmbedBuilder.Fields = AllFieldBuilders.GetRange(startIndex,
                        isFinalPage ? (AllFieldBuilders.Count - startIndex) : maxFieldsPerPage);
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