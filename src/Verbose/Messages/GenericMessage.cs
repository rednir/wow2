using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Discord;

namespace wow2.Verbose.Messages
{
    public class GenericMessage : Message
    {
        public GenericMessage(
            string description = "",
            string title = "",
            List<EmbedFieldBuilder> fieldBuilders = null,
            int fieldBuildersPage = 0)
        {
            const int maxFieldsPerPage = 8;
            const int maxDescriptionLength = 2048;

            if (description.Length >= maxDescriptionLength)
            {
                // The description will be uploaded as a file.
                DescriptionAsStream = new MemoryStream(Encoding.ASCII.GetBytes(description));
                description = "[DELETED]";
            }

            EmbedBuilder = new EmbedBuilder()
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
                    EmbedBuilder.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = $"https://cdn.discordapp.com/emojis/{WarningEmoteId}.png",
                        Text = $"{fieldBuilders.Count - maxFieldsPerPage} items were excluded"
                    };
                }
                else
                {
                    EmbedBuilder.Footer = new EmbedFooterBuilder()
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
            EmbedBuilder.Fields = fieldBuilders ?? new List<EmbedFieldBuilder>();
        }
    }
}