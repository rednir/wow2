using System;
using System.Collections.Generic;
using System.Xml;
using Google.Apis.YouTube.v3.Data;

namespace wow2.Bot.Modules.Voice
{
    public class VideoMetadata
    {
        public VideoMetadata()
        {
        }

        public VideoMetadata(Video video)
        {
            title = video.Snippet.Title;
            uploader = video.Snippet.ChannelTitle;
            webpage_url = "https://www.youtube.com/watch?v=" + video.Id;
            description = video.Snippet.Description;
            extractor = "youtube";
            duration = XmlConvert.ToTimeSpan(video.ContentDetails.Duration).TotalSeconds;
            view_count = video.Statistics.ViewCount;
            like_count = video.Statistics.LikeCount;
            dislike_count = video.Statistics.DislikeCount;
            thumbnails.Add(new() { url = video.Snippet.Thumbnails.Default__.Url });
        }

        public string title { get; set; }
        public string uploader { get; set; }
        public string webpage_url { get; set; }
        public string description { get; set; }
        public string extractor { get; set; }
        public double duration { get; set; }

        public ulong? view_count { get; set; }
        public ulong? like_count { get; set; }
        public ulong? dislike_count { get; set; }

        public List<VideoMetadataThumbnails> thumbnails { get; set; } = new();
    }
}