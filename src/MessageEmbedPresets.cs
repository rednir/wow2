using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

namespace wow2
{
    public enum VerboseMessageSeverity
    {
        Info, Warning, Error
    }

    public static class MessageEmbedPresets
    {
        /// <summary>Builds and returns an embed for letting the user know the result of an action</summary>
        public static Embed Verbose(string description, VerboseMessageSeverity severity = VerboseMessageSeverity.Info)
        {
            var embedBuilder = new EmbedBuilder();

            switch (severity)
            {
                case VerboseMessageSeverity.Info:
                    embedBuilder.Description = $"{new Emoji("<:wowinfo:804732580423008297>")} {description}";
                    embedBuilder.Color = Color.Blue;
                    break;

                case VerboseMessageSeverity.Warning:
                    embedBuilder.Description = $"{new Emoji("<:wowwarning:804732632751407174>")} {description}";
                    embedBuilder.Color = Color.LightOrange;
                    break;

                case VerboseMessageSeverity.Error:
                    embedBuilder.Title = $"{new Emoji("<:wowerror:804732656721199144>")} Something bad happened...";
                    embedBuilder.Description = description;
                    embedBuilder.Color = Color.Red;
                    break;
            }

            return embedBuilder.Build();
        }

        /// <summary>Builds and returns an embed for responding to some user action.</summary>
        public static Embed GenericResponse(string description, string title = "", string imageUrl = "")
        {
            var embedBuilder = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = Color.LightGrey,
                ImageUrl = imageUrl
            };

            return embedBuilder.Build();
        }

        /// <summary>Builds and returns an embed for showing a list of fields, for example, showing command help.</summary>
        public static Embed Fields(List<EmbedFieldBuilder> fieldBuilders, string title = "", string description = "")
        {
            var embedBuilder = new EmbedBuilder()
            {
                Title = title,
                Description = description,
                Fields = fieldBuilders,
                Color = Color.LightGrey
            };

            return embedBuilder.Build();
        }

        /// <summary>Builds and returns an embed for displaying media metadata.</summary>
        public static Embed NowPlaying
        (
            string title,
            string url = "",
            DateTime timeRequested = new DateTime(),
            SocketUser requestedBy = null,
            string thumbnailUrl = "",
            int? viewCount = null,
            int? likeCount = null,
            int? dislikeCount = null
        )
        {
            var authorBuilder = new EmbedAuthorBuilder()
            {
                Name = "Now Playing",
                IconUrl = "https://cdn4.iconfinder.com/data/icons/social-messaging-ui-color-shapes-2-free/128/social-youtube-circle-512.png",
                Url = url
            };
            var footerBuilder = new EmbedFooterBuilder()
            {
                Text = $"👁️  {viewCount ?? 0}      |      👍  {likeCount ?? 0}      |      👎  {dislikeCount ?? 0}"
            };

            var embedBuilder = new EmbedBuilder()
            {
                Author = authorBuilder,
                Title = title,
                ThumbnailUrl = thumbnailUrl,
                Description = $"Requested at {timeRequested.ToString("HH:mm")} by {requestedBy.Mention}",
                Footer = footerBuilder,
                Color = Color.LightGrey
            };

            return embedBuilder.Build();
        }
    }
}