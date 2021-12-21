using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Voice
{
    public class NowPlayingMessage : InteractiveMessage
    {
        protected override ActionButton[] ActionButtons => new[]
        {
            new ActionButton()
            {
                Label = "More details",
                Style = ButtonStyle.Link,
                Url = Request.VideoMetadata.WebpageUrl,
            },
            new ActionButton()
            {
                Label = UsernameWhoSkipped == null ? "Skip this request" : $"Skipped request on behalf of {UsernameWhoSkipped}",
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
            if (Request.VideoMetadata.Extractor.StartsWith("twitch"))
                iconUrl = twitchIconUrl;
            else if (Request.VideoMetadata.Extractor.StartsWith("spotify"))
                iconUrl = spotifyIconUrl;
            else
                iconUrl = youtubeIconUrl;

            EmbedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = "Now Playing",
                    IconUrl = iconUrl,
                    Url = Request.VideoMetadata.WebpageUrl,
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = Request.VideoMetadata.Extractor.StartsWith("youtube") || Request.VideoMetadata.Extractor.StartsWith("spotify") ?
                        $"👁️  {Request.VideoMetadata.ViewCount?.Humanize() ?? "0"}      |      👍  {Request.VideoMetadata.LikeCount?.Humanize() ?? "0"}      |      🕓  {VoiceModule.DurationAsString(Request.VideoMetadata.Duration)}" : string.Empty,
                },
                Title = (Request.VideoMetadata.Extractor == "twitch:stream" ? $"*(LIVE)* {Request.VideoMetadata.Description}" : Request.VideoMetadata.Title)
                    + (request.VideoMetadata.Extractor == "spotify" ? string.Empty : $" *({Request.VideoMetadata.Uploader})*"),
                ThumbnailUrl = Request.VideoMetadata.Thumbnails.FirstOrDefault()?.url,
                Description = $"Requested {Request.TimeRequested.ToDiscordTimestamp("R")} by {Request.RequestedByMention}",
                Color = Color.LightGrey,
            };
        }

        public UserSongRequest Request { get; }

        public VoiceModuleConfig Config { get; }

        public Func<SocketMessageComponent, UserSongRequest, Task> SkipButtonAction { get; }

        public string UsernameWhoSkipped { get; set; }
    }
}