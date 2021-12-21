using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Web;
using System.Xml;
using Google.Apis.YouTube.v3.Data;
using SpotifyAPI.Web;

namespace wow2.Bot.Modules.Voice
{
    public class VideoMetadata
    {
        public VideoMetadata()
        {
        }

        public VideoMetadata(Video video)
        {
            Title = video.Snippet.Title;
            Id = video.Id;
            Uploader = video.Snippet.ChannelTitle;
            WebpageUrl = "https://www.youtube.com/watch?v=" + video.Id;
            Description = video.Snippet.Description;
            Extractor = "youtube";
            Duration = XmlConvert.ToTimeSpan(video.ContentDetails.Duration).TotalSeconds;
            ViewCount = video.Statistics.ViewCount;
            LikeCount = video.Statistics.LikeCount;
            Thumbnails.Add(new() { url = video.Snippet.Thumbnails.Default__?.Url });
        }

        public VideoMetadata(FullTrack spotifyTrack)
        {
            LookupTitleOnYoutube = true;
            Title = $"{spotifyTrack.Artists.FirstOrDefault()?.Name} - {spotifyTrack.Name}";
            Uploader = "Spotify";
            WebpageUrl = "https://open.spotify.com/track/" + spotifyTrack.Id;
            Extractor = "spotify";
            Duration = spotifyTrack.DurationMs / 1000;
        }

        public VideoMetadata(SimpleTrack spotifyTrack)
        {
            LookupTitleOnYoutube = true;
            Title = $"{spotifyTrack.Artists.FirstOrDefault()?.Name} - {spotifyTrack.Name}";
            Uploader = "Spotify";
            WebpageUrl = "https://open.spotify.com/track/" + spotifyTrack.Id;
            Extractor = "spotify";
            Duration = spotifyTrack.DurationMs / 1000;
        }

        public bool LookupTitleOnYoutube { get; set; }

        public string DirectAudioUrl { get; set; }

        public DateTimeOffset DirectAudioExpiryDate
        {
            get
            {
                if (DirectAudioUrl == null)
                    return DateTimeOffset.MaxValue;

                var query = HttpUtility.ParseQueryString(new Uri(DirectAudioUrl).Query);
                if (long.TryParse(query.Get("expire"), out long unixTime))
                    return DateTimeOffset.FromUnixTimeSeconds(unixTime);

                return DateTimeOffset.MaxValue;
            }
        }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("uploader")]
        public string Uploader { get; set; }

        [JsonPropertyName("webpage_url")]
        public string WebpageUrl { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("extractor")]
        public string Extractor { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("view_count")]
        public ulong? ViewCount { get; set; }

        [JsonPropertyName("like_count")]
        public ulong? LikeCount { get; set; }

        [JsonPropertyName("thumbnails")]
        public List<VideoMetadataThumbnails> Thumbnails { get; set; } = new();
    }
}