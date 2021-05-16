using System.Collections.Generic;
using Google.Apis.YouTube.v3.Data;

namespace wow2.Modules.Voice
{
    public class VideoMetadata
    {
        public VideoMetadata()
        {
        }

        public VideoMetadata(Video video)
        {
            Title = video.Snippet.Title;
            Uploader = video.Snippet.ChannelTitle;
            WebpageUrl = "https://www.youtube.com/watch?v=" + video.Id;
            Description = video.Snippet.Description;
            Extractor = "youtube";
            Duration = 100; // TODO
            ViewCount = video.Statistics.ViewCount;
            LikeCount = video.Statistics.LikeCount;
            DislikeCount = video.Statistics.DislikeCount;
            ThumbnailUrl = video.Snippet.Thumbnails.Default__.Url;
        }

        public string Title { get; set; }
        public string Uploader { get; set; }
        public string WebpageUrl { get; set; }
        public string Description { get; set; }
        public string Extractor { get; set; }

        public float? Duration { get; set; }
        public ulong? ViewCount { get; set; }
        public ulong? LikeCount { get; set; }
        public ulong? DislikeCount { get; set; }

        public string ThumbnailUrl { get; set; }
    }
}