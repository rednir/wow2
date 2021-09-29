using System;
using System.Collections.Generic;
using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Keywords
{
    public class ResponseGallery : PagedMessage
    {
        public ResponseGallery(List<KeywordValue> values)
            : base(new List<EmbedFieldBuilder>(), "INITIAL STATE", string.Empty)
        {
            Values = values ?? throw new ArgumentNullException(nameof(values));
            Page = 1;
            UpdateProperties();
        }

        private List<KeywordValue> Values { get; }

        protected override void UpdateProperties()
        {
            // This method is called in the ctor of PagedMessage when Page is not set yet.
            if (Page == null)
                return;

            if (Page > Values.Count)
                Page = 1;
            else if (Page < 1)
                Page = Values.Count;

            KeywordValue value = Values[Page.Value - 1];
            Text = $"**⋘ page {Page}/{Values.Count} ⋙**\n";

            // Don't use embed message if the value to send contains a link.
            if (value.Content.Contains("http://") || value.Content.Contains("https://"))
            {
                // todo: This is not the same implementation as SendAsPlainTextAsync()
                // Probably won't cause any major issues but may display values with titles incorrectly.
                Text += value.Content;
                EmbedBuilder = null;
            }
            else
            {
                if (EmbedBuilder == null)
                    EmbedBuilder = new EmbedBuilder();

                EmbedBuilder.Title = value.Title;
                EmbedBuilder.Description = value.Content;
            }
        }
    }
}