using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Google.Apis.YouTube.v3.Data;
using wow2.Bot.Modules.YouTube;

namespace wow2.Bot.Modules.Voice
{
    public static class DownloadService
    {
        public static readonly string YouTubeDlPath = Environment.GetEnvironmentVariable("YOUTUBE_DL_PATH") ?? "youtube-dl";
        public static readonly string FFmpegPath = Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "ffmpeg";

        public static Cache<VideoMetadata> VideoMetadataCache { get; } = new(120);

        /// <summary>Looks up a URL or search term and gets the video metadata.</summary>
        /// <returns>Video metadata deserialized into <c>YouTubeVideoMetadata</c>.</returns>
        public static async Task<VideoMetadata> GetMetadataAsync(string searchOrUrl)
        {
            searchOrUrl = searchOrUrl.Trim('\"');

            if (VideoMetadataCache.TryFetch(searchOrUrl, out var metadataFromCache))
                return metadataFromCache;

            Video video;
            if (TryGetVideoIdFromUrl(searchOrUrl, out string id))
            {
                video = await YouTubeModule.GetVideoAsync(id);
            }
            else if (searchOrUrl.Contains("twitch.tv/"))
            {
                return await GetMetadataFromYoutubeDlAsync(searchOrUrl);
            }
            else
            {
                SearchResult searchResult = await YouTubeModule.SearchForAsync(searchOrUrl, "video");
                video = await YouTubeModule.GetVideoAsync(searchResult.Id.VideoId);
            }

            var videoMetadata = new VideoMetadata(video);
            VideoMetadataCache.Add(searchOrUrl, videoMetadata);
            return videoMetadata;
        }

        /// <summary>Creates a new FFmpeg process that creates an audio stream from youtube-dl.</summary>
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

        private static async Task<VideoMetadata> GetMetadataFromYoutubeDlAsync(string input)
        {
            const string arguments = "-j -q";
            bool isUrl = input.StartsWith("http://") || input.StartsWith("https://");
            string standardOutput = string.Empty;
            string standardError = string.Empty;

            using (var process = new Process())
            {
                process.StartInfo.FileName = YouTubeDlPath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.Arguments = isUrl ? $"\"{input}\" {arguments}" : $"ytsearch:\"{input}\" {arguments}";

                process.OutputDataReceived += (sendingProcess, outline) => standardOutput += outline.Data + "\n";
                process.ErrorDataReceived += (sendingProcess, outline) => standardError += outline.Data + "\n";

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();
            }

            if (!string.IsNullOrWhiteSpace(standardError))
                throw new ArgumentException(standardError);

            return JsonSerializer.Deserialize<VideoMetadata>(standardOutput);
        }

        private static bool TryGetVideoIdFromUrl(string url, out string id)
        {
            try
            {
                var uri = new Uri(url);
                if (!uri.Host.Contains("youtu"))
                {
                    id = null;
                    return false;
                }

                var query = HttpUtility.ParseQueryString(uri.Query);
                id = query.AllKeys.Contains("v") ? query["v"] : uri.Segments.Last();
                return true;
            }
            catch (UriFormatException)
            {
                id = null;
                return false;
            }
        }
    }
}