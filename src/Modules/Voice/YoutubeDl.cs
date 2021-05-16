using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Google;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Services;
using wow2.Data;
using wow2.Modules.YouTube;

namespace wow2.Modules.Voice
{
    public static class YouTubeDl
    {
        public static readonly string YouTubeDlPath = Environment.GetEnvironmentVariable("YOUTUBE_DL_PATH") ?? "youtube-dl";
        public static readonly string FFmpegPath = Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "ffmpeg";

        /// <summary>Looks up a URL or search term and gets the video metadata.</summary>
        /// <returns>Video metadata deserialized into <c>YouTubeVideoMetadata</c>.</returns>
        public static async Task<VideoMetadata> GetMetadata(string searchOrUrl)
        {
            searchOrUrl = searchOrUrl.Trim('\"');
            bool isUrl = searchOrUrl.StartsWith("http://") || searchOrUrl.StartsWith("https://"); // TODO

            SearchResult searchResult = await YouTubeModule.SearchForAsync(searchOrUrl, "video");
            Video video = await YouTubeModule.GetVideoAsync(searchResult.Id.VideoId);
            return new VideoMetadata(video);
        }

        /// <returns>The FFmpeg process.</returns>
        public static Process CreateStreamFromVideoUrl(string url)
        {
            const string youtubeDlArgs = "-q -f worstaudio --no-playlist --no-warnings";
            string shellCommand = $"{YouTubeDlPath} {url} {youtubeDlArgs} -o - | {FFmpegPath} -hide_banner -loglevel panic -i - -ac 2 -f s16le -ar 48000 pipe:1";
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            return Process.Start(new ProcessStartInfo
            {
                FileName = isWindows ? "cmd" : "bash",
                Arguments = $"{(isWindows ? "/c" : "-c")} \"{shellCommand}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
    }
}