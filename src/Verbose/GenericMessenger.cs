using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Rest;

namespace wow2.Verbose
{
    /// <summary>Class containing methods used to send embeds that are not specific to one command module.</summary>
    public static class GenericMessenger
    {
        public static readonly ulong SuccessEmoteId = 823595458978512997;
        public static readonly ulong InfoEmoteId = 804732580423008297;
        public static readonly ulong WarningEmoteId = 804732632751407174;
        public static readonly ulong ErrorEmoteId = 804732656721199144;

        /// <summary>Notify a channel about a successful action.</summary>
        public static async Task<IUserMessage> SendSuccessAsync(IMessageChannel channel, string description, string title = null)
            => await channel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowsuccess:{SuccessEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = Color.Green
            }
            .Build());

        /// <summary>Notify a channel about some infomation.</summary>
        public static async Task<IUserMessage> SendInfoAsync(IMessageChannel channel, string description, string title = null)
            => await channel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowinfo:{InfoEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = Color.Blue
            }
            .Build());

        /// <summary>Notify a channel about a handled unsuccessful action.</summary>
        public static async Task<IUserMessage> SendWarningAsync(IMessageChannel channel, string description, string title = null)
             => await channel.SendMessageAsync(embed: new EmbedBuilder()
             {
                 Description = $"{new Emoji($"<:wowwarning:{WarningEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                 Color = Color.LightOrange
             }
            .Build());

        /// <summary>Notify a channel about an unhandled error.</summary>
        public static async Task<IUserMessage> SendErrorAsync(IMessageChannel channel, string description, string title = null)
            => await channel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = $"{new Emoji($"<:wowerror:{ErrorEmoteId}>")} Something bad happened...",
                Description = GetStatusMessageFormattedDescription(description, title),
                Color = Color.Red
            }
            .Build());

        /// <summary>Send a generic response to a command in a channel.</summary>
        public static async Task<IUserMessage> SendResponseAsync(
            IMessageChannel channel,
            string description = "",
            string title = "",
            List<EmbedFieldBuilder> fieldBuilders = null,
            int fieldBuildersPage = 0)
        {
            const int maxFieldsPerPage = 10;
            const int maxDescriptionLength = 2048;

            if (description.Length >= maxDescriptionLength)
            {
                MemoryStream descriptionStream = new MemoryStream(Encoding.ASCII.GetBytes(description));
                await GenericMessenger.SendWarningAsync(channel, $"A message was too long, so it was uploaded as a file.");
                await channel.SendFileAsync(descriptionStream, $"{title}_text.txt");
                return null;
            }

            var embedBuilder = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = Color.LightGrey,
            };

            // Check if the fields don't fit on one page.
            // If fields is null, this if statement is ignored.
            if (fieldBuilders?.Count > maxFieldsPerPage)
            {
                int totalFieldBuilderPages = (int)Math.Ceiling((float)fieldBuilders.Count / (float)maxFieldsPerPage);

                // Page number should always be within bounds.
                fieldBuildersPage = Math.Min(totalFieldBuilderPages, Math.Max(1, fieldBuildersPage));

                // Check if the page number has not been specifed by the method caller.
                if (fieldBuildersPage == 0)
                {
                    embedBuilder.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = $"https://cdn.discordapp.com/emojis/{WarningEmoteId}.png",
                        Text = $"{fieldBuilders.Count - maxFieldsPerPage} items were excluded"
                    };
                }
                else
                {
                    embedBuilder.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = $"https://cdn.discordapp.com/emojis/{InfoEmoteId}.png",
                        Text = $"Page {fieldBuildersPage}/{totalFieldBuilderPages}"
                    };
                }

                // Page 0 and 1 should have the same starting index (never negative).
                int startIndex = maxFieldsPerPage * (fieldBuildersPage - (fieldBuildersPage == 0 ? 0 : 1));
                bool isFinalPage = fieldBuildersPage == totalFieldBuilderPages;

                // Get the fields that are within the page and set fieldBuilders to that.
                fieldBuilders = fieldBuilders.GetRange(startIndex,
                    isFinalPage ? (fieldBuilders.Count - startIndex) : maxFieldsPerPage);
            }
            embedBuilder.Fields = fieldBuilders ?? new List<EmbedFieldBuilder>();

            return await channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        private static string GetStatusMessageFormattedDescription(string description, string title)
            => $"{(title == null ? null : $"**{title}**\n")}{description}";
    }
}