using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

namespace wow2.Verbose
{
    public static class Messenger
    {
        public static async Task SendSuccessAsync(ISocketMessageChannel channel, string description, string title = null)
            => await channel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Description = $"{new Emoji("<:wowsuccess:823595458978512997>")} {description}",
                Color = Color.Green
            }
            .Build());

        public static async Task SendInfoAsync(ISocketMessageChannel channel, string description, string title = null)
            => await channel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Description = $"{new Emoji("<:wowinfo:804732580423008297>")} {description}",
                Color = Color.Blue
            }
            .Build());

        public static async Task SendWarningAsync(ISocketMessageChannel channel, string description, string title = null)
             => await channel.SendMessageAsync(embed: new EmbedBuilder()
             {
                 Description = $"{new Emoji("<:wowwarning:804732632751407174>")} {description}",
                 Color = Color.LightOrange
             }
            .Build());

        public static async Task SendErrorAsync(ISocketMessageChannel channel, string description, string title = null)
            => await channel.SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = $"{new Emoji("<:wowerror:804732656721199144>")} Something bad happened...",
                Description = description,
                Color = Color.Red
            }
            .Build());

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

        /// <summary>Builds and returns an embed for displaying about text.</summary>
        public static Embed About(string name, IUser author, string description = "", string footer = "")
        {
            var authorBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Hosted by {author}",
                IconUrl = author.GetAvatarUrl(),
            };
            var footerBuilder = new EmbedFooterBuilder()
            {
                Text = footer
            };

            var embedBuilder = new EmbedBuilder()
            {
                Title = name,
                Description = description,
                Author = authorBuilder,
                Footer = footerBuilder,
                Color = Color.LightGrey
            };

            return embedBuilder.Build();
        }

        /// <summary>Builds and returns an embed for showing a list of fields, for example, showing command help.</summary>
        public static Embed Fields(List<EmbedFieldBuilder> fieldBuilders, string title = "", string description = "")
        {
            const int maxFields = 24;

            // Truncate list to comply with the embed fields limit. 
            if (fieldBuilders.Count >= maxFields)
                fieldBuilders.RemoveRange(maxFields, fieldBuilders.Count - maxFields);

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