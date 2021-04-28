using System;
using System.Text.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
            const string arguments = "-j -q";
            bool isUrl = searchOrUrl.StartsWith("http://") || searchOrUrl.StartsWith("https://");
            string standardOutput = "";
            string standardError = "";

            await Task.Run(() =>
            {
                using var process = new Process();

                process.StartInfo.FileName = YouTubeDlPath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.Arguments = isUrl ? $"\"{searchOrUrl}\" {arguments}" : $"ytsearch:\"{searchOrUrl}\" {arguments}";

                process.OutputDataReceived += (sendingProcess, outline) => standardOutput += outline.Data + "\n";
                process.ErrorDataReceived += (sendingProcess, outline) => standardError += outline.Data + "\n";

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
            });

            if (!string.IsNullOrWhiteSpace(standardError))
                throw new ArgumentException(standardError);

            var metadata = JsonSerializer.Deserialize<VideoMetadata>(standardOutput);
            return metadata;
        }

        /// <returns>The FFmpeg process.</returns>
        public static Process CreateStreamFromVideoUrl(string url)
        {
            const string youtubeDlArgs = "-q -f worstaudio --no-playlist --no-warnings";
            string shellCommand = $"{YouTubeDlPath} {url} {youtubeDlArgs} -o - | {FFmpegPath} -hide_banner -loglevel panic -i - -ac 2 -f s16le -ar 44100 pipe:1";
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