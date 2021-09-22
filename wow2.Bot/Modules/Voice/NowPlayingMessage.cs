using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Voice
{
    public class NowPlayingMessage : SavedMessage
    {
        protected override ActionButtons[] ActionButtons => new[]
        {
            new ActionButtons()
            {
                Label = "More details",
                Style = ButtonStyle.Link,
                Url = Request.VideoMetadata.webpage_url,
            },
            new ActionButtons()
            {
                Label = "Skip this request",
                Style = ButtonStyle.Secondary,
                Action = async component => await SkipButtonAction.Invoke(component, Request),
            },
        };

        public NowPlayingMessage(UserSongRequest request, VoiceModuleConfig config, Func<SocketMessageComponent, UserSongRequest, Task> skipButton)
        {
            const string youtubeIconUrl = "https://cdn4.iconfinder.com/data/icons/social-messaging-ui-color-shapes-2-free/128/social-youtube-circle-512.png";
            const string twitchIconUrl = "https://www.net-aware.org.uk/siteassets/images-and-icons/application-icons/app-icons-twitch.png?w=585&scale=down";
            const string spotifyIconUrl = "https://www.techspot.com/images2/downloads/topdownload/2016/12/spotify-icon-18.png";

            Request = request;
            Config = config;
            SkipButtonAction = skipButton;

            string iconUrl;
            if (Request.VideoMetadata.extractor.StartsWith("twitch"))
                iconUrl = twitchIconUrl;
            else if (Request.VideoMetadata.extractor.StartsWith("spotify"))
                iconUrl = spotifyIconUrl;
            else
                iconUrl = youtubeIconUrl;

            EmbedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = "Now Playing",
                    IconUrl = iconUrl,
                    Url = Request.VideoMetadata.webpage_url,
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = Request.VideoMetadata.extractor.StartsWith("youtube") || Request.VideoMetadata.extractor.StartsWith("spotify") ?
                        $"👁️  {Request.VideoMetadata.view_count ?? 0}      |      👍  {Request.VideoMetadata.like_count ?? 0}      |      👎  {Request.VideoMetadata.dislike_count ?? 0}      |      🕓  {VoiceModule.DurationAsString(Request.VideoMetadata.duration)}" : string.Empty,
                },
                Title = (Request.VideoMetadata.extractor == "twitch:stream" ? $"*(LIVE)* {Request.VideoMetadata.description}" : Request.VideoMetadata.title) + $" *({Request.VideoMetadata.uploader})*",
                ThumbnailUrl = Request.VideoMetadata.thumbnails.LastOrDefault()?.url,
                Description = $"Requested at {Request.TimeRequested:HH:mm} by {Request.RequestedByMention}",
                Color = Color.LightGrey,
            };
        }

        public UserSongRequest Request { get; }

        public VoiceModuleConfig Config { get; }

        public Func<SocketMessageComponent, UserSongRequest, Task> SkipButtonAction { get; }
    }
}