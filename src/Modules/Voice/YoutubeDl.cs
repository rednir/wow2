using System;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;

namespace wow2.Modules.Voice
{
    public class YoutubeDl
    {
        /// <summary>Looks up a URL or search term and gets the video metadata.</summary>
        /// <returns>Video metadata deserialized into <c>YoutubeVideoMetadata</c>.</returns>
        public static async Task<YoutubeVideoMetadata> GetMetadata(string searchOrUrl)
        {
            const string arguments = "-j";
            bool isUrl = searchOrUrl.StartsWith("http://") || searchOrUrl.StartsWith("https://");
            string standardOutput = "";
            string standardError = "";

            await Task.Run(() =>
            {
                using (var process = new Process())
                {
                    // TODO: make youtube-dl path env variable
                    process.StartInfo.FileName = "youtube-dl";
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
                }
            });

            if (!string.IsNullOrWhiteSpace(standardError))
                throw new ArgumentException(standardError);

            var metadata = JsonSerializer.Deserialize<YoutubeVideoMetadata>(standardOutput);

            return metadata;
        }
    }
}