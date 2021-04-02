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
    /// <summary>Base class for sending and building embed messages.</summary>
    public abstract class Message
    {
        public const ulong SuccessEmoteId = 823595458978512997;
        public const ulong InfoEmoteId = 804732580423008297;
        public const ulong WarningEmoteId = 804732632751407174;
        public const ulong ErrorEmoteId = 804732656721199144;

        public Embed Embed
        {
            get { return EmbedBuilder.Build(); }
        }

        protected EmbedBuilder EmbedBuilder;
        protected MemoryStream DescriptionAsStream;

        public async Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            if (DescriptionAsStream != null)
            {
                return await channel.SendFileAsync(
                    stream: DescriptionAsStream,
                    filename: $"{Embed?.Title}_desc.txt",
                    embed: new WarningMessage("A message was too long, so it was uploaded as a file.").Embed);
            }
            else
            {
                return await channel.SendMessageAsync(embed: EmbedBuilder.Build());
            }
        }

        protected string GetStatusMessageFormattedDescription(string description, string title)
            => $"{(title == null ? null : $"**{title}**\n")}{description}";
    }

    public class SuccessMessage : Message
    {
        public SuccessMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowsuccess:{SuccessEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = Color.Green
            };
        }
    }

    public class InfoMessage : Message
    {
        public InfoMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowinfo:{InfoEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = Color.Blue
            };
        }
    }

    public class WarningMessage : Message
    {
        public WarningMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"{new Emoji($"<:wowwarning:{WarningEmoteId}>")} {GetStatusMessageFormattedDescription(description, title)}",
                Color = Color.LightOrange
            };
        }
    }

    public class ErrorMessage : Message
    {
        public ErrorMessage(string description, string title = null)
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Title = $"{new Emoji($"<:wowerror:{ErrorEmoteId}>")} Something bad happened...",
                Description = GetStatusMessageFormattedDescription(description, title),
                Color = Color.Red
            };
        }
    }

    public class GenericMessage : Message
    {
        public GenericMessage(
            string description = "",
            string title = "",
            List<EmbedFieldBuilder> fieldBuilders = null,
            int fieldBuildersPage = 0)
        {
            const int maxFieldsPerPage = 10;
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