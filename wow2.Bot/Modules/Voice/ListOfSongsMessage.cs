using System.Collections.Generic;
using System.Linq;
using Discord;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Voice
{
    public class ListOfSongsMessage : PagedMessage
    {
        public ListOfSongsMessage(Queue<UserSongRequest> queue, string title = "List of songs", int? page = null)
            : base(new List<EmbedFieldBuilder>(), $"*The total duration of the songs below is {VoiceModule.DurationAsString(queue.Sum(r => r.VideoMetadata.Duration))}*", title)
        {
            double totalDuration = 0;
            int i = 0;
            foreach (UserSongRequest songRequest in queue)
            {
                i++;
                totalDuration += songRequest.VideoMetadata.Duration;
                var fieldBuilderForSongRequest = new EmbedFieldBuilder()
                {
                    Name = $"{i}) {songRequest.VideoMetadata.Title}",
                    Value = $"[More details]({songRequest.VideoMetadata.WebpageUrl}) • {VoiceModule.DurationAsString(songRequest.VideoMetadata.Duration)} \nRequested {songRequest.TimeRequested.ToDiscordTimestamp("R")} by {songRequest.RequestedByMention}",
                };
                AllFieldBuilders.Add(fieldBuilderForSongRequest);
            }

            Page = page;
            UpdateProperties();
        }
    }
}