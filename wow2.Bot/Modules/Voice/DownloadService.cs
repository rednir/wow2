using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Google.Apis.YouTube.v3.Data;
using SpotifyAPI.Web;
using wow2.Bot.Modules.Spotify;
using wow2.Bot.Modules.YouTube;
using wow2.Bot.Verbose;

namespace wow2.Bot.Modules.Voice
{
    public static class DownloadService
    {
        public static readonly string YouTubeDlPath = Environment.GetEnvironmentVariable("YOUTUBE_DL_PATH") ?? "youtube-dl";
        public static readonly string FFmpegPath = Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "ffmpeg";

        public static IYoutubeModuleService YouTubeService { get; set; }

        public static ISpotifyModuleService SpotifyService { get; set; }

        public static Cache<List<VideoMetadata>> VideoMetadataCache { get; } = new(120);

        /// <summary>Looks up a URL or search term and gets the video metadata.</summary>
        /// <returns>A list of video metadata. Will have only one element unless it is a playlist.</returns>
        public static async Task<List<VideoMetadata>> GetMetadataAsync(string searchOrUrl)
        {
            searchOrUrl = searchOrUrl.Trim('\"');

            if (VideoMetadataCache.TryFetch(searchOrUrl, out var metadataFromCache))
                return metadataFromCache;

            // TODO: this seems like its getting a little bloated, could do with a lookover.
            Video video;
            if (TryGetYoutubeVideoIdFromUrl(searchOrUrl, out string youtubeVideoId))
            {
                video = await YouTubeService.GetVideoAsync(youtubeVideoId);
            }
            else if (searchOrUrl.Contains("twitch.tv/"))
            {
                return new List<VideoMetadata>() { await GetMetadataFromYoutubeDlAsync(searchOrUrl) };
            }
            else if (searchOrUrl.Contains("open.spotify.com/playlist/"))
            {
                return new List<VideoMetadata>(await GetMetadataFromSpotifyPlaylistAsync(searchOrUrl));
            }
            else
            {
                SearchResult searchResult = await YouTubeService.SearchForAsync(searchOrUrl, "video");
                video = await YouTubeService.GetVideoAsync(searchResult.Id.VideoId);
            }

            var videoMetadata = new List<VideoMetadata>() { new VideoMetadata(video) };
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

        private static async Task<List<VideoMetadataFromSpotify>> GetMetadataFromSpotifyPlaylistAsync(string url)
        {
            Paging<PlaylistTrack<IPlayableItem>> playlist;
            try
            {
                var uri = new Uri(url);

                // TODO: increase limit.
                playlist = await SpotifyService.Client.Playlists.GetItems(uri.Segments.Last());
            }
            catch (UriFormatException)
            {
                return null;
            }

            var videos = new List<VideoMetadataFromSpotify>();
            foreach (var item in playlist.Items)
            {
                if (item.Track.Type != ItemType.Track)
                    continue;

                videos.Add(new VideoMetadataFromSpotify((FullTrack)item.Track));
            }

            return videos;
        }

        private static bool TryGetYoutubeVideoIdFromUrl(string url, out string id)
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