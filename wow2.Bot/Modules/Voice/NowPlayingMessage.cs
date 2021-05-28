using System.Linq;
using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Voice
{
    public class NowPlayingMessage : Message
    {
        public NowPlayingMessage(UserSongRequest request)
        {
            const string youtubeIconUrl = "https://cdn4.iconfinder.com/data/icons/social-messaging-ui-color-shapes-2-free/128/social-youtube-circle-512.png";
            const string twitchIconUrl = "https://www.net-aware.org.uk/siteassets/images-and-icons/application-icons/app-icons-twitch.png?w=585&scale=down";

            EmbedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = "Now Playing",
                    IconUrl = request.VideoMetadata.extractor.StartsWith("twitch") ? twitchIconUrl : youtubeIconUrl,
                    Url = request.VideoMetadata.webpage_url,
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = request.VideoMetadata.extractor.StartsWith("youtube") ?
                        $"👁️  {request.VideoMetadata.view_count ?? 0}      |      👍  {request.VideoMetadata.like_count ?? 0}      |      👎  {request.VideoMetadata.dislike_count ?? 0}      |      🕓  {VoiceModule.DurationAsString(request.VideoMetadata.duration)}" : string.Empty,
                },
                Title = (request.VideoMetadata.extractor == "twitch:stream" ? $"*(LIVE)* {request.VideoMetadata.description}" : request.VideoMetadata.title) + $" *({request.VideoMetadata.uploader})*",
                ThumbnailUrl = request.VideoMetadata.thumbnails.LastOrDefault()?.url,
                Description = $"Requested at {request.TimeRequested:HH:mm} by {request.RequestedBy?.Mention}",
                Color = Color.LightGrey,
            };
        }
    }
}