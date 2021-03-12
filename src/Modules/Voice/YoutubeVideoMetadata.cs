

namespace wow2.Modules.Voice
{
    public class YoutubeVideoMetadata
    {
        public string title { get; set; }
        public string webpage_url { get; set; }
        public string description { get; set; }

        public int? view_count { get; set; }
        public int? like_count { get; set; }
        public int? dislike_count { get; set; }

        //public List<MetadataThumbnails> thumbnails { get; set; }
    }
}