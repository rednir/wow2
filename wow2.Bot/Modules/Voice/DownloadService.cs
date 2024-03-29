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
using YoutubeExplode;

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

            Video video;
            if (TryGetYoutubeVideoIdFromUrl(searchOrUrl, out string youtubeVideoId))
            {
                video = await YouTubeService.GetVideoAsync(youtubeVideoId);
            }
            else if (searchOrUrl.Contains("twitch.tv/"))
            {
                var twitchMetadata = new List<VideoMetadata>() { await GetMetadataFromYoutubeDlAsync(searchOrUrl) };
                VideoMetadataCache.Add(searchOrUrl, twitchMetadata);
                return twitchMetadata;
            }
            else if (searchOrUrl.Contains("open.spotify.com/track/"))
            {
                var spotifyTrackMetadata = new List<VideoMetadata>() { await GetMetadataFromSpotifyTrackAsync(searchOrUrl) };
                VideoMetadataCache.Add(searchOrUrl, spotifyTrackMetadata);
                return spotifyTrackMetadata;
            }
            else if (searchOrUrl.Contains("open.spotify.com/playlist/"))
            {
                var spotifyPlaylistMetadata = await GetMetadataFromSpotifyPlaylistAsync(searchOrUrl);
                VideoMetadataCache.Add(searchOrUrl, spotifyPlaylistMetadata);
                return new List<VideoMetadata>(spotifyPlaylistMetadata);
            }
            else if (searchOrUrl.Contains("open.spotify.com/album/"))
            {
                var spotifyAlbumMetadata = await GetMetadataFromSpotifyAlbumAsync(searchOrUrl);
                VideoMetadataCache.Add(searchOrUrl, spotifyAlbumMetadata);
                return new List<VideoMetadata>(spotifyAlbumMetadata);
            }
            else
            {
                SearchResult searchResult = await YouTubeService.SearchForAsync(searchOrUrl, "video");
                video = await YouTubeService.GetVideoAsync(searchResult.Id.VideoId);
            }

            var result = new List<VideoMetadata>()
            {
                new VideoMetadata(video) { DirectAudioUrl = await YouTubeService.GetYoutubeAudioUrlAsync(video.Id) },
            };

            VideoMetadataCache.Add(searchOrUrl, result);
            return result;
        }

        /// <summary>Creates a new FFmpeg process that creates an audio stream from url.</summary>
        /// <returns>The FFmpeg process.</returns>
        public static Process CreateStream(VideoMetadata metadata)
        {
            // Use direct audio url if it exists.
            string shellCommand = metadata.DirectAudioUrl == null ?
                $"{YouTubeDlPath} {metadata.WebpageUrl} -q -f worstaudio --no-playlist --no-warnings -o - | {FFmpegPath} -hide_banner -loglevel panic -i - -ac 2 -f s16le -ar 48000 pipe:1" :
                $"{FFmpegPath} -hide_banner -loglevel panic -i \"{metadata.DirectAudioUrl}\" -ac 2 -f s16le -ar 48000 pipe:1";

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return Process.Start(new ProcessStartInfo
            {
                FileName = isWindows ? "cmd" : "bash",
                Arguments = isWindows ? $"/c \"{shellCommand}\"" : $"-c \'{shellCommand}\'",
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

        private static async Task<VideoMetadata> GetMetadataFromSpotifyTrackAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
                return null;

            var track = await SpotifyService.Client.Tracks.Get(uri.Segments.Last());
            return new VideoMetadata(track);
        }

        private static async Task<List<VideoMetadata>> GetMetadataFromSpotifyPlaylistAsync(string url)
        {
            var result = new List<VideoMetadata>();
            await helper(0);
            return result;

            async Task helper(int offset)
            {
                if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
                    return;

                Paging<PlaylistTrack<IPlayableItem>> playlist = await SpotifyService.Client.Playlists.GetItems(
                    uri.Segments.Last(), new PlaylistGetItemsRequest() { Limit = 100, Offset = offset });

                foreach (var item in playlist.Items)
                {
                    if (item.Track?.Type != ItemType.Track)
                        continue;

                    result.Add(new VideoMetadata((FullTrack)item.Track));
                }

                // The max amount of playlist items returned by the spotify api is 100.
                // Make another request if there are still more songs to discover (but avoid too many requests).
                if (result.Count % 100 == 0 && offset < 1000)
                    await helper(offset + 100);
            }
        }

        private static async Task<List<VideoMetadata>> GetMetadataFromSpotifyAlbumAsync(string url)
        {
            // TODO: use try create method.
            FullAlbum album;
            try
            {
                var uri = new Uri(url);
                album = await SpotifyService.Client.Albums.Get(uri.Segments.Last());
            }
            catch (UriFormatException)
            {
                return null;
            }

            var videos = new List<VideoMetadata>();
            foreach (var track in album.Tracks.Items)
                videos.Add(new VideoMetadata(track));

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